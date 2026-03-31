using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Scripts.UI
{
    public class DraggableImage : MonoBehaviour,
        IPointerDownHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [NonSerialized] public UnityEvent<PointerEventData> OnPointerDownEvent = new();
        [NonSerialized] public UnityEvent<PointerEventData> OnBeginDragEvent = new();
        [NonSerialized] public UnityEvent<PointerEventData> OnDragEvent = new();
        [NonSerialized] public UnityEvent<PointerEventData> OnEndDragEvent = new();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                OnPointerDownEvent.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragEvent.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnDragEvent.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragEvent.Invoke(eventData);
        }
    }
}