using System.Collections;
using Mirror;
using Scripts.UI.SceneUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UI.PauseMenu
{
    public class PauseMenuController : Singleton<PauseMenuController>
    {
        [SerializeField] private GameObject _pause_menu;
        [SerializeField] private GameObject _settings_menu;

        private ESCRequest _open_esc;
        private ESCRequest _close_esc;

        private void Start()
        {
            _pause_menu.SetActive(false);
            _settings_menu.SetActive(false);
            _open_esc = SceneUI.Instance.ESC.AddMultipleInteraction(OpenMenu);
        }

        public void SetPauseMenu(GameObject pause_menu)
        {
            _pause_menu = pause_menu;
        }

        private void OpenMenu()
        {
            _close_esc = SceneUI.Instance.ESC.AddSingleInteraction(CloseMenu);

            //CharacterData.LocalCharacter.GetComponent<Character>().Disactivate();
            //CharacterData.LocalCharacter.GetComponentInChildren<InterfaceController>().DiactivateGameUIPause();

            SceneUI.Instance.Cursor.Show();

            _pause_menu.SetActive(true);
        }

        public void CloseMenu()
        {
            SceneUI.Instance.ESC.BlockRequest(_close_esc);

            //CharacterData.LocalCharacter.GetComponent<Character>().Activate();
            //CharacterData.LocalCharacter.GetComponentInChildren<InterfaceController>().ActivateGameUIPause();

            SceneUI.Instance.Cursor.Hide();

            _pause_menu.SetActive(false);
        }

        public void OpenSettingsMenu()
        {
            SceneUI.Instance.ESC.AddSingleInteraction(CloseSettingsMenu);
            _settings_menu.SetActive(true);
            //_pause_menu.SetActive(false);
        }

        public void CloseSettingsMenu()
        {
            _settings_menu.SetActive(false);
            //_pause_menu.SetActive(true);
        }

        public void ComebackToMenu()
        {
            IEnumerator Leave()
            {
                yield return StartCoroutine(SceneUI.Instance.Fader.FadeOut());

                var network_manager = NetworkManager.singleton;
                if (NetworkServer.active)
                    network_manager.StopHost();
                else network_manager.StopClient();
            }

            StartCoroutine(Leave());
        }
    }
}