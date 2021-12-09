using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
    public class Vector4Field : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private BoundInputField inputZ;

		[SerializeField]
		private BoundInputField inputW;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelZ;

		[SerializeField]
		private Text labelW;
#pragma warning restore 0649

#if UNITY_2017_2_OR_NEWER
		private bool isVector4Int;
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

			inputZ.Initialize();
			inputZ.OnValueChanged += ( _, input ) => OnValueChanged( input, 2 );
			inputZ.OnValueSubmitted += ( _, input ) => OnValueSubmitted( input, 2 );
			inputZ.DefaultEmptyValue = "0";

			inputW.Initialize();
			inputW.OnValueChanged += ( _, input ) => OnValueChanged( input, 3 );
			inputW.OnValueSubmitted += ( _, input ) => OnValueSubmitted( input, 3 );
			inputW.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Vector4 );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );
			UpdateInputs();
		}

		private bool OnValueChanged( string input, int coordinate )
		{
			if( !float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out float value ) )
				return false;

			if( Value is MultiValue multiValue )
			{
				var list = new List<Vector4>();
				foreach( Vector4 oldV in multiValue )
				{
					Vector4 newV = oldV;
					newV[coordinate] = value;
					list.Add( newV );
				}
				Value = new MultiValue( list );
			}
			else
			{
				Vector4 newV = (Vector4) Value;
				newV[coordinate] = value;
				Value = newV;
			}

			return true;
		}

		private void UpdateInputs()
		{
			var coords = new float?[4];

			if( Value is MultiValue multiValue )
			{
				int count = 0;
				foreach( Vector4 v in multiValue )
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
				var v = (Vector4) Value;
				for( int i = 0; i < coords.Length; i++ )
					coords[i] = v[i];
			}

			inputX.HasMultipleValues = !coords[0].HasValue;
			inputY.HasMultipleValues = !coords[1].HasValue;
			inputZ.HasMultipleValues = !coords[2].HasValue;
			inputW.HasMultipleValues = !coords[3].HasValue;

			if( coords[0].HasValue )
				inputX.Text = coords[0].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[1].HasValue )
				inputY.Text = coords[1].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[2].HasValue )
				inputZ.Text = coords[2].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[3].HasValue )
				inputW.Text = coords[3].Value.ToString( RuntimeInspectorUtils.numberFormat );
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
			labelZ.SetSkinText( Skin );
			labelW.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;
			inputZ.Skin = Skin;
			inputW.Skin = Skin;

			float inputFieldWidth = ( 1f - Skin.LabelWidthPercentage ) / 3f;
			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage + inputFieldWidth, 0f );
			Vector2 rightSideAnchorMax = new Vector2( Skin.LabelWidthPercentage + 2f * inputFieldWidth, 1f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) inputX.transform ).SetAnchorMinMaxInputField( labelX.rectTransform, new Vector2( rightSideAnchorMin.x, 0.5f ), rightSideAnchorMax );
			( (RectTransform) inputZ.transform ).SetAnchorMinMaxInputField( labelZ.rectTransform, rightSideAnchorMin, new Vector2( rightSideAnchorMax.x, 0.5f ) );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x = 1f;
			( (RectTransform) inputY.transform ).SetAnchorMinMaxInputField( labelY.rectTransform, new Vector2( rightSideAnchorMin.x, 0.5f ), rightSideAnchorMax );
			( (RectTransform) inputW.transform ).SetAnchorMinMaxInputField( labelW.rectTransform, rightSideAnchorMin, new Vector2( rightSideAnchorMax.x, 0.5f ) );
		}

		public override void Refresh()
		{
			base.Refresh();
			UpdateInputs();
		}
	}
}
