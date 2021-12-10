using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
    public class Vector2Field : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;
#pragma warning restore 0649

#if UNITY_2017_2_OR_NEWER
		private bool isVector2Int;
#endif

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputX.OnValueChanged += ( _, input ) => OnValueChanged( input, 0 );
			inputX.OnValueSubmitted += ( _, input ) => OnValueSubmitted( input, 0 );
			inputX.DefaultEmptyValue = "0";

			inputY.Initialize();
			inputY.OnValueChanged += ( _, input ) => OnValueChanged( input, 1 );
			inputY.OnValueSubmitted += ( _, input ) => OnValueSubmitted( input, 1 );
			inputY.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
#if UNITY_2017_2_OR_NEWER
			if( type == typeof( Vector2Int ) )
				return true;
#endif
			return type == typeof( Vector2 );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );
#if UNITY_2017_2_OR_NEWER
			isVector2Int = BoundVariableType == typeof( Vector2Int );
#endif
			UpdateInputs();
		}

		private bool OnValueChanged( string input, int coordinate )
		{
#if UNITY_2017_2_OR_NEWER
			if( isVector2Int )
				return OnIntChanged( input, coordinate );
#endif
			return OnFloatChanged( input, coordinate );
		}

		private bool OnIntChanged( string input, int coordinate )
		{
			if( !int.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out int value ) )
				return false;

			if( Value is MultiValue multiValue )
			{
				var list = new List<Vector2Int>();
				foreach( Vector2Int oldV in multiValue )
				{
					Vector2Int newV = oldV;
					newV[coordinate] = value;
					list.Add( newV );
				}
				Value = new MultiValue( list );
			}
			else
			{
				Vector2Int newV = (Vector2Int) Value;
				newV[coordinate] = value;
				Value = newV;
			}

			return true;
		}

		private bool OnFloatChanged( string input, int coordinate )
		{
			if( !float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out float value ) )
				return false;

			if( Value is MultiValue multiValue )
			{
				var list = new List<Vector2>();
				foreach( Vector2 oldV in multiValue )
				{
					Vector2 newV = oldV;
					newV[coordinate] = value;
					list.Add( newV );
				}
				Value = new MultiValue( list );
			}
			else
			{
				Vector2 newV = (Vector2) Value;
				newV[coordinate] = value;
				Value = newV;
			}

			return true;
		}

		private void UpdateInputs()
		{
#if UNITY_2017_2_OR_NEWER
			if( isVector2Int )
				UpdateInputsFromInt();
#endif
			UpdateInputsFromFloat();
		}

		private void UpdateInputsFromInt()
		{
			var coords = new int?[3];

			if( Value is MultiValue multiValue )
			{
				int count = 0;
				foreach( Vector2Int v in multiValue )
				{
					count++;
					if( count == 1 )
					{
						for( int i = 0; i < coords.Length; i++ )
							coords[i] = v[i];
						continue;
					}

					for( int i = 0; i < coords.Length; i++ )
					{
						float? coord = coords[i];
						if( coord.HasValue )
							if( coord.Value != v[i] )
								coords[i] = null;
					}
				}
			}
			else
			{
				var v = (Vector2Int) Value;
				for( int i = 0; i < coords.Length; i++ )
					coords[i] = v[i];
			}

			inputX.HasMultipleValues = !coords[0].HasValue;
			inputY.HasMultipleValues = !coords[1].HasValue;

			if( coords[0].HasValue )
				inputX.Text = coords[0].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[1].HasValue )
				inputY.Text = coords[1].Value.ToString( RuntimeInspectorUtils.numberFormat );
		}

		private void UpdateInputsFromFloat()
		{
			var coords = new float?[2];

			if( Value is MultiValue multiValue )
			{
				int count = 0;
				foreach( Vector2 v in multiValue )
				{
					count++;
					if( count == 1 )
					{
						for( int i = 0; i < coords.Length; i++ )
							coords[i] = v[i];
						continue;
					}

					for( int i = 0; i < coords.Length; i++ )
					{
						float? coord = coords[i];
						if( coord.HasValue )
							if( coord.Value != v[i] )
								coords[i] = null;
					}
				}
			}
			else
			{
				var v = (Vector2) Value;
				for( int i = 0; i < coords.Length; i++ )
					coords[i] = v[i];
			}

			inputX.HasMultipleValues = !coords[0].HasValue;
			inputY.HasMultipleValues = !coords[1].HasValue;

			if( coords[0].HasValue )
				inputX.Text = coords[0].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[1].HasValue )
				inputY.Text = coords[1].Value.ToString( RuntimeInspectorUtils.numberFormat );
		}

		private bool OnValueSubmitted( string input, int coordinate )
		{
			Inspector.RefreshDelayed();
			return OnValueChanged( input, coordinate );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			labelX.SetSkinText( Skin );
			labelY.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;

			float inputFieldWidth = ( 1f - Skin.LabelWidthPercentage ) / 3f;
			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage + inputFieldWidth, 0f );
			Vector2 rightSideAnchorMax = new Vector2( Skin.LabelWidthPercentage + 2f * inputFieldWidth, 1f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) inputX.transform ).SetAnchorMinMaxInputField( labelX.rectTransform, rightSideAnchorMin, rightSideAnchorMax );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x = 1f;
			( (RectTransform) inputY.transform ).SetAnchorMinMaxInputField( labelY.rectTransform, rightSideAnchorMin, rightSideAnchorMax );
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

			inputX.BackingField.textComponent.color = textColor;
			inputY.BackingField.textComponent.color = textColor;

			labelX.color = textColor;
			labelY.color = textColor;
		}
	}
}
