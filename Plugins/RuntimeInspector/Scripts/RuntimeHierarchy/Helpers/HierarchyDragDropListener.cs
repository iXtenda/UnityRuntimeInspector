using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDragDropListener : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
	{
		private const float POINTER_VALIDATE_INTERVAL = 5f;

#pragma warning disable 0649
		[SerializeField]
		private float siblingIndexModificationArea = 5f;

		[SerializeField]
		private float scrollableArea = 75f;
		private float _1OverScrollableArea;

		[SerializeField]
		private float scrollSpeed = 75f;

		[SerializeField]
		private bool canDropParentOnChild = false;

		[SerializeField]
		private bool canAddObjectsToPseudoScenes = false;

		[Header( "Internal Variables" )]
		[SerializeField]
		private RuntimeHierarchy hierarchy;

		[SerializeField]
		private RectTransform content;

		[SerializeField]
		private Image dragDropTargetVisualization;
#pragma warning restore 0649

		private Canvas canvas;

		private RectTransform rectTransform;
		private float height;

		private PointerEventData pointer;
		private Camera worldCamera;

		private float pointerLastYPos;
		private float nextPointerValidation;

		private void Start()
		{
			rectTransform = (RectTransform) transform;
			canvas = hierarchy.GetComponentInParent<Canvas>();
			_1OverScrollableArea = 1f / scrollableArea;
		}

		private void OnRectTransformDimensionsChange()
		{
			height = 0f;
		}

		private void Update()
		{
			if( pointer == null )
				return;

			nextPointerValidation -= Time.unscaledDeltaTime;
			if( nextPointerValidation <= 0f )
			{
				nextPointerValidation = POINTER_VALIDATE_INTERVAL;

				if( !pointer.IsPointerValid() )
				{
					pointer = null;
					return;
				}
			}

			Vector2 position;
			if( RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, pointer.position, worldCamera, out position ) && position.y != pointerLastYPos )
			{
				pointerLastYPos = -position.y;

				if( height <= 0f )
					height = rectTransform.rect.height;

				// Scroll the hierarchy when hovering near top or bottom edges
				float scrollAmount = 0f;
				float viewportYPos = pointerLastYPos;
				if( pointerLastYPos < scrollableArea )
					scrollAmount = ( scrollableArea - pointerLastYPos ) * _1OverScrollableArea;
				else if( pointerLastYPos > height - scrollableArea )
					scrollAmount = ( height - scrollableArea - viewportYPos ) * _1OverScrollableArea;

				float contentYPos = pointerLastYPos + content.anchoredPosition.y;
				if( contentYPos < 0f )
				{
					if( dragDropTargetVisualization.gameObject.activeSelf )
						dragDropTargetVisualization.gameObject.SetActive( false );

					hierarchy.AutoScrollSpeed = 0f;
				}
				else
				{
					if( contentYPos < hierarchy.ItemCount * hierarchy.Skin.LineHeight )
					{
						// Show a visual feedback of where the dragged object would be dropped to
						if( !dragDropTargetVisualization.gameObject.activeSelf )
						{
							dragDropTargetVisualization.rectTransform.SetAsLastSibling();
							dragDropTargetVisualization.gameObject.SetActive( true );
						}

						float relativePosition = contentYPos % hierarchy.Skin.LineHeight;
						float absolutePosition = -contentYPos + relativePosition;
						if( relativePosition < siblingIndexModificationArea )
						{
							// Dragged object will be dropped above the target
							dragDropTargetVisualization.rectTransform.anchoredPosition = new Vector2( 0f, absolutePosition + 2f );
							dragDropTargetVisualization.rectTransform.sizeDelta = new Vector2( 20f, 4f ); // 20f: The visualization extends beyond scrollbar
						}
						else if( relativePosition > hierarchy.Skin.LineHeight - siblingIndexModificationArea )
						{
							// Dragged object will be dropped below the target
							dragDropTargetVisualization.rectTransform.anchoredPosition = new Vector2( 0f, absolutePosition - hierarchy.Skin.LineHeight + 2f );
							dragDropTargetVisualization.rectTransform.sizeDelta = new Vector2( 20f, 4f );
						}
						else
						{
							// Dragged object will be dropped onto the target
							dragDropTargetVisualization.rectTransform.anchoredPosition = new Vector2( 0f, absolutePosition );
							dragDropTargetVisualization.rectTransform.sizeDelta = new Vector2( 20f, hierarchy.Skin.LineHeight );
						}
					}
					else if( dragDropTargetVisualization.gameObject.activeSelf )
						dragDropTargetVisualization.gameObject.SetActive( false );

					hierarchy.AutoScrollSpeed = scrollAmount * scrollSpeed;
				}
			}
		}

		void IDropHandler.OnDrop( PointerEventData eventData )
		{
			( (IPointerExitHandler) this ).OnPointerExit( eventData );

			if( !hierarchy.CanReorganizeItems || hierarchy.IsInSearchMode )
				return;

			HashSet<Object> droppedObjects = RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem( eventData, typeof( Transform ) );
			if( droppedObjects.IsNullOrEmpty() )
				return;

			var droppedTransforms = droppedObjects.Cast<Transform>();

			int newSiblingIndex = -1;
			bool shouldFocusObjectInHierarchy = false;

			float contentYPos = pointerLastYPos + content.anchoredPosition.y;
			int dataIndex = (int) contentYPos / hierarchy.Skin.LineHeight;
			HierarchyData target = hierarchy.GetDataAt( dataIndex );

			if( target == null )
			{
				// Dropped object onto the blank space at the bottom of the Hierarchy
				foreach( var item in droppedTransforms )
				{
					if( item.parent == null )
						continue;

					item.SetParent( null, true );
					shouldFocusObjectInHierarchy = true;
				}

				if( !shouldFocusObjectInHierarchy )
					return;
			}
			else
			{
				int insertDirection;
				float relativePosition = contentYPos % hierarchy.Skin.LineHeight;
				if( relativePosition < siblingIndexModificationArea )
					insertDirection = -1;
				else if( relativePosition > hierarchy.Skin.LineHeight - siblingIndexModificationArea )
					insertDirection = 1;
				else
					insertDirection = 0;

				HierarchyDataRoot newScene = null;
				HierarchyDataTransform dataTransform = null;
				if( !( target is HierarchyDataTransform ) )
				{
					// Dropped onto a scene or pseudo-scene
					newScene = (HierarchyDataRoot) target;
				}
				else
				{
					// Dropped onto a Transform
					dataTransform = (HierarchyDataTransform) target;
					Transform newParent = dataTransform.BoundTransform;

					// Dropped onto itself, ignore
					if( !newParent || ( droppedTransforms.Count() == 1 && droppedTransforms.First() == newParent ) )
						return;

					if( insertDirection != 0 )
					{
						if( insertDirection > 0 && target.Height > 1 )
						{
							// Dropped below an expanded Transform, make dropped object a child of it
							newSiblingIndex = 0;
						}
						else if( target.Depth == 1 && target.Root is HierarchyDataRootPseudoScene pseudoScene )
						{
							// Dropped above or below a root pseudo-scene object, don't actually change the parent
							if( insertDirection < 0 )
								newSiblingIndex = pseudoScene.IndexOf( newParent );
							else
								newSiblingIndex = pseudoScene.IndexOf( newParent ) + 1;

							newParent = null;
						}
						else
						{
							// To be able to drop the object at that sibling index, object's parent must also be changed
							newParent = newParent.parent;
						}
					}

					if( !newParent )
						newScene = target.Root;
					else
					{
						if( !canDropParentOnChild || droppedTransforms.Count() > 1 )
						{
							// Avoid setting child object as parent of the parent object
							if( droppedTransforms.Any( t => newParent.IsChildOf( t ) ) )
								return;
						}
						else
						{
							Transform droppedTransform = droppedTransforms.First();
							// First, set the child object's parent as dropped object's current parent so that
							// the dropped object can then become a child of the former child object
							Transform curr = newParent;
							while( curr.parent != null && curr.parent != droppedTransform )
								curr = curr.parent;

							if( curr.parent == droppedTransform )
							{
								if( droppedTransform.parent == null && target.Root is HierarchyDataRootPseudoScene pseudoScene )
								{
									// Dropped object was a root pseudo-scene object, swap the child and parent objects in the pseudo-scene, as well
									if( !canAddObjectsToPseudoScenes )
										return;

									pseudoScene.InsertChild( pseudoScene.IndexOf( newParent ), curr );
									pseudoScene.RemoveChild( newParent );
								}

								int siblingIndex = droppedTransform.GetSiblingIndex();
								curr.SetParent( droppedTransform.parent, true );
								curr.SetSiblingIndex( siblingIndex );

								shouldFocusObjectInHierarchy = true;
							}
						}

						foreach( var item in droppedTransforms )
						{
							item.SetParent( newParent, true );
							item.SetAsLastSibling();
						}
					}
				}

				if( newScene != null )
				{
					// Inserting above/below a scene or pseudo-scene is a special case
					if( insertDirection != 0 && !( target is HierarchyDataTransform ) )
					{
						if( insertDirection < 0 && dataIndex > 0 )
						{
							// In Hierarchy AB, if inserting above B, then instead insert below A; it is easier for calculations
							HierarchyData _target = hierarchy.GetDataAt( dataIndex - 1 );
							if( _target != null )
							{
								target = _target;
								insertDirection = 1;
							}
						}
						else if( insertDirection > 0 && dataIndex < hierarchy.ItemCount - 1 )
						{
							// In Hierarchy AB, if inserting below A, then instead insert above B if B is a Transform; it is easier for calculations
							HierarchyData _target = hierarchy.GetDataAt( dataIndex + 1 );
							if( _target != null && _target is HierarchyDataTransform )
							{
								target = _target;
								insertDirection = -1;
							}
						}
					}

					if( newScene is HierarchyDataRootPseudoScene pseudoScene )
					{
						if( !canAddObjectsToPseudoScenes )
							return;

						// Add object to pseudo-scene
						if( newSiblingIndex < 0 )
							pseudoScene.AddChildren( droppedTransforms );
						else
						{
							pseudoScene.InsertChildren( newSiblingIndex, droppedTransforms );

							// Don't try to change the actual sibling index of the Transform
							newSiblingIndex = -1;
							target = newScene;
						}
					}
					else if( newScene is HierarchyDataRootScene rootScene )
					{
						// Change dropped object's scene
						Scene scene = rootScene.Scene;

						foreach( var item in droppedTransforms )
						{
							// Only root GameObject's can be moved
							if( item.parent != null )
								item.SetParent( null, true );

							if( item.gameObject.scene != scene )
								SceneManager.MoveGameObjectToScene( item.gameObject, scene );

							item.SetAsLastSibling();
						}

						if( newSiblingIndex < 0 && insertDirection == 0)
						{
							// If object was dropped onto the scene, add it to the bottom of the scene
							newSiblingIndex = scene.rootCount + 1;
							shouldFocusObjectInHierarchy = true;
						}
					}
				}

				if( newSiblingIndex == -1 && dataTransform != null )
				{
					newSiblingIndex = insertDirection switch
					{
						-1 => dataTransform.BoundTransform.GetSiblingIndex(),
						0 => 0,
						1 => dataTransform.BoundTransform.GetSiblingIndex() + 1,
						_ => throw new System.NotSupportedException( "Insert Direction must be a value between -1 and 1" )
					};
				}

				if( newSiblingIndex >= 0 )
				{
					// TODO this does not guarantee the correct order of the dropped items
					foreach( var item in droppedTransforms )
					{
						item.SetSiblingIndex( newSiblingIndex );
					}
				}
			}

			// Selecting the object in Hierarchy automatically expands collapsed parent entries and snaps the scroll view to the
			// selected object. However, this snapping can be distracting, so don't select the object unless it is necessary
			if( shouldFocusObjectInHierarchy || ( newSiblingIndex < 0 && !target.IsExpanded ) )
				hierarchy.Select( droppedTransforms, true );
			else
				hierarchy.Refresh();
		}

		void IPointerEnterHandler.OnPointerEnter( PointerEventData eventData )
		{
			if( !hierarchy.CanReorganizeItems || hierarchy.IsInSearchMode )
				return;

			if( RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem( eventData, typeof( Transform ) ).IsNullOrEmpty() )
				return;

			pointer = eventData;
			pointerLastYPos = -1f;
			nextPointerValidation = POINTER_VALIDATE_INTERVAL;

			if( canvas.renderMode == RenderMode.ScreenSpaceOverlay || ( canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null ) )
				worldCamera = null;
			else
				worldCamera = canvas.worldCamera ? canvas.worldCamera : Camera.main;

			Update();
		}

		void IPointerExitHandler.OnPointerExit( PointerEventData eventData )
		{
			pointer = null;
			worldCamera = null;

			if( dragDropTargetVisualization.gameObject.activeSelf )
				dragDropTargetVisualization.gameObject.SetActive( false );

			hierarchy.AutoScrollSpeed = 0f;
		}
	}
}