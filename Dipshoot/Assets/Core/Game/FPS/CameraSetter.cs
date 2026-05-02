using Mirror;
using UnityEngine;

namespace Game.Players
{
    public class CameraSetter : NetworkBehaviour
    {
        [SerializeField] private Transform _camera_point;
        [SerializeField] private PlayerCharacter _character;

        private Camera _attached_camera;
        private Transform _initial_parent;
        private Vector3 _initial_local_position;
        private Quaternion _initial_local_rotation;
        private bool _is_camera_attached;

        private void Awake()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();
        }

        public override void OnStartAuthority()
        {
            TryAttachCamera();
        }

        public override void OnStopAuthority()
        {
            DetachCamera();
        }

        private void LateUpdate()
        {
            if (!isOwned)
                return;

            TryAttachCamera();
            UpdateCameraPitch();
        }

        private void OnDisable()
        {
            if (!_is_camera_attached)
                return;

            DetachCamera();
        }

        private void OnDestroy()
        {
            if (!_is_camera_attached)
                return;

            DetachCamera();
        }

        private void TryAttachCamera()
        {
            if (_is_camera_attached || _camera_point == null || Camera.main == null)
                return;

            _attached_camera = Camera.main;
            Transform camera_transform = _attached_camera.transform;
            _initial_parent = camera_transform.parent;
            _initial_local_position = camera_transform.localPosition;
            _initial_local_rotation = camera_transform.localRotation;

            camera_transform.SetParent(_camera_point, false);
            camera_transform.localPosition = Vector3.zero;
            camera_transform.localRotation = Quaternion.identity;
            _is_camera_attached = true;
        }

        private void DetachCamera()
        {
            if (!_is_camera_attached || _attached_camera == null)
                return;

            Transform camera_transform = _attached_camera.transform;
            camera_transform.SetParent(_initial_parent, false);
            camera_transform.localPosition = _initial_local_position;
            camera_transform.localRotation = _initial_local_rotation;

            _attached_camera = null;
            _is_camera_attached = false;
        }

        private void UpdateCameraPitch()
        {
            if (_camera_point == null || _character == null || _character.TickManager == null)
                return;

            if (!_character.StateBuffer.TryGetLastAtOrBefore(_character.TickManager.CurrentTick, out PlayerState state))
                return;

            _camera_point.localRotation = Quaternion.Euler(state.CameraPitch, 0f, 0f);
        }
    }
}
