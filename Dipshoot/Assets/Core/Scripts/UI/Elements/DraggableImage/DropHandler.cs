using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Scripts.UI
{
    public class DropHandler : MonoBehaviour, IDropHandler
    {
        [NonSerialized] public UnityEvent<GameObject> OnDropped = new();

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            OnDropped.Invoke(eventData.pointerDrag);
        }
    }
}