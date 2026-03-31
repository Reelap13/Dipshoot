using TMPro;
using UnityEngine;

namespace Scripts.UI
{
    public class TimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timer;
        [SerializeField] private int _after_decimal_numbers = 2;

        private bool _is_initialized;
        private float _start_time;
        private float _duration;

        public void Initialize(double remaining_time, float duration)
        {
            _is_initialized = true;
            _start_time = (float)(Time.time - remaining_time);
            _duration = duration;
        }

        public void SetActive(bool active_state) => gameObject.SetActive(active_state);

        private void Update()
        {
            if (!_is_initialized)
                return;

            UpdateText();
        }

        private void UpdateText()
        {
            _timer.text = RemainingTime.ToString($"F{_after_decimal_numbers}");
        }

        public double RemainingTime => Mathf.Max(_duration - (Time.time - _start_time), 0f);
    }
}