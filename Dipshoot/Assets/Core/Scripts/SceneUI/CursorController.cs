using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts.UI.SceneUI
{
    public class CursorController : MonoBehaviour
    {
        private int _count;

        private void Awake()
        {
            SetDefault();
        }

        public void Show()
        {
            ++_count;
            UpdateCursorState();
        }

        public void Hide()
        {
            --_count;
            UpdateCursorState();
        }

        private void UpdateCursorState()
        {
            if (_count > 0)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            { 
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void SetDefault()
        {
            _count = 1;
            UpdateCursorState();
        }
    }
}