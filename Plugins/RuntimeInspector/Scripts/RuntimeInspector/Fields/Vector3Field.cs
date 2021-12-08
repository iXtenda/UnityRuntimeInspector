using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector3Field : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private BoundInputField inputZ;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelZ;
#pragma warning restore 0649

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();
			inputZ.Initialize();

			inputX.OnValueChanged += ( _, input ) => OnValueChanged( input, 0 );
			inputX.OnValueSubmitted += ( _, input ) => OnValueSubmitted( input, 0 );

			inputY.OnValueChanged += ( _, input ) => OnValueChanged( input, 1 );
			inputY.OnValueSubmitted += ( _, input ) => OnValueSubmitted( input, 1 );

			inputZ.OnValueChanged += ( _, input ) => OnValueChanged( input, 2 );
			inputZ.OnValueSubmitted += ( _, input ) => OnValueSubmitted( input, 2 );

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
			inputZ.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Vector3 );
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
				var list = new List<Vector3>();
				foreach( Vector3 oldV in multiValue )
				{
					Vector3 newV = oldV;
					newV[coordinate] = value;
					list.Add( newV );
				}
				Value = new MultiValue( list );
			}
			else
			{
				Vector3 newV = (Vector3) Value;
				newV[coordinate] = value;
				Value = newV;
			}

			return true;
		}

		protected virtual void UpdateInputs()
		{
			var coords = new float?[3];

			if( Value is MultiValue multiValue )
			{
				int count = 0;
				foreach( Vector3 v in multiValue )
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
				var v = (Vector3) Value;
				for( int i = 0; i < coords.Length; i++ )
					coords[i] = v[i];
			}

			inputX.HasMultipleValues = !coords[0].HasValue;
			inputY.HasMultipleValues = !coords[1].HasValue;
			inputZ.HasMultipleValues = !coords[2].HasValue;

			if( coords[0].HasValue )
				inputX.Text = coords[0].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[1].HasValue )
				inputY.Text = coords[1].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[2].HasValue )
				inputZ.Text = coords[2].Value.ToString( RuntimeInspectorUtils.numberFormat );
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
	}
}
