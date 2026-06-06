using System.Collections;
using TMPro;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientErrorLayer : MonoBehaviour
    {
        [SerializeField] private Transform _container;
        [SerializeField] private GameObject _message_prefab;
        [SerializeField] private float _visible_time = 3f;
        [SerializeField] private float _fade_time = 1.5f;

        private void Awake()
        {
            ClientAppRoot.Instance.LobbyStore.OnErrorRegistered += ShowError;
        }

        private void OnDestroy()
        {
            if (!ClientAppRoot.HasInstance)
                return;

            ClientAppRoot.Instance.LobbyStore.OnErrorRegistered -= ShowError;
        }

        private void ShowError(string error)
        {
            StartCoroutine(SpawnError(error));
        }

        private IEnumerator SpawnError(string error)
        {
            if (_message_prefab == null || _container == null)
                yield break;

            GameObject panel = Instantiate(_message_prefab, _container, false);
            if (panel.TryGetComponent(out CanvasGroup canvas_group))
                canvas_group.alpha = 1f;
            else
                canvas_group = panel.AddComponent<CanvasGroup>();

            TextMeshProUGUI text = panel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = error;

            yield return new WaitForSeconds(_visible_time);

            float elapsed = 0f;
            while (elapsed < _fade_time)
            {
                elapsed += Time.deltaTime;
                canvas_group.alpha = 1f - Mathf.Clamp01(elapsed / _fade_time);
                yield return null;
            }

            Destroy(panel);
        }
    }
}
