using Core.ClientPresentation;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.MatchMode
{
    public class SpectatorPawn : NetworkBehaviour
    {
        [SerializeField] private float _move_speed = 12f;
        [SerializeField] private float _sprint_multiplier = 3f;
        [SerializeField] private float _look_sensitivity = 0.15f;

        [SyncVar] private int _player_id = -1;
        [SyncVar] private TeamId _team_id = TeamId.Spectator;

        private Camera _attached_camera;
        private bool _is_camera_attached;
        private float _pitch;

        public TeamId TeamId => _team_id;

        public void InitializeServer(int player_id, TeamId team_id)
        {
            if (!isServer)
                return;

            _player_id = player_id;
            _team_id = team_id;
        }

        public override void OnStartAuthority()
        {
            _pitch = NormalizePitch(transform.eulerAngles.x);
            TryAttachCamera();

            if (ClientAppRoot.HasInstance)
                ClientAppRoot.Instance.PresentationRoot.SetState(ClientPresentationState.Match);
        }

        public override void OnStopAuthority()
        {
            DetachCamera();
        }

        private void Update()
        {
            if (!isOwned)
                return;

            TryAttachCamera();
            UpdateLocalControl();
        }

        private void OnDisable()
        {
            DetachCamera();
        }

        private void OnDestroy()
        {
            DetachCamera();
        }

        private void UpdateLocalControl()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null)
                return;

            Vector2 look = mouse.delta.ReadValue() * _look_sensitivity;
            look *= ClientGameplaySettings.MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);
            float yaw = transform.eulerAngles.y + look.x;
            transform.rotation = Quaternion.Euler(_pitch, yaw, 0f);

            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed)
                move += transform.forward;
            if (keyboard.sKey.isPressed)
                move -= transform.forward;
            if (keyboard.dKey.isPressed)
                move += transform.right;
            if (keyboard.aKey.isPressed)
                move -= transform.right;
            if (keyboard.spaceKey.isPressed)
                move += Vector3.up;
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                move -= Vector3.up;

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            float speed = _move_speed;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                speed *= _sprint_multiplier;

            transform.position += move * speed * Time.deltaTime;
        }

        private void TryAttachCamera()
        {
            if (_is_camera_attached)
                return;

            if (!TryGetSceneCamera(out _attached_camera))
                return;

            Transform camera_transform = _attached_camera.transform;
            camera_transform.SetParent(transform, false);
            camera_transform.localPosition = Vector3.zero;
            camera_transform.localRotation = Quaternion.identity;
            _attached_camera.enabled = true;
            _attached_camera.depth = 100f;
            if (_attached_camera.TryGetComponent(out AudioListener listener))
                listener.enabled = true;

            _is_camera_attached = true;
        }

        private void DetachCamera()
        {
            if (!_is_camera_attached || _attached_camera == null)
                return;

            Destroy(_attached_camera.gameObject);
            _attached_camera = null;
            _is_camera_attached = false;
        }

        private bool TryGetSceneCamera(out Camera camera)
        {
            camera = CreateLocalCamera();
            return camera != null;
        }

        private Camera CreateLocalCamera()
        {
            GameObject camera_object = new("SpectatorCamera");
            SceneManager.MoveGameObjectToScene(camera_object, gameObject.scene);
            Camera camera = camera_object.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera_object.AddComponent<AudioListener>();
            return camera;
        }

        private static float NormalizePitch(float pitch)
        {
            return pitch > 180f ? pitch - 360f : pitch;
        }
    }
}
