using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class DragListener : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		public delegate void DragEvent( PointerEventData eventData );

		public event DragEvent BeginDrag, Drag, EndDrag;

		void IBeginDragHandler.OnBeginDrag( PointerEventData eventData )
		{
			if( BeginDrag != null )
				BeginDrag.Invoke( eventData );
		}

		void IDragHandler.OnDrag( PointerEventData eventData )
		{
			if( Drag != null )
				Drag.Invoke( eventData );
		}

		void IEndDragHandler.OnEndDrag( PointerEventData eventData )
		{
			if( EndDrag != null )
				EndDrag.Invoke( eventData );
		}
	}
}
