using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts.UI
{
    public abstract class DragInteractor : MonoBehaviour
    {
        [SerializeField] private DraggableImage _draggable;

        protected RectTransform _rect;
        protected Canvas _canvas;
        protected Transform _original_parent;
        protected CanvasGroup _canvas_group;

        protected virtual void Awake()
        {
            _rect = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            _canvas_group = _draggable.GetComponent<CanvasGroup>();

            _draggable.OnBeginDragEvent.AddListener(OnBeginDragInternal);
            _draggable.OnDragEvent.AddListener(OnDragInternal);
            _draggable.OnEndDragEvent.AddListener(OnEndDragInternal);
        }

        protected void OnBeginDragInternal(PointerEventData eventData)
        {
            _original_parent = transform.parent;

            transform.SetParent(_canvas.transform);
            transform.SetAsLastSibling();

            _canvas_group.blocksRaycasts = false;

            OnBeginDrag(eventData);
        }
        private void OnDragInternal(PointerEventData eventData)
        {
            Vector3 worldPos;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _rect,
                eventData.position,
                eventData.pressEventCamera,
                out worldPos))
            {
                _rect.position = worldPos;
            }

            OnDrag(eventData);
        }

        private void OnEndDragInternal(PointerEventData eventData)
        {
            _canvas_group.blocksRaycasts = true;

            OnEndDrag(eventData);
        }

        protected void ReturnToOriginal()
        {
            transform.SetParent(_original_parent);
            _rect.anchoredPosition = Vector2.zero;
        }

        protected virtual void OnBeginDrag(PointerEventData eventData) { }
        protected virtual void OnDrag(PointerEventData eventData) { }
        protected virtual void OnEndDrag(PointerEventData eventData) { }
    }
}