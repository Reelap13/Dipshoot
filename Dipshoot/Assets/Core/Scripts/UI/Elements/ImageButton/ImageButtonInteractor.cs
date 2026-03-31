using UnityEngine;

namespace Scripts.UI
{
    public abstract class ImageButtonInteractor : MonoBehaviour
    {
        [SerializeField] private ImageButton _button;

        protected virtual void Awake()
        {
            _button.OnLeftMouseClicked.AddListener(ProcessLeftMouseClick);
            _button.OnRightMouseClicked.AddListener(ProcessRightMouseClick);
            _button.OnCursorEntered.AddListener(ProcessCursorEnter);
            _button.OnCursorExited.AddListener(ProcessCursorExit);
        }

        protected virtual void ProcessLeftMouseClick() { }
        protected virtual void ProcessRightMouseClick() { }
        protected virtual void ProcessCursorEnter() { }
        protected virtual void ProcessCursorExit() { }
    }
}