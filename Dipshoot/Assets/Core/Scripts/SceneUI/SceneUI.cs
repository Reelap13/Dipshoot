using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts.UI.SceneUI
{
    public class SceneUI : Singleton<SceneUI>
    {
        [field: SerializeField]
        public SceneFader Fader { get; private set; }
        [field: SerializeField]
        public ESCController ESC { get; private set; }
        [field: SerializeField]
        public CursorController Cursor { get; private set; }

        private void Awake()
        {
            if (SceneUI.Instance != this)
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Cursor.SetDefault();
            ESC.SetDefault();
        }
    }
}