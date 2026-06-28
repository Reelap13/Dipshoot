using Game.MatchMode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientKillFeedEntryView : MonoBehaviour
    {
        private const float FadeInDuration = 0.1f;
        private const float HoldUntil = 4.5f;
        private const float Lifetime = 5.5f;

        [SerializeField] private CanvasGroup _canvas_group;
        [SerializeField] private RectTransform _rect_transform;
        [SerializeField] private TextMeshProUGUI _killer_name_text;
        [SerializeField] private Image _weapon_icon;
        [SerializeField] private TextMeshProUGUI _victim_name_text;

        private float _started_at;
        private Vector2 _target_position;

        public void Play(
            MatchKillEvent kill_event,
            Sprite weapon_sprite,
            Color killer_color,
            Color victim_color,
            float started_at)
        {
            _started_at = started_at;
            if (_killer_name_text != null)
            {
                _killer_name_text.text = kill_event.Killer.Nickname ?? string.Empty;
                _killer_name_text.color = killer_color;
            }

            if (_victim_name_text != null)
            {
                _victim_name_text.text = kill_event.Victim.Nickname ?? string.Empty;
                _victim_name_text.color = victim_color;
            }

            if (_weapon_icon != null)
            {
                _weapon_icon.sprite = weapon_sprite;
                _weapon_icon.enabled = weapon_sprite != null;
            }

            if (_canvas_group != null)
                _canvas_group.alpha = 0f;

            gameObject.SetActive(true);
        }

        public void SetImmediatePosition(Vector2 position)
        {
            _target_position = position;
            if (_rect_transform != null)
                _rect_transform.anchoredPosition = position;
        }

        public void SetTargetPosition(Vector2 position)
        {
            _target_position = position;
        }

        public bool Tick(float now, float delta_time, float position_lerp_speed)
        {
            float elapsed = now - _started_at;
            if (_canvas_group != null)
                _canvas_group.alpha = GetAlpha(elapsed);

            if (_rect_transform != null)
            {
                float lerp = 1f - Mathf.Exp(
                    -Mathf.Max(0f, position_lerp_speed) * delta_time);
                _rect_transform.anchoredPosition = Vector2.Lerp(
                    _rect_transform.anchoredPosition,
                    _target_position,
                    lerp);
            }

            return elapsed < Lifetime;
        }

        public void Release()
        {
            if (_canvas_group != null)
                _canvas_group.alpha = 0f;
            gameObject.SetActive(false);
        }

        private static float GetAlpha(float elapsed)
        {
            if (elapsed <= 0f)
                return 0f;
            if (elapsed < FadeInDuration)
                return Mathf.Clamp01(elapsed / FadeInDuration);
            if (elapsed <= HoldUntil)
                return 1f;

            return 1f - Mathf.Clamp01((elapsed - HoldUntil) / (Lifetime - HoldUntil));
        }
    }
}
