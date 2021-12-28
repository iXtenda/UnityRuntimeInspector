﻿using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector2Field : InspectorField<Vector2>
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
			inputY.Initialize();

			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;

			inputX.OnValueSubmitted += OnValueSubmitted;
			inputY.OnValueSubmitted += OnValueSubmitted;

			inputX.DefaultEmptyValue = "0";
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
			isVector2Int = m_boundVariableType == typeof( Vector2Int );
			if( isVector2Int )
			{
				inputX.Text = ( (int) BoundValues.x ).ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = ( (int) BoundValues.y ).ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				inputX.Text = BoundValues.x.ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = BoundValues.y.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			bool couldParse;
			float value;

#if UNITY_2017_2_OR_NEWER
			if( isVector2Int )
			{
					couldParse = int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out int intval );
					value = intval;
			}
			else
#endif
			couldParse = float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out value );

			if( couldParse )
			{
				Vector2 val = BoundValues;
				if( source == inputX )
					val.x = value;
				else
					val.y = value;

				BoundValues = val;
				return true;
			}

			return false;
		}

		private bool OnValueSubmitted( BoundInputField source, string input )
		{
			Inspector.RefreshDelayed();
			return OnValueChanged( source, input );
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
			Vector2 prevVal = BoundValues;
			base.Refresh();

#if UNITY_2017_2_OR_NEWER
			if( isVector2Int )
			{
				if( BoundValues.x != prevVal.x )
					inputX.Text = ( (int) BoundValues.x ).ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.y != prevVal.y )
					inputY.Text = ( (int) BoundValues.y ).ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				if( BoundValues.x != prevVal.x )
					inputX.Text = BoundValues.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.y != prevVal.y )
					inputY.Text = BoundValues.y.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}
	}
}
