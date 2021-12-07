using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
    public class GameObjectField : ExpandableInspectorField
	{
		protected override int Length { get { return components.Count + 4; } } // 4: active, name, tag, layer

		private PropertyInfo layerProp;

		private readonly List<List<Component>> components = new List<List<Component>>();
		private readonly HashSet<Object> expandedElements = new HashSet<Object>();

		private Type[] addComponentTypes;

		internal static ExposedMethod addComponentMethod = new ExposedMethod(
			typeof( GameObjectField ).GetMethod( nameof( AddComponentButtonClicked ), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ),
			new RuntimeInspectorButtonAttribute( "Add Component", false, ButtonVisibility.InitializedObjects ),
			false);
		internal static ExposedMethod removeComponentMethod = new ExposedMethod(
			typeof( GameObjectField ).GetMethod( nameof( RemoveComponentButtonClicked ), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static ),
			new RuntimeInspectorButtonAttribute( "Remove Component", false, ButtonVisibility.InitializedObjects ),
			true);

		public override void Initialize()
		{
			base.Initialize();
			layerProp = typeof( GameObject ).GetProperty( "layer" );
		}

		public override bool SupportsType( Type type )
		{
			return typeof( GameObject ) == type;
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();

			components.Clear();
			expandedElements.Clear();
		}

		protected override void ClearElements()
		{
			expandedElements.Clear();
			for( int i = 0; i < elements.Count; i++ )
			{
				// Don't keep track of non-expandable drawers' or destroyed components' expanded states
				if( elements[i] is ExpandableInspectorField drawer && drawer.Value is Object obj && drawer.IsExpanded )
					expandedElements.Add( obj );
			}

			base.ClearElements();
		}

		protected override void GenerateElements()
		{
			CreateDrawer<GameObject, bool>(
				variableName: "Is Active",
				getter: go => go.activeSelf,
				setter: ( go, active ) => go.SetActive( active ));

			StringField tagField = CreateDrawer<GameObject, string>(
				variableName: "Tag",
				getter: go => go.tag,
				setter: ( go, tag ) => go.tag = tag ) as StringField;

			StringField nameField = CreateDrawer<GameObject, string>(
				variableName: "Name",
				getter: go => go.name,
				setter: ( go, name ) =>
				{
					go.name = name;
					NameRaw = go.GetNameWithType();

					RuntimeHierarchy hierarchy = Inspector.ConnectedHierarchy;
					if( hierarchy )
						hierarchy.RefreshNameOf( go.transform );
				}) as StringField;

			CreateDrawerForVariable( layerProp, "Layer" );

			foreach( var multiEditedComponents in components )
			{
				InspectorField drawer = CreateDrawerForComponent( multiEditedComponents );

				if( !( drawer is ExpandableInspectorField expandable ) )
					return;

				foreach( Component comp in multiEditedComponents )
				{
					if( expandedElements.Contains( comp ) )
					{
						// If one of the multi-edited components is expanded, expand their shared drawer
						expandable.IsExpanded = true;
						break;
					}
				}
			}

			if( nameField )
				nameField.SetterMode = StringField.Mode.OnSubmit;

			if( tagField )
				tagField.SetterMode = StringField.Mode.OnSubmit;

			if( Inspector.ShowAddComponentButton )
				CreateExposedMethodButton( addComponentMethod, () => this, ( value ) => { } );

			expandedElements.Clear();
		}

		private List<Component> GetFilteredComponents( GameObject go )
		{
			var comps = new List<Component>();
			go.GetComponents( comps );

			for( int i = comps.Count - 1; i >= 0; i-- )
			{
				if( !comps[i] )
					comps.RemoveAt( i );
			}

			if( Inspector.ComponentFilter != null )
				Inspector.ComponentFilter( go, comps );

			return comps;
		}

		public override void Refresh()
		{
			// Refresh components
			components.Clear();
			if( Value is GameObject go )
			{
				foreach( Component comp in GetFilteredComponents( go ) )
					components.Add( new List<Component> { comp } );
			}
			else if( Value is IEnumerable gameObjects )
			{
				var lut = new Dictionary<Type, Dictionary<GameObject, Queue<Component>>>();
				int goCount = 0;

				foreach( GameObject obj in gameObjects )
				{
					goCount++;
					foreach( Component comp in GetFilteredComponents( obj ) )
					{
						Dictionary<GameObject, Queue<Component>> dictInLut;
						Type compType = comp.GetType();

						if( !lut.TryGetValue( compType, out dictInLut ) )
						{
							dictInLut = new Dictionary<GameObject, Queue<Component>>
							{
								{ obj, new Queue<Component>() },
							};
						}

						dictInLut[obj].Enqueue( comp );
					}
				}

				while( lut.Count > 0 )
				{
					var invalidTypes = new List<Type>();

					foreach( var pair in lut )
					{
						Type compType = pair.Key;
						var dictInLut = pair.Value;
						var comps = new List<Component>( goCount );
						bool compOnAllObjects = true;

						foreach( GameObject obj in gameObjects )
						{
							if( dictInLut.TryGetValue( obj, out var queue ) )
							{
								comps.Add( queue.Dequeue() );
								if( queue.Count == 0 )
									dictInLut.Remove( obj );
							}
							else
							{
								compOnAllObjects = false;
								invalidTypes.Add( compType );
								break;
							}
						}

						if( compOnAllObjects )
						{
							components.Add( comps );

							if( dictInLut.Count == 0)
								invalidTypes.Add( compType );
						}
					}

					foreach( Type type in invalidTypes )
						lut.Remove( type );
				}
			}

			// Regenerate components' drawers, if necessary
			base.Refresh();
		}

		private void FillComponentTypes()
		{
			List<Type> componentTypes = new List<Type>( 128 );

#if UNITY_EDITOR || !NETFX_CORE
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
#else
			// Common Unity assemblies
			IEnumerable<Assembly> assemblies = new HashSet<Assembly> 
			{
				typeof( Transform ).Assembly,
				typeof( RectTransform ).Assembly,
				typeof( Rigidbody ).Assembly,
				typeof( Rigidbody2D ).Assembly,
				typeof( AudioSource ).Assembly
			};
#endif
			// Search assemblies for Component types
			foreach( Assembly assembly in assemblies )
			{
#if( NET_4_6 || NET_STANDARD_2_0 ) && ( UNITY_EDITOR || !NETFX_CORE )
				if( assembly.IsDynamic )
					continue;
#endif
				try
				{
					foreach( Type type in assembly.GetExportedTypes() )
					{
						if( !typeof( Component ).IsAssignableFrom( type ) )
							continue;

#if UNITY_EDITOR || !NETFX_CORE
						if( type.IsGenericType || type.IsAbstract )
#else
						if( type.GetTypeInfo().IsGenericType || type.GetTypeInfo().IsAbstract )
#endif
							continue;

						componentTypes.Add( type );
					}
				}
				catch( NotSupportedException ) { }
				catch( System.IO.FileNotFoundException ) { }
				catch( Exception e )
				{
					Debug.LogError( "Couldn't search assembly for Component types: " + assembly.GetName().Name + "\n" + e.ToString() );
				}
			}

			addComponentTypes = componentTypes.ToArray();
		}

		[UnityEngine.Scripting.Preserve] // This method is bound to addComponentMethod
		private void AddComponentButtonClicked()
		{
			ObjectReferencePicker.ReferenceCallback onSelectionConfirmed = null;

			if( Value is GameObject target )
			{
				onSelectionConfirmed = type =>
				{
					// Make sure that RuntimeInspector is still inspecting this GameObject
					if( type != null && target && Inspector && ( Inspector.InspectedObject as GameObject ) == target )
					{
						target.AddComponent( (Type) type );
						Inspector.Refresh();
					}
				};
			}
			else if( Value is IEnumerable<GameObject> targets )
			{
				onSelectionConfirmed = type =>
				{
					if( !( Inspector.InspectedObject is IEnumerable<GameObject> inspected ) )
						return;

					if( type == null || targets == null || !Inspector || inspected != targets )
						return;

					foreach( GameObject target in targets )
						target.AddComponent( (Type) type );

					Inspector.Refresh();
				};
			}
			else
			{
				return;
			}

			ObjectReferencePicker.Instance.Skin = Inspector.Skin;
			ObjectReferencePicker.Instance.Show(
				onReferenceChanged: null,
				onSelectionConfirmed: onSelectionConfirmed,
				referenceNameGetter:        type => ( (Type) type ).FullName,
				referenceDisplayNameGetter: type => ( (Type) type ).FullName,
                references: addComponentTypes,
				initialReference: null,
				includeNullReference: false,
				title: "Add Component",
				referenceCanvas: Inspector.Canvas );
		}

		[UnityEngine.Scripting.Preserve] // This method is bound to removeComponentMethod
		private static void RemoveComponentButtonClicked( ExpandableInspectorField componentDrawer )
		{
			if( !componentDrawer || !componentDrawer.Inspector )
				return;

			void Remove( Component component )
			{
				if( component && !( component is Transform ) )
					componentDrawer.StartCoroutine( RemoveComponentCoroutine( component, componentDrawer.Inspector ) );
			}

			if( componentDrawer.Value is Component component )
				Remove( component );
			else if( componentDrawer.Value is IEnumerable<Component> components )
				foreach( var comp in components )
					Remove( comp );
		}

		private static IEnumerator RemoveComponentCoroutine( Component component, RuntimeInspector inspector )
		{
			Destroy( component );

			// Destroy operation doesn't take place immediately, wait for the component to be fully destroyed
			yield return null;

			inspector.Refresh();
			inspector.EnsureScrollViewIsWithinBounds(); // Scroll view's contents can get out of bounds after removing a component
		}
	}
}
