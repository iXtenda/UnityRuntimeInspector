using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using System.Collections;
using Mode = RuntimeInspectorNamespace.StringField.Mode;

namespace RuntimeInspectorNamespace
{
	public class NumberField : InspectorField
	{
		private static readonly HashSet<Type> supportedTypes = new HashSet<Type>()
		{
			typeof( int ), typeof( uint ), typeof( long ), typeof( ulong ),
			typeof( byte ), typeof( sbyte ), typeof( short ), typeof( ushort ), typeof( char ),
			typeof( float ), typeof( double ), typeof( decimal )
		};

#pragma warning disable 0649
		[SerializeField]
		protected BoundInputField input;
#pragma warning restore 0649

		private Mode m_setterMode = Mode.OnValueChange;
		public Mode SetterMode
		{
			get { return m_setterMode; }
			set
			{
				m_setterMode = value;
				input.CacheTextOnValueChange = m_setterMode == Mode.OnValueChange;
			}
		}

		protected INumberHandler numberHandler;
		public IFormatProvider provider = RuntimeInspectorUtils.numberFormat;
		public const string DEFAULT_FORMAT = "0.######";
		public string format = DEFAULT_FORMAT;

		public override void Initialize()
		{
			base.Initialize();

			input.Initialize();
			input.OnValueChanged += OnValueChanged;
			input.OnValueSubmitted += OnValueSubmitted;
			input.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return supportedTypes.Contains( type );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

			if( input.BackingField.contentType != TMP_InputField.ContentType.Custom )
			{
				if( BoundVariableType == typeof( float ) || BoundVariableType == typeof( double ) || BoundVariableType == typeof( decimal ) )
					input.BackingField.contentType = TMP_InputField.ContentType.DecimalNumber;
				else
					input.BackingField.contentType = TMP_InputField.ContentType.IntegerNumber;
			}

			numberHandler = NumberHandlers.Get( BoundVariableType );
			UpdateInput();
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();
			SetterMode = default;
		}

		protected virtual bool OnValueChanged( BoundInputField source, string input )
		{
			if( m_setterMode != Mode.OnValueChange )
				return false;
			return ApplyValue( input );
		}

		private bool OnValueSubmitted( BoundInputField source, string input )
		{
			if( m_setterMode != Mode.OnSubmit )
				return false;
			Inspector.RefreshDelayed();
			return ApplyValue( input );
		}

		private bool ApplyValue( string input )
		{
			object value;
			if( numberHandler.TryParse( input, out value ) )
			{
				Value = value;
				return true;
			}

			return false;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			input.Skin = Skin;

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) input.transform ).anchorMin = rightSideAnchorMin;
		}

		public override void Refresh()
		{
			base.Refresh();
			UpdateInput();
		}

		private void UpdateInput()
		{
			object first = null;

			// Regard "approximate equality" for float and double used in
			// NumberHandlers
			if( Value is IEnumerable enumerable )
			{
				foreach( object f in enumerable )
				{
					if( first == null )
					{
						first = f;
						continue;
					}

					if( !numberHandler.ValuesAreEqual( first, f ) )
					{
						input.HasMultipleValues = true;
						return;
					}
				}
			}

			if( first == null )
				first = Value;

			input.HasMultipleValues = false;
			input.Text = numberHandler.ToString( first, format, provider );
		}

		protected override void OnIsInteractableChanged()
		{
			base.OnIsInteractableChanged();
			input.BackingField.interactable = IsInteractable;
			input.BackingField.textComponent.color = this.GetTextColor();
		}
	}
}
