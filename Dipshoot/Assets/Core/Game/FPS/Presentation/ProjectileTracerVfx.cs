using UnityEngine;

namespace Game.Players
{
    public class ProjectileTracerVfx : MonoBehaviour
    {
        private Vector3 _start;
        private Vector3 _end;
        private Vector3 _direction;
        private float _distance;
        private float _speed;
        private float _lifetime;
        private float _fade_start_time;
        private float _fade_time;
        private float _length;
        private float _age;
        private LineRenderer _line_renderer;
        private TrailRenderer[] _trail_renderers;
        private Color _start_color;
        private Color _end_color;

        public void Initialize(Vector3 start, Vector3 end, float speed, float lifetime)
        {
            Initialize(start, end, speed, lifetime, 0f, 0f);
        }

        public void Initialize(
            Vector3 start,
            Vector3 end,
            float speed,
            float min_visible_time,
            float fade_time,
            float length)
        {
            _start = start;
            _end = end;
            _direction = (_end - _start).normalized;
            _distance = Vector3.Distance(_start, _end);
            _speed = Mathf.Max(1f, speed);
            _fade_time = Mathf.Max(0f, fade_time);
            _length = Mathf.Max(0f, length);
            float travel_time = _distance / _speed;
            _fade_start_time = Mathf.Max(travel_time, min_visible_time);
            _lifetime = Mathf.Max(0.01f, _fade_start_time + _fade_time);
            _age = 0f;

            transform.position = _start;
            if (_direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_direction);

            _line_renderer = GetComponentInChildren<LineRenderer>();
            if (_line_renderer != null)
            {
                _start_color = _line_renderer.startColor;
                _end_color = _line_renderer.endColor;
            }

            _trail_renderers = GetComponentsInChildren<TrailRenderer>(true);
            for (int i = 0; i < _trail_renderers.Length; i++)
            {
                TrailRenderer trail = _trail_renderers[i];
                trail.enabled = true;
                trail.Clear();
                trail.emitting = true;
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;

            float traveled = Mathf.Min(_distance, _age * _speed);
            transform.position = _start + _direction * traveled;
            UpdateLine(traveled);

            if (_age >= _lifetime)
                Destroy(gameObject);
        }

        private void UpdateLine(float traveled)
        {
            if (_line_renderer == null)
                return;

            float tail = _length <= 0f ? 0f : Mathf.Max(0f, traveled - _length);
            _line_renderer.useWorldSpace = true;
            _line_renderer.SetPosition(0, _start + _direction * tail);
            _line_renderer.SetPosition(1, _start + _direction * traveled);

            float alpha = _age <= _fade_start_time || _fade_time <= 0f
                ? 1f
                : Mathf.Clamp01(1f - (_age - _fade_start_time) / _fade_time);
            _line_renderer.startColor = WithAlpha(_start_color, _start_color.a * alpha);
            _line_renderer.endColor = WithAlpha(_end_color, _end_color.a * alpha);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
