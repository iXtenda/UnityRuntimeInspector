using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Mode = RuntimeInspectorNamespace.StringField.Mode;

namespace RuntimeInspectorNamespace
{
	public class Vector3Field : InspectorField<Vector3>
	{
#pragma warning disable 0649
		[SerializeField]
		protected BoundInputField inputX;

		[SerializeField]
		protected BoundInputField inputY;

		[SerializeField]
		protected BoundInputField inputZ;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelZ;
#pragma warning restore 0649

#if UNITY_2017_2_OR_NEWER
		private bool isVector3Int;
#endif

		private Mode m_setterMode = Mode.OnValueChange;
		public Mode SetterMode
		{
			get { return m_setterMode; }
			set
			{
				m_setterMode = value;
				inputX.CacheTextOnValueChange = value == Mode.OnValueChange;
				inputY.CacheTextOnValueChange = value == Mode.OnValueChange;
				inputZ.CacheTextOnValueChange = value == Mode.OnValueChange;
			}
		}

		public IFormatProvider provider = RuntimeInspectorUtils.numberFormat;
		public string format = NumberField.DEFAULT_FORMAT;

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();
			inputZ.Initialize();

			inputX.OnValueChanged += OnValueEdited;
			inputY.OnValueChanged += OnValueEdited;
			inputZ.OnValueChanged += OnValueEdited;

			inputX.OnValueSubmitted += OnValueSubmitted;
			inputY.OnValueSubmitted += OnValueSubmitted;
			inputZ.OnValueSubmitted += OnValueSubmitted;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
			inputZ.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
#if UNITY_2017_2_OR_NEWER
			if( type == typeof( Vector3Int ) )
				return true;
#endif
			return type == typeof( Vector3 );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

#if UNITY_2017_2_OR_NEWER
			isVector3Int = m_boundVariableType == typeof( Vector3Int );
#endif
			UpdateInputs();
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();
			SetterMode = Mode.OnValueChange;
		}

		private void UpdateInputs()
		{
			float?[] coords = BoundValues
				.Select( RuntimeInspectorUtils.ToArray )
				.SinglePerEntry();

			inputX.HasMultipleValues = !coords[0].HasValue;
			inputY.HasMultipleValues = !coords[1].HasValue;
			inputZ.HasMultipleValues = !coords[2].HasValue;

#if UNITY_2017_2_OR_NEWER
			if( isVector3Int )
				UpdateInputTexts( coords.Cast<float?, int?>() );
			else
#endif
				UpdateInputTexts( coords );
		}

		private void UpdateInputTexts<T>( IList<T?> coords ) where T : struct, IConvertible
		{
			if( coords[0].HasValue )
				inputX.Text = coords[0].Value.ToString( provider );
			if( coords[1].HasValue )
				inputY.Text = coords[1].Value.ToString( provider );
			if( coords[2].HasValue )
				inputZ.Text = coords[2].Value.ToString( provider );
		}

		private void UpdateInputTexts( IList<float?> coords )
		{
			if( coords[0].HasValue )
				inputX.Text = coords[0].Value.ToString( format, provider );
			if( coords[1].HasValue )
				inputY.Text = coords[1].Value.ToString( format, provider );
			if( coords[2].HasValue )
				inputZ.Text = coords[2].Value.ToString( format, provider );
		}

		private bool OnValueEdited( BoundInputField source, string input )
		{
			if( m_setterMode != Mode.OnValueChange )
				return false;
			return OnValueChanged( source, input );
		}

		private bool OnValueSubmitted( BoundInputField source, string input )
		{
			if( m_setterMode != Mode.OnSubmit )
				return false;
			return OnValueChanged( source, input );
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			bool couldParse;
			float value;

#if UNITY_2017_2_OR_NEWER
			if( isVector3Int )
			{
					int intval;
					couldParse = int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out intval );
					value = intval;
			}
			else
#endif
			couldParse = float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out value );

			if( !couldParse )
					return false;

			int coord;
			if( source == inputX )
					coord = 0;
			else if( source == inputY )
					coord = 1;
			else
					coord = 2;

			var newVs = new List<Vector3>();
			foreach( Vector3 oldV in BoundValues )
			{
				Vector3 newV = oldV;
				newV[coord] = value;
				newVs.Add( newV );
			}

			BoundValues = newVs.AsReadOnly();
			return true;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			labelX.SetSkinText( Skin );
			labelY.SetSkinText( Skin );
			labelZ.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;
			inputZ.Skin = Skin;

			float inputFieldWidth = ( 1f - Skin.LabelWidthPercentage ) / 3f;
			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			Vector2 rightSideAnchorMax = new Vector2( Skin.LabelWidthPercentage + inputFieldWidth, 1f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) inputX.transform ).SetAnchorMinMaxInputField( labelX.rectTransform, rightSideAnchorMin, rightSideAnchorMax );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x += inputFieldWidth;
			( (RectTransform) inputY.transform ).SetAnchorMinMaxInputField( labelY.rectTransform, rightSideAnchorMin, rightSideAnchorMax );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x = 1f;
			( (RectTransform) inputZ.transform ).SetAnchorMinMaxInputField( labelZ.rectTransform, rightSideAnchorMin, rightSideAnchorMax );
		}

		public override void Refresh()
		{
			base.Refresh();
			UpdateInputs();
		}

		protected override void OnIsInteractableChanged()
		{
			base.OnIsInteractableChanged();
			Color textColor = this.GetTextColor();

			inputX.BackingField.interactable = IsInteractable;
			inputY.BackingField.interactable = IsInteractable;
			inputZ.BackingField.interactable = IsInteractable;

			inputX.BackingField.textComponent.color = textColor;
			inputY.BackingField.textComponent.color = textColor;
			inputZ.BackingField.textComponent.color = textColor;

			labelX.color = textColor;
			labelY.color = textColor;
			labelZ.color = textColor;
		}
	}
}
