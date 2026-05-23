using Game.MatchMode;
using Game.Players.Input;
using Game.TickSystem;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class WeaponController : NetworkBehaviour, ITickSystem, IPlayerSimulationResettable
    {
        private const string LogPrefix = "[NetTick][Weapon]";

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private int _damage = 25;
        [SerializeField] private float _range = 100f;
        [SerializeField] private Vector3 _eye_offset = new(0f, 0.49f, 0.359f);
        [SerializeField] private LayerMask _hit_mask = ~0;
        [SerializeField] private QueryTriggerInteraction _trigger_interaction = QueryTriggerInteraction.Ignore;
        [SerializeField] private float _tracer_lifetime = 0.12f;
        [SerializeField] private float _tracer_width = 0.03f;
        [SerializeField] private Color _tracer_color = Color.cyan;
        [SerializeField] private float _marker_lifetime = 0.6f;
        [SerializeField] private float _hit_marker_size = 0.18f;
        [SerializeField] private float _miss_marker_size = 0.1f;
        [SerializeField] private Color _miss_color = Color.yellow;
        [SerializeField] private Color _hit_color = Color.red;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private static Material _tracer_material;
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
            return isServer &&
                _character != null &&
                (_character.Health == null || _character.Health.IsAlive) &&
                _character.IsGameplayActive &&
                _character.TickManager == context.TickManager;
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
                $"serverTick={result.ServerTick} hit={result.HasHit} hitNetId={result.HitNetId} " +
                $"damage={result.DidDamage}");

            RpcRegisterShot(result);
        }

        private ShotResult BuildShotResult(int input_tick, int server_tick, PlayerState state)
        {
            Vector3 origin = GetShotOrigin(state);
            Vector3 direction = GetShotDirection(state);
            bool has_hit = TryGetShotHit(origin, direction, out RaycastHit hit);
            Vector3 point = has_hit ? hit.point : origin + direction * _range;

            ShotResult result = new()
            {
                ShooterNetId = netId,
                HitNetId = has_hit ? GetHitNetId(hit) : 0,
                InputTick = input_tick,
                ServerTick = server_tick,
                Origin = origin,
                Direction = direction,
                Point = point,
                Damage = _damage,
                HasHit = has_hit,
                DidDamage = false,
            };

            TryApplyDamage(ref result, hit);
            return result;
        }

        private void TryApplyDamage(ref ShotResult result, RaycastHit hit)
        {
            if (!result.HasHit || hit.collider == null)
                return;

            PlayerHealth health = hit.collider.GetComponentInParent<PlayerHealth>();
            if (health == null)
                return;

            if (IsFriendlyTarget(health))
                return;

            result.HitNetId = health.netId;
            result.DidDamage = health.TryApplyDamage(result.Damage, result.ShooterNetId);
        }

        private bool IsFriendlyTarget(PlayerHealth health)
        {
            PlayerMatchIdentity shooter_identity = _character.GetComponent<PlayerMatchIdentity>();
            PlayerMatchIdentity target_identity = health.GetComponent<PlayerMatchIdentity>();

            return shooter_identity != null &&
                target_identity != null &&
                shooter_identity.TeamId != TeamId.None &&
                shooter_identity.TeamId == target_identity.TeamId;
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
            int hits_count = RaycastScene(origin, direction);

            for (int i = 0; i < hits_count; i++)
            {
                RaycastHit current_hit = _hits[i];
                if (current_hit.collider == null || IsOwnCollider(current_hit.collider))
                    continue;

                PlayerHealth health = current_hit.collider.GetComponentInParent<PlayerHealth>();
                if (health != null && !health.IsAlive)
                    continue;

                if (current_hit.distance >= closest_distance)
                    continue;

                closest_distance = current_hit.distance;
                hit = current_hit;
                has_hit = true;
            }

            return has_hit;
        }

        private int RaycastScene(Vector3 origin, Vector3 direction)
        {
            Physics.SyncTransforms();

            PhysicsScene physics_scene = gameObject.scene.GetPhysicsScene();
            if (!physics_scene.IsValid())
            {
                return Physics.RaycastNonAlloc(
                    origin,
                    direction,
                    _hits,
                    _range,
                    _hit_mask,
                    _trigger_interaction);
            }

            return physics_scene.Raycast(
                origin,
                direction,
                _hits,
                _range,
                _hit_mask,
                _trigger_interaction);
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
            DrawShotTracer(result);
            DrawShotMarker(result);
        }

        private void DrawShotTracer(ShotResult result)
        {
            GameObject tracer = new("ShotTracer");
            MoveToObjectScene(tracer);

            LineRenderer line_renderer = tracer.AddComponent<LineRenderer>();
            line_renderer.positionCount = 2;
            line_renderer.useWorldSpace = true;
            line_renderer.SetPosition(0, result.Origin);
            line_renderer.SetPosition(1, result.Point);
            line_renderer.startWidth = _tracer_width;
            line_renderer.endWidth = _tracer_width;
            line_renderer.numCapVertices = 2;
            Material tracer_material = GetTracerMaterial();
            if (tracer_material != null)
                line_renderer.material = tracer_material;
            line_renderer.startColor = _tracer_color;
            line_renderer.endColor = _tracer_color;

            tracer.AddComponent<SelfDestroyer>().Initialize(_tracer_lifetime);
        }

        private static Material GetTracerMaterial()
        {
            if (_tracer_material != null)
                return _tracer_material;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            _tracer_material = new Material(shader);
            return _tracer_material;
        }

        private void DrawShotMarker(ShotResult result)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = result.HasHit ? "ShotHitConfirm" : "ShotMissConfirm";
            marker.transform.position = result.Point;
            marker.transform.localScale = Vector3.one * (result.HasHit ? _hit_marker_size : _miss_marker_size);

            MoveToObjectScene(marker);

            if (marker.TryGetComponent(out Collider marker_collider))
                Destroy(marker_collider);

            if (marker.TryGetComponent(out Renderer marker_renderer))
                marker_renderer.material.color = result.HasHit ? _hit_color : _miss_color;

            marker.AddComponent<SelfDestroyer>().Initialize(_marker_lifetime);
        }

        private void MoveToObjectScene(GameObject target)
        {
            Scene scene = gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.MoveGameObjectToScene(target, scene);
        }

        public void ResetSimulation()
        {
            _last_processed_input_tick = -1;
        }
    }
}
