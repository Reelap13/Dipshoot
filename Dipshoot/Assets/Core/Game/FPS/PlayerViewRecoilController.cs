using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class PlayerViewRecoilController : MonoBehaviour, IPlayerSimulationResettable
    {
        [SerializeField] private float _pitch_impulse_scale = 1f;
        [SerializeField] private float _yaw_impulse_scale = 0.75f;
        [SerializeField] private float _return_speed = 18f;
        [SerializeField] private float _smooth_time = 0.035f;
        [SerializeField] private float _max_pitch = 9f;
        [SerializeField] private float _max_yaw = 4f;

        private Vector2 _target;
        private Vector2 _current;
        private Vector2 _velocity;

        private void LateUpdate()
        {
            float delta_time = Time.deltaTime;
            _target = Vector2.MoveTowards(_target, Vector2.zero, _return_speed * delta_time);
            _current = Vector2.SmoothDamp(_current, _target, ref _velocity, _smooth_time, Mathf.Infinity, delta_time);
        }

        public void AddImpulse(float pitch, float yaw)
        {
            _target.x = Mathf.Clamp(_target.x + pitch * _pitch_impulse_scale, 0f, _max_pitch);
            _target.y = Mathf.Clamp(_target.y + yaw * _yaw_impulse_scale, -_max_yaw, _max_yaw);
        }

        public Quaternion GetRotationOffset()
        {
            return Quaternion.Euler(-_current.x, _current.y, 0f);
        }

        public void ResetRecoil()
        {
            _target = Vector2.zero;
            _current = Vector2.zero;
            _velocity = Vector2.zero;
        }

        public void ResetSimulation()
        {
            ResetRecoil();
        }
    }
}
