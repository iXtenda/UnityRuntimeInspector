using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class BoolField : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private Image toggleBackground;

		[SerializeField]
		private Toggle input;

		[SerializeField]
		private Image multiValuesMark;
#pragma warning restore 0649

		public override void Initialize()
		{
			base.Initialize();
			input.onValueChanged.AddListener( OnValueChanged );
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( bool );
		}

		private void OnValueChanged( bool input )
		{
			if( HasMultipleValues )
				Value = true;
			else
				Value = input;

			Inspector.RefreshDelayed();
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );
			if( !HasMultipleValues )
				input.SetIsOnWithoutNotify( (bool) Value );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			toggleBackground.color = Skin.InputFieldNormalBackgroundColor;
			input.graphic.color = Skin.ToggleCheckmarkColor;
			multiValuesMark.color = Skin.ToggleCheckmarkColor;

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) input.transform ).anchorMin = rightSideAnchorMin;
		}

		private void SwitchMarks( bool hasMultipleValues )
		{
			input.graphic.enabled = !hasMultipleValues;
			multiValuesMark.enabled = hasMultipleValues;
		}

		public override void Refresh()
		{
			base.Refresh();
			if( Value is bool b )
			{
				if( input.isOn != b )
					input.isOn = b;
				SwitchMarks( false );
			}
			else
			{
				SwitchMarks( true );
			}
		}

		protected override void OnIsInteractableChanged()
		{
			base.OnIsInteractableChanged();
			input.interactable = IsInteractable;
			input.graphic.color = this.GetTextColor();
		}
	}
}
