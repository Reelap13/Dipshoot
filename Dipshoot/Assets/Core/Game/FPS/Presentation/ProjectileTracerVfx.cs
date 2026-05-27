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
        private float _age;

        public void Initialize(Vector3 start, Vector3 end, float speed, float lifetime)
        {
            _start = start;
            _end = end;
            _direction = (_end - _start).normalized;
            _distance = Vector3.Distance(_start, _end);
            _speed = Mathf.Max(1f, speed);
            _lifetime = Mathf.Max(0.01f, lifetime);
            _age = 0f;

            transform.position = _start;
            if (_direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void Update()
        {
            _age += Time.deltaTime;

            float traveled = Mathf.Min(_distance, _age * _speed);
            transform.position = _start + _direction * traveled;

            if (_age >= _lifetime)
                Destroy(gameObject);
        }
    }
}
