using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Scripts.UI.SceneUI
{
    public class SceneFader : MonoBehaviour
    {
        [NonSerialized] public UnityEvent<FadeState> OnFadeStateUpdated = new();

        [SerializeField] private Image _fade_image;
        [SerializeField] private float _fade_speed = 1f;
        [SerializeField] private bool _fade_on_awake = false;

        private FadeState _state;
        private float _elapsed;

        private void Awake()
        {
            _state = _fade_on_awake ? FadeState.FADE_OUT_FINISHED : FadeState.FADE_IN_FINISHED;
            _elapsed = _fade_on_awake ? 1f: 0f;
            _fade_image.gameObject.SetActive(_fade_on_awake);
            
            Color color = _fade_image.color;
            color.a = _elapsed;
            _fade_image.color = color;
        }

        public IEnumerator FadeOut()
        {
            _fade_image.gameObject.SetActive(true);
            State = FadeState.FADE_OUT_STARTED;
            float elapsed = _elapsed;
            Color color = _fade_image.color;

            while (elapsed < 1)
            {
                elapsed += Time.deltaTime / _fade_speed;
                _elapsed = elapsed;
                color.a = Mathf.Clamp01(elapsed);
                _fade_image.color = color;
                yield return null;
            }
            _elapsed = 1;
            State = FadeState.FADE_OUT_FINISHED;
        }

        public IEnumerator FadeIn()
        {
            State = FadeState.FADE_IN_STARTED;
            float elapsed = _elapsed;
            Color color = _fade_image.color;

            while (elapsed > 0)
            {
                elapsed -= Time.deltaTime / _fade_speed;
                _elapsed = elapsed;
                color.a = Mathf.Clamp01(elapsed);
                _fade_image.color = color;
                yield return null;
            }
            _elapsed = 0;
            _fade_image.gameObject.SetActive(false);
            State = FadeState.FADE_IN_FINISHED;
        }

        public FadeState State
        {
            get { return _state; }
            set
            {
                _state = value;
                OnFadeStateUpdated.Invoke(_state);
            }
        }
    }

    public enum FadeState
    {
        FADE_OUT_STARTED,
        FADE_OUT_FINISHED,
        FADE_IN_STARTED,
        FADE_IN_FINISHED
    }
}