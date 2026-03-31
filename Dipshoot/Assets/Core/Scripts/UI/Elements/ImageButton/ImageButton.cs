using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Scripts.UI 
{
    public class ImageButton : MonoBehaviour, 
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [NonSerialized] public UnityEvent OnLeftMouseClicked = new();
        [NonSerialized] public UnityEvent OnRightMouseClicked = new();
        [NonSerialized] public UnityEvent OnCursorEntered = new();
        [NonSerialized] public UnityEvent OnCursorExited = new();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnLeftMouseClicked.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightMouseClicked.Invoke();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnCursorEntered.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnCursorExited.Invoke();
        }
    }
}