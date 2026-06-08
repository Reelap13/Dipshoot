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
        [SerializeField] private float _sync_interval = 0.05f;

        [SyncVar] private int _player_id = -1;
        [SyncVar] private TeamId _team_id = TeamId.Spectator;
        [SyncVar(hook = nameof(HandlePositionSynced))] private Vector3 _synced_position;
        [SyncVar(hook = nameof(HandleRotationSynced))] private Quaternion _synced_rotation;

        private Camera _attached_camera;
        private Transform _initial_parent;
        private Vector3 _initial_local_position;
        private Quaternion _initial_local_rotation;
        private bool _is_camera_attached;
        private float _pitch;
        private float _next_sync_time;

        public TeamId TeamId => _team_id;

        public void InitializeServer(int player_id, TeamId team_id)
        {
            if (!isServer)
                return;

            _player_id = player_id;
            _team_id = team_id;
            _synced_position = transform.position;
            _synced_rotation = transform.rotation;
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
            TrySyncTransform();
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
            if (ClientAppRoot.HasInstance && !ClientAppRoot.Instance.InputRouter.IsGameplayInputAllowed)
                return;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null)
                return;

            Vector2 look = mouse.delta.ReadValue() * _look_sensitivity;
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

        private void TrySyncTransform()
        {
            if (Time.time < _next_sync_time)
                return;

            _next_sync_time = Time.time + Mathf.Max(0.02f, _sync_interval);
            CmdSyncTransform(transform.position, transform.rotation);
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSyncTransform(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _synced_position = position;
            _synced_rotation = rotation;
        }

        private void HandlePositionSynced(Vector3 old_position, Vector3 new_position)
        {
            if (isOwned)
                return;

            transform.position = new_position;
        }

        private void HandleRotationSynced(Quaternion old_rotation, Quaternion new_rotation)
        {
            if (isOwned)
                return;

            transform.rotation = new_rotation;
        }

        private void TryAttachCamera()
        {
            if (_is_camera_attached)
                return;

            if (!TryGetSceneCamera(out _attached_camera))
                return;

            Transform camera_transform = _attached_camera.transform;
            _initial_parent = camera_transform.parent;
            _initial_local_position = camera_transform.localPosition;
            _initial_local_rotation = camera_transform.localRotation;

            camera_transform.SetParent(transform, false);
            camera_transform.localPosition = Vector3.zero;
            camera_transform.localRotation = Quaternion.identity;
            _attached_camera.enabled = true;
            if (_attached_camera.TryGetComponent(out AudioListener listener))
                listener.enabled = true;

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

        private bool TryGetSceneCamera(out Camera camera)
        {
            camera = null;
            Camera fallback_camera = null;
            Scene scene = gameObject.scene;

            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            GameObject[] root_objects = scene.GetRootGameObjects();
            foreach (GameObject root_object in root_objects)
            {
                Camera[] cameras = root_object.GetComponentsInChildren<Camera>(true);
                foreach (Camera scene_camera in cameras)
                {
                    if (scene_camera == null)
                        continue;

                    fallback_camera ??= scene_camera;
                    if (!scene_camera.isActiveAndEnabled)
                        continue;

                    camera ??= scene_camera;
                    if (scene_camera.CompareTag("MainCamera"))
                    {
                        camera = scene_camera;
                        return true;
                    }
                }
            }

            camera ??= fallback_camera;
            return camera != null;
        }

        private static float NormalizePitch(float pitch)
        {
            return pitch > 180f ? pitch - 360f : pitch;
        }
    }
}
