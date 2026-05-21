using UnityEngine;

namespace Game.ProcGen.City
{
    public sealed class CityDemoCameraController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 18f;
        [SerializeField] private float _fastMoveMultiplier = 3f;
        [SerializeField] private float _lookSensitivity = 3f;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        private void Update()
        {
            UpdateLook();
            UpdateMovement();
        }

        private void UpdateLook()
        {
            if (Input.GetMouseButton(1) == false)
                return;

            _yaw += Input.GetAxis("Mouse X") * _lookSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * _lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -85f, 85f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void UpdateMovement()
        {
            Vector3 direction = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
                direction += transform.forward;
            if (Input.GetKey(KeyCode.S))
                direction -= transform.forward;
            if (Input.GetKey(KeyCode.D))
                direction += transform.right;
            if (Input.GetKey(KeyCode.A))
                direction -= transform.right;
            if (Input.GetKey(KeyCode.Space))
                direction += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl))
                direction -= Vector3.up;

            if (direction.sqrMagnitude <= 0.001f)
                return;

            float speed = Input.GetKey(KeyCode.LeftShift) ? _moveSpeed * _fastMoveMultiplier : _moveSpeed;
            transform.position += direction.normalized * speed * Time.deltaTime;
        }
    }
}
