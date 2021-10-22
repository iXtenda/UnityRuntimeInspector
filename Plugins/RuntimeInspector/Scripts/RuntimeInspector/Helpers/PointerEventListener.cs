using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class PointerEventListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
	{
		public delegate void PointerEvent( PointerEventData eventData );

		public event PointerEvent PointerDown, PointerUp, PointerClick;
		public bool reactToLeftClick = true;
		public bool reactToRightClick = true;
		public bool reactToMiddleClick = true;

		bool IsReactingToButton( PointerEventData eventData )
			=> eventData.button == PointerEventData.InputButton.Left   && reactToLeftClick
			|| eventData.button == PointerEventData.InputButton.Right  && reactToRightClick
			|| eventData.button == PointerEventData.InputButton.Middle && reactToMiddleClick;

		void IPointerDownHandler.OnPointerDown( PointerEventData eventData )
		{
			if( PointerDown != null && IsReactingToButton( eventData ) )
				PointerDown( eventData );
		}

		void IPointerUpHandler.OnPointerUp( PointerEventData eventData )
		{
			if( PointerDown != null && IsReactingToButton( eventData ) )
				PointerUp( eventData );
		}

		void IPointerClickHandler.OnPointerClick( PointerEventData eventData )
		{
			if( PointerClick != null && IsReactingToButton( eventData ) )
				PointerClick( eventData );
		}
	}
}
