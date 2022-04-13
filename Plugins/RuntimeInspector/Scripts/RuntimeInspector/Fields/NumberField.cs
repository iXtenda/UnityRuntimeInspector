using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using Mode = RuntimeInspectorNamespace.StringField.Mode;

namespace RuntimeInspectorNamespace
{
    public class NumberField : InspectorField<IConvertible>
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
				input.CacheTextOnValueChange = ( m_setterMode & Mode.OnValueChange ) == Mode.OnValueChange;
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
			input.OnValueChanged += OnValueEdited;
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

			if( m_boundVariableType == typeof( float ) || m_boundVariableType == typeof( double ) || m_boundVariableType == typeof( decimal ) )
				input.BackingField.contentType = TMP_InputField.ContentType.DecimalNumber;
			else
				input.BackingField.contentType = TMP_InputField.ContentType.IntegerNumber;

			numberHandler = NumberHandlers.Get( m_boundVariableType );
			UpdateInput();
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();
			SetterMode = default;
		}

		protected bool OnValueEdited( BoundInputField source, string input )
		{
			if( ( m_setterMode & Mode.OnValueChange ) != Mode.OnValueChange )
				return false;
			return OnValueChanged( source, input );
		}

		protected virtual bool OnValueSubmitted( BoundInputField source, string input )
		{
			if( ( m_setterMode & Mode.OnSubmit ) != Mode.OnSubmit )
				return false;
			Inspector.RefreshDelayed();
			return OnValueChanged( source, input );
		}

		protected virtual bool OnValueChanged( BoundInputField source, string input )
		{
			IConvertible value;
			if( numberHandler.TryParse( input, out value ) )
			{
				BoundValues = new IConvertible[] { value }.AsReadOnly();
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
			IConvertible value;
			if( BoundValues.TryGetSingle( out value ) )
			{
				input.Text = numberHandler.ToString( value, format, provider );
				input.HasMultipleValues = false;
			}
			else
			{
				input.HasMultipleValues = true;
			}
		}

		protected override void OnIsInteractableChanged()
		{
			base.OnIsInteractableChanged();
			input.BackingField.interactable = IsInteractable;
			input.BackingField.textComponent.color = this.GetTextColor();
		}
	}
}
