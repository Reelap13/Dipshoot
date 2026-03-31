using System.Collections;
using Scripts.UI;
using Server.Lobby;
using TMPro;
using UnityEngine;

namespace Server.PlayerHub
{
    public class ErrorUIController : MonoBehaviour
    {
        [SerializeField] private PlayerHubUIController _controller;
        [SerializeField] private ErrorMessage _error_message_prefab;
        [SerializeField] private Transform _from;
        [SerializeField] private Transform _to;
        [SerializeField] private float _move_time = 1f;
        [SerializeField] private float _fade_time = 5f;


        private void Awake()
        {
            _controller.OnErrorRegistered.AddListener(ShowError);
        }

        private void ShowError(string error)
        {
            UIUtilsGameObject.Instance.StartCoroutine(SpawnMoveAndFade(error));
        }

        public IEnumerator SpawnMoveAndFade(string error)
        {
            // Спавн
            ErrorMessage obj = Instantiate(_error_message_prefab, _from.position, Quaternion.identity, _from.parent);
            obj.Text.text = error;

            // Если это UI — лучше работать через RectTransform
            RectTransform rect = obj.GetComponent<RectTransform>();

            // ДВИЖЕНИЕ
            float t = 0f;
            Vector3 startPos = _from.position;
            Vector3 endPos = _to.position;

            while (t < _move_time)
            {
                t += Time.deltaTime;
                float lerp = t / _move_time;

                if (rect != null)
                    rect.position = Vector3.Lerp(startPos, endPos, lerp);
                else
                    obj.transform.position = Vector3.Lerp(startPos, endPos, lerp);

                yield return null;
            }

            // ФЕЙД ВСЕХ UI ЭЛЕМЕНТОВ
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = obj.gameObject.AddComponent<CanvasGroup>();

            t = 0f;

            while (t < _fade_time)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = 1f - (t / _fade_time);
                yield return null;
            }

            // Удаление
            Destroy(obj);
        }
    }
}