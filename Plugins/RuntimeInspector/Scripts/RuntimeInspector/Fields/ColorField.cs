using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class ColorField : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private RectTransform colorPickerArea;

		[SerializeField]
		private PointerEventListener inputColor;

		[SerializeField]
		private Text multiValueText;

		private Image colorImg;
#pragma warning restore 0649

		private bool isColor32;

		public override void Initialize()
		{
			base.Initialize();

			colorImg = inputColor.GetComponent<Image>();
			inputColor.PointerClick += ShowColorPicker;
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Color ) || type == typeof( Color32 );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );
			isColor32 = BoundVariableType == typeof( Color32 );
		}

		private void ShowColorPicker( PointerEventData eventData )
		{
			if( !IsInteractable )
				return;

			Color initial;
			object initialValue = Value;

			if( HasMultipleValues )
				initial = Color.white;
			else
				initial = isColor32 ? (Color) (Color32) Value : (Color) Value;

			ColorPicker.Instance.Skin = Inspector.Skin;
			ColorPicker.Instance.Show(
				OnColorChanged,
				null,
				initial,
				Inspector.Canvas,
				() => Value = initialValue );
		}

		private void OnColorChanged( Color32 color )
		{
			colorImg.color = color;

			if( isColor32 )
				Value = color;
			else
				Value = (Color) color;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			colorPickerArea.anchorMin = rightSideAnchorMin;
			multiValueText.SetSkinInputFieldText( Skin );
		}

		public override void Refresh()
		{
			base.Refresh();

			if( HasMultipleValues )
			{
				colorImg.color = Skin.InputFieldNormalBackgroundColor;
				multiValueText.enabled = true;
				return;
			}

			multiValueText.enabled = false;

			if( isColor32 )
				colorImg.color = (Color32) Value;
			else
				colorImg.color = (Color) Value;
		}
	}
}
