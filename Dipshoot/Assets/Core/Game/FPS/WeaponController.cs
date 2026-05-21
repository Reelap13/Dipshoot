using Game.Players.Input;
using Game.TickSystem;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class WeaponController : NetworkBehaviour, ITickSystem
    {
        private const string LogPrefix = "[NetTick][Weapon]";

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private float _range = 100f;
        [SerializeField] private Vector3 _eye_offset = new(0f, 0.49f, 0.359f);
        [SerializeField] private LayerMask _hit_mask = ~0;
        [SerializeField] private QueryTriggerInteraction _trigger_interaction = QueryTriggerInteraction.Ignore;
        [SerializeField] private float _marker_lifetime = 0.6f;
        [SerializeField] private float _hit_marker_size = 0.18f;
        [SerializeField] private float _miss_marker_size = 0.1f;
        [SerializeField] private Color _miss_color = Color.yellow;
        [SerializeField] private Color _hit_color = Color.red;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private TickManager _registered_tick_manager;
        private int _last_processed_input_tick = -1;

        public TickLayer TickLayer => TickLayer.WeaponSimulation;
        public int TickOrder => 0;

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            CacheReferences();
            TryRegisterTickSystem();
        }

        private void OnDisable()
        {
            TryUnregisterTickSystem();
        }

        public bool ShouldTick(GameTickContext context)
        {
            return isServer && _character != null && _character.TickManager == context.TickManager;
        }

        public void Tick(GameTickContext context)
        {
            TryProcessShot(context.Tick);
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();
        }

        private void TryRegisterTickSystem()
        {
            if (_registered_tick_manager != null || _character == null || _character.TickManager == null)
                return;

            _registered_tick_manager = _character.TickManager;
            _registered_tick_manager.RegisterSystem(this);
        }

        private void TryUnregisterTickSystem()
        {
            if (_registered_tick_manager == null)
                return;

            _registered_tick_manager.UnregisterSystem(this);
            _registered_tick_manager = null;
        }

        private void TryProcessShot(int server_tick)
        {
            if (!_character.InputBuffet.TryGetFirstAfter(_last_processed_input_tick, out PlayerInputData input))
                return;

            _last_processed_input_tick = input.Tick;

            if (!input.IsShootPressed)
                return;

            if (!_character.StateBuffer.TryGet(server_tick, out PlayerState shot_state))
                return;

            ShotResult result = BuildShotResult(input.Tick, server_tick, shot_state);

            Debug.Log(
                $"{LogPrefix} Shot. netId={result.ShooterNetId} inputTick={result.InputTick} " +
                $"serverTick={result.ServerTick} hit={result.HasHit} hitNetId={result.HitNetId}");

            RpcRegisterShot(result);
        }

        private ShotResult BuildShotResult(int input_tick, int server_tick, PlayerState state)
        {
            Vector3 origin = GetShotOrigin(state);
            Vector3 direction = GetShotDirection(state);
            bool has_hit = TryGetShotHit(origin, direction, out RaycastHit hit);
            Vector3 point = has_hit ? hit.point : origin + direction * _range;

            return new ShotResult
            {
                ShooterNetId = netId,
                HitNetId = has_hit ? GetHitNetId(hit) : 0,
                InputTick = input_tick,
                ServerTick = server_tick,
                Origin = origin,
                Direction = direction,
                Point = point,
                HasHit = has_hit,
            };
        }

        private Vector3 GetShotOrigin(PlayerState state)
        {
            return state.Position + state.Rotation * _eye_offset;
        }

        private Vector3 GetShotDirection(PlayerState state)
        {
            Quaternion pitch_rotation = Quaternion.Euler(state.CameraPitch, 0f, 0f);
            return state.Rotation * pitch_rotation * Vector3.forward;
        }

        private bool TryGetShotHit(Vector3 origin, Vector3 direction, out RaycastHit hit)
        {
            hit = default;
            float closest_distance = float.MaxValue;
            bool has_hit = false;
            int hits_count = Physics.RaycastNonAlloc(
                origin,
                direction,
                _hits,
                _range,
                _hit_mask,
                _trigger_interaction);

            for (int i = 0; i < hits_count; i++)
            {
                RaycastHit current_hit = _hits[i];
                if (current_hit.collider == null || IsOwnCollider(current_hit.collider))
                    continue;

                if (current_hit.distance >= closest_distance)
                    continue;

                closest_distance = current_hit.distance;
                hit = current_hit;
                has_hit = true;
            }

            return has_hit;
        }

        private bool IsOwnCollider(Collider target)
        {
            return target.transform == transform || target.transform.IsChildOf(transform);
        }

        private uint GetHitNetId(RaycastHit hit)
        {
            NetworkIdentity identity = hit.collider.GetComponentInParent<NetworkIdentity>();
            return identity == null ? 0 : identity.netId;
        }

        [ClientRpc]
        private void RpcRegisterShot(ShotResult result)
        {
            DrawShotMarker(result);
        }

        private void DrawShotMarker(ShotResult result)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = result.HasHit ? "ShotHitConfirm" : "ShotMissConfirm";
            marker.transform.position = result.Point;
            marker.transform.localScale = Vector3.one * (result.HasHit ? _hit_marker_size : _miss_marker_size);

            Scene scene = gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.MoveGameObjectToScene(marker, scene);

            if (marker.TryGetComponent(out Collider marker_collider))
                Destroy(marker_collider);

            if (marker.TryGetComponent(out Renderer marker_renderer))
                marker_renderer.material.color = result.HasHit ? _hit_color : _miss_color;

            marker.AddComponent<SelfDestroyer>().Initialize(_marker_lifetime);
        }
    }
}
