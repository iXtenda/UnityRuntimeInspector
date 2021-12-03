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

		private Getter isActiveGetter, nameGetter, tagGetter;
		private Setter isActiveSetter, nameSetter, tagSetter;
		private PropertyInfo layerProp;

		private readonly Dictionary<Type, List<Component>> components = new Dictionary<Type, List<Component>>();
		private readonly Dictionary<Type, bool> componentsExpandedStates = new Dictionary<Type, bool>();

		private Type[] addComponentTypes;

		internal static ExposedMethod addComponentMethod = new ExposedMethod(
			typeof( GameObjectField ).GetMethod( nameof( AddComponentButtonClicked ), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ),
			new RuntimeInspectorButtonAttribute( "Add Component", false, ButtonVisibility.InitializedObjects ),
			false);
		internal static ExposedMethod removeComponentMethod = new ExposedMethod(
			typeof( GameObjectField ).GetMethod( nameof( RemoveComponentButtonClicked ), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static ),
			new RuntimeInspectorButtonAttribute( "Remove Component", false, ButtonVisibility.InitializedObjects ),
			true);

		private void SetterWrapper<T>( Action<GameObject, T> setter, T value )
		{
			if( Value is IEnumerable<GameObject> objs )
				foreach( GameObject go in objs )
					setter( go, value );
			else
				setter( (GameObject) Value, value );
		}

		private T GetterWrapper<T>( Func<GameObject, T> getter ) where T : class
		{
			if( Value is IEnumerable<GameObject> objs )
			{
				T value = null;
				foreach( var go in objs )
				{
					if( value == null )
					{
						value = getter( go );
						continue;
					}

					if( value.Equals( getter( go ) ) )
						return null;
				}

				return value;
			}

			return getter( (GameObject) Value );
		}

		public override void Initialize()
		{
			base.Initialize();

			isActiveGetter = () => GetterWrapper( go => (object) go.activeSelf );
			tagGetter      = () => GetterWrapper( go => go.tag );
			nameGetter     = () => GetterWrapper( go => go.name );

			isActiveSetter = value => SetterWrapper( ( go, x ) => go.SetActive( (bool) x ), value );
			tagSetter      = value => SetterWrapper( ( go, x ) => go.tag = (string) x, value );
			nameSetter     = value => SetterWrapper( ( go, x ) =>
			{
				go.name = (string) x;
				NameRaw = go.GetNameWithType();

				RuntimeHierarchy hierarchy = Inspector.ConnectedHierarchy;
				if( hierarchy )
					hierarchy.RefreshNameOf( go.transform );
			}, value);

			layerProp = typeof( GameObject ).GetProperty( "layer" );
		}

		public override bool SupportsType( Type type )
		{
			return typeof( GameObject ) == type || typeof( IEnumerable<GameObject> ).IsAssignableFrom( type );
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();

			components.Clear();
			componentsExpandedStates.Clear();
		}

		protected override void ClearElements()
		{
			componentsExpandedStates.Clear();
			for( int i = 0; i < elements.Count; i++ )
			{
				// Don't keep track of non-expandable drawers' or destroyed components' expanded states
				if( elements[i] is ExpandableInspectorField drawer && drawer.Value as Object )
					componentsExpandedStates[drawer.GetType()] = drawer.IsExpanded;
			}

			base.ClearElements();
		}

		protected override void GenerateElements()
		{
			if( components.Count == 0 )
				return;

			CreateDrawer( typeof( bool? ), "Is Active", isActiveGetter, isActiveSetter );
			StringField nameField = CreateDrawer( typeof( string ), "Name", nameGetter, nameSetter ) as StringField;
			StringField tagField = CreateDrawer( typeof( string ), "Tag", tagGetter, tagSetter ) as StringField;
			CreateDrawerForVariable( layerProp, "Layer" );

			foreach( var pair in components )
			{
				InspectorField componentDrawer = CreateDrawerForComponent( pair.Value );
				if( componentDrawer is ExpandableInspectorField expandableDrawer && componentsExpandedStates.ContainsKey( pair.Key ) )
					expandableDrawer.IsExpanded = true;
			}

			if( nameField )
				nameField.SetterMode = StringField.Mode.OnSubmit;

			if( tagField )
				tagField.SetterMode = StringField.Mode.OnSubmit;

			if( Inspector.ShowAddComponentButton )
				CreateExposedMethodButton( addComponentMethod, () => this, ( value ) => { } );

			componentsExpandedStates.Clear();
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
					components[comp.GetType()] = new List<Component> { comp };
			}
			else if( Value is IEnumerable<GameObject> gameObjects )
			{
				foreach( GameObject obj in gameObjects )
				{
					foreach( Component comp in GetFilteredComponents( obj ) )
					{
						List<Component> inDict;
						Type type = comp.GetType();
						if( !components.TryGetValue( type, out inDict ) )
						{
							inDict = new List<Component>();
							components[type] = inDict;
						}
						inDict.Add( comp );
					}
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
