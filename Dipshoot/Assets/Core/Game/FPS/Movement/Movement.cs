using Game.Players.Input;
using Game.TickSystem;
using Scripts.Stats;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Players
{
    public class Movement : PlayerCharacterComponent
    {
        private const int MaxCollisionCastIterations = 3;
        private const int MaxOverlapHits = 32;
        private const float MinMoveDistance = 0.0001f;
        private const float CameraHeightRatio = 0.745f;

        [SerializeField] private StatsController _stats;
        [SerializeField] private CapsuleCollider _capsule;
        [SerializeField] private Transform _camera_point;
        [SerializeField] private LayerMask _collision_mask = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction _trigger_interaction = QueryTriggerInteraction.Ignore;

        private readonly RaycastHit[] _cast_hits = new RaycastHit[MaxOverlapHits];
        private readonly Collider[] _overlap_hits = new Collider[MaxOverlapHits];
        private Collider[] _own_colliders = System.Array.Empty<Collider>();
        private int _last_server_processed_input_tick = -1;
        private bool _has_last_server_input;
        private PlayerInputData _last_server_input;

        public int LastServerProcessedInputTick => _last_server_processed_input_tick;

        public override TickLayer TickLayer => TickLayer.Movement;

        private void Awake()
        {
            CacheReferences();
        }

        public override bool ShouldTick(GameTickContext context)
        {
            return base.ShouldTick(context) && IsAlive && IsGameplayActive && (IsServer || IsClient && IsOwned);
        }

        protected override void OnTick(GameTickContext context)
        {
            if (IsServer)
            {
                SimulateServerTick(context.Tick, context.DeltaTime);
                return;
            }

            SimulateTick(context.Tick, context.DeltaTime);
        }

        public bool SimulateTick(int tick)
        {
            return SimulateTick(tick, TickManager.TickDelta);
        }

        public bool SimulateTick(int tick, float delta_time)
        {
            if (!Character.InputBuffet.TryGet(tick, out PlayerInputData input))
            {
                return false;
            }

            if (!TryGetSimulationStateForTick(tick, out PlayerState previous_state))
            {
                return false;
            }

            PlayerState new_state = Simulate(
                previous_state,
                input,
                delta_time,
                tick);

            Character.StateBuffer.Add(new_state);
            ApplyState(new_state);
            return true;
        }

        public bool SimulateServerTick(int server_tick)
        {
            return SimulateServerTick(server_tick, TickManager.TickDelta);
        }

        public bool SimulateServerTick(int server_tick, float delta_time)
        {
            if (!TryGetSimulationStateForTick(server_tick, out PlayerState previous_state))
            {
                return false;
            }

            previous_state = FillMissingStates(previous_state, server_tick - 1, delta_time);

            PlayerInputData input = GetServerInput(server_tick);

            PlayerState new_state = Simulate(
                previous_state,
                input,
                delta_time,
                server_tick);

            Character.StateBuffer.Add(new_state);
            ApplyState(new_state);
            return true;
        }

        public void ReplayFromTick(int from_tick, int to_tick)
        {
            if (to_tick < from_tick)
                return;

            for (int tick = from_tick; tick <= to_tick; tick++)
                SimulateTick(tick);
        }

        public PlayerState Simulate(
            PlayerState previous_state,
            PlayerInputData input,
            float delta_time,
            int tick)
        {
            CacheReferences();
            MovementSettings settings = GetMovementSettings();
            PlayerState new_state = MovementSimulation.Simulate(
                previous_state,
                input,
                delta_time,
                settings,
                tick);

            ResolveStance(previous_state, ref new_state, settings);
            ResolveCollision(previous_state, ref new_state, settings);
            UpdateGrounding(ref new_state, settings);
            if (!previous_state.IsGrounded && new_state.IsGrounded)
                MovementSimulation.ApplyLandingGroundControl(ref new_state, input, delta_time, settings);

            return new_state;
        }

        public void ApplyState(PlayerState state)
        {
            CacheReferences();
            ApplyCapsule(state, GetMovementSettings());
            MovementPresentation.ApplyState(transform, state);
        }

        private void CacheReferences()
        {
            if (_stats == null)
                _stats = GetComponent<StatsController>();

            if (_capsule == null)
                _capsule = GetComponentInChildren<CapsuleCollider>(true);

            if (_camera_point == null)
                _camera_point = transform.Find("CameraPoint");

            if (_own_colliders.Length == 0)
                _own_colliders = GetComponentsInChildren<Collider>(true);
        }

        private MovementSettings GetMovementSettings()
        {
            return new MovementSettings(
                GetStat(Stat.MOVEMENT_WALK_SPEED, 5f),
                GetStat(Stat.MOVEMENT_SPRINT_SPEED, 7.5f),
                GetStat(Stat.MOVEMENT_CROUCH_SPEED, 2.7f),
                GetStat(Stat.MOVEMENT_GROUND_ACCELERATION, 35f),
                GetStat(Stat.MOVEMENT_AIR_ACCELERATION, 8f),
                GetStat(Stat.MOVEMENT_GROUND_FRICTION, 12f),
                GetStat(Stat.MOVEMENT_AIR_CONTROL, 0.45f),
                GetStat(Stat.MOVEMENT_GRAVITY, 22f),
                GetStat(Stat.MOVEMENT_JUMP_SPEED, 7f),
                GetStat(Stat.MOVEMENT_COYOTE_TIME, 0.08f),
                GetStat(Stat.MOVEMENT_JUMP_BUFFER_TIME, 0.12f),
                GetStat(Stat.MOVEMENT_STAND_HEIGHT, 2f),
                GetStat(Stat.MOVEMENT_CROUCH_HEIGHT, 1.2f),
                GetStat(Stat.MOVEMENT_CAPSULE_RADIUS, 0.5f),
                GetStat(Stat.MOVEMENT_SLOPE_LIMIT, 45f),
                GetStat(Stat.MOVEMENT_STEP_HEIGHT, 0.35f),
                GetStat(Stat.MOVEMENT_SKIN_WIDTH, 0.05f),
                GetStat(Stat.MOVEMENT_SPRINT_FORWARD_DOT, 0.35f),
                GetStat(Stat.MOVEMENT_SPRINT_SIDE_MULTIPLIER, 0.8f),
                GetStat(Stat.MOVEMENT_SPRINT_BACK_MULTIPLIER, 0.6f));
        }

        private float GetStat(Stat stat, float fallback_value)
        {
            return _stats == null
                ? fallback_value
                : _stats.GetStatValue(stat, fallback_value);
        }

        private void ResolveStance(
            PlayerState previous_state,
            ref PlayerState new_state,
            MovementSettings settings)
        {
            if (new_state.Stance != MovementStance.Standing ||
                previous_state.Stance != MovementStance.Crouching)
            {
                return;
            }

            if (!HasBlockingOverlap(new_state.Position, settings.StandHeight, settings))
                return;

            new_state.Stance = MovementStance.Crouching;
        }

        private void ResolveCollision(
            PlayerState previous_state,
            ref PlayerState new_state,
            MovementSettings settings)
        {
            float height = GetHeight(new_state.Stance, settings);
            Vector3 target_position = new_state.Position;
            Vector3 horizontal_displacement = new(
                target_position.x - previous_state.Position.x,
                0f,
                target_position.z - previous_state.Position.z);

            Vector3 horizontal_start = previous_state.Position;
            Vector3 horizontal_position = ResolveHorizontalMove(
                horizontal_start,
                horizontal_displacement,
                height,
                settings);

            new_state.Position = new Vector3(
                horizontal_position.x,
                target_position.y,
                horizontal_position.z);

            ResolveVerticalMove(previous_state, ref new_state, height, settings);
        }

        private Vector3 ResolveHorizontalMove(
            Vector3 start_position,
            Vector3 displacement,
            float height,
            MovementSettings settings)
        {
            Vector3 position = start_position;
            Vector3 remaining = displacement;

            for (int i = 0; i < MaxCollisionCastIterations; i++)
            {
                float distance = remaining.magnitude;
                if (distance <= MinMoveDistance)
                    break;

                Vector3 direction = remaining / distance;
                if (!TryCapsuleCast(
                        position,
                        height,
                        direction,
                        distance + settings.SkinWidth,
                        settings,
                        out RaycastHit hit))
                {
                    position += remaining;
                    break;
                }

                float travel_distance = Mathf.Max(0f, hit.distance - settings.SkinWidth);
                position += direction * Mathf.Min(travel_distance, distance);

                Vector3 slide_normal = new(hit.normal.x, 0f, hit.normal.z);
                if (slide_normal.sqrMagnitude <= MinMoveDistance)
                    break;

                Vector3 travelled = direction * travel_distance;
                remaining = Vector3.ProjectOnPlane(remaining - travelled, slide_normal.normalized);
            }

            return position;
        }

        private void ResolveVerticalMove(
            PlayerState previous_state,
            ref PlayerState new_state,
            float height,
            MovementSettings settings)
        {
            float vertical_delta = new_state.Position.y - previous_state.Position.y;
            if (Mathf.Abs(vertical_delta) <= MinMoveDistance)
                return;

            Vector3 start_position = new(
                new_state.Position.x,
                previous_state.Position.y,
                new_state.Position.z);

            Vector3 direction = vertical_delta > 0f
                ? Vector3.up
                : Vector3.down;

            if (!TryCapsuleCast(
                    start_position,
                    height,
                    direction,
                    Mathf.Abs(vertical_delta) + settings.SkinWidth,
                    settings,
                    out RaycastHit hit))
            {
                return;
            }

            if (vertical_delta < 0f && !IsWalkableNormal(hit.normal, settings))
                return;

            float resolved_delta = Mathf.Max(0f, hit.distance - settings.SkinWidth);
            new_state.Position.y = previous_state.Position.y + direction.y * resolved_delta;
            new_state.Velocity.y = 0f;

            if (vertical_delta < 0f)
            {
                new_state.IsGrounded = true;
                new_state.TimeSinceGrounded = 0f;
            }
        }

        private void UpdateGrounding(ref PlayerState state, MovementSettings settings)
        {
            if (state.Velocity.y > 0f)
            {
                state.IsGrounded = false;
                return;
            }

            float height = GetHeight(state.Stance, settings);
            if (!TryFindGround(state.Position, height, settings, out RaycastHit hit))
            {
                state.IsGrounded = false;
                return;
            }

            state.Position.y = hit.point.y + settings.StandHeight * 0.5f;
            state.Velocity.y = 0f;
            state.IsGrounded = true;
            state.TimeSinceGrounded = 0f;
        }

        private bool TryFindGround(
            Vector3 position,
            float height,
            MovementSettings settings,
            out RaycastHit ground_hit)
        {
            ground_hit = default;

            GetCapsulePoints(
                position,
                height,
                settings,
                out Vector3 bottom,
                out _);

            float radius = Mathf.Max(
                MinMoveDistance,
                Mathf.Min(settings.CapsuleRadius, height * 0.5f) - settings.SkinWidth);
            Vector3 origin = bottom + Vector3.up * (settings.StepHeight + settings.SkinWidth);
            float distance = settings.StepHeight + settings.SkinWidth * 3f;

            int hit_count = SphereCastScene(
                origin,
                radius,
                Vector3.down,
                distance,
                out RaycastHit[] hits);

            float best_distance = float.MaxValue;
            for (int i = 0; i < hit_count; i++)
            {
                RaycastHit hit = hits[i];
                if (!IsValidCollisionHit(hit.collider))
                    continue;

                if (!IsWalkableNormal(hit.normal, settings))
                    continue;

                if (hit.distance >= best_distance)
                    continue;

                best_distance = hit.distance;
                ground_hit = hit;
            }

            return ground_hit.collider != null;
        }

        private bool TryCapsuleCast(
            Vector3 position,
            float height,
            Vector3 direction,
            float distance,
            MovementSettings settings,
            out RaycastHit closest_hit)
        {
            closest_hit = default;
            bool is_horizontal_cast = Mathf.Abs(direction.y) <= 0.01f;
            bool is_upward_cast = direction.y > 0.01f;
            GetCapsulePoints(
                position,
                height,
                settings,
                is_horizontal_cast || is_upward_cast,
                out Vector3 bottom,
                out Vector3 top);

            int hit_count = CapsuleCastScene(
                bottom,
                top,
                settings.CapsuleRadius,
                direction,
                distance,
                out RaycastHit[] hits);

            float closest_distance = float.MaxValue;
            for (int i = 0; i < hit_count; i++)
            {
                RaycastHit hit = hits[i];
                if (!IsValidCollisionHit(hit.collider))
                    continue;

                if (is_upward_cast)
                {
                    if (IsWalkableNormal(hit.normal, settings))
                        continue;

                    if (hit.distance <= settings.SkinWidth &&
                        hit.point.y <= position.y + settings.SkinWidth)
                    {
                        continue;
                    }
                }

                if (is_horizontal_cast)
                {
                    if (IsWalkableNormal(hit.normal, settings))
                        continue;

                    Vector3 horizontal_normal = new(hit.normal.x, 0f, hit.normal.z);
                    if (horizontal_normal.sqrMagnitude <= MinMoveDistance)
                        continue;

                    horizontal_normal.Normalize();
                    if (hit.distance <= settings.SkinWidth &&
                        Vector3.Dot(direction, horizontal_normal) >= 0f)
                    {
                        continue;
                    }
                }

                if (hit.distance >= closest_distance)
                    continue;

                closest_distance = hit.distance;
                closest_hit = hit;
            }

            return closest_hit.collider != null;
        }

        private bool HasBlockingOverlap(
            Vector3 position,
            float height,
            MovementSettings settings)
        {
            GetCapsulePoints(
                position,
                height,
                settings,
                out Vector3 bottom,
                out Vector3 top);

            int hit_count = OverlapCapsuleScene(
                bottom,
                top,
                settings.CapsuleRadius,
                out Collider[] hits);

            for (int i = 0; i < hit_count; i++)
            {
                if (IsBlockingStanceOverlap(hits[i], position, settings))
                    return true;
            }

            return false;
        }

        private void GetCapsulePoints(
            Vector3 position,
            float height,
            MovementSettings settings,
            out Vector3 bottom,
            out Vector3 top)
        {
            GetCapsulePoints(
                position,
                height,
                settings,
                false,
                out bottom,
                out top);
        }

        private void GetCapsulePoints(
            Vector3 position,
            float height,
            MovementSettings settings,
            bool raise_bottom,
            out Vector3 bottom,
            out Vector3 top)
        {
            float radius = Mathf.Min(settings.CapsuleRadius, height * 0.5f);
            float center_y = GetCapsuleCenterY(height, settings);
            float half_segment = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 center = position + Vector3.up * center_y;

            bottom = center + Vector3.down * half_segment;
            top = center + Vector3.up * half_segment;

            if (raise_bottom)
                bottom += Vector3.up * settings.SkinWidth;
        }

        private void ApplyCapsule(PlayerState state, MovementSettings settings)
        {
            float height = GetHeight(state.Stance, settings);
            if (_capsule != null)
            {
                _capsule.radius = Mathf.Min(settings.CapsuleRadius, height * 0.5f);
                _capsule.height = height;
                _capsule.center = new Vector3(0f, GetCapsuleCenterY(height, settings), 0f);
            }

            if (_camera_point == null)
                return;

            Vector3 camera_position = _camera_point.localPosition;
            camera_position.y = -settings.StandHeight * 0.5f + height * CameraHeightRatio;
            _camera_point.localPosition = camera_position;
        }

        private float GetHeight(MovementStance stance, MovementSettings settings)
        {
            return stance == MovementStance.Crouching
                ? settings.CrouchHeight
                : settings.StandHeight;
        }

        private static float GetCapsuleCenterY(float height, MovementSettings settings)
        {
            return -settings.StandHeight * 0.5f + height * 0.5f;
        }

        private bool IsValidCollisionHit(Collider hit_collider)
        {
            if (hit_collider == null || hit_collider.isTrigger)
                return false;

            for (int i = 0; i < _own_colliders.Length; i++)
            {
                if (_own_colliders[i] == hit_collider)
                    return false;
            }

            return true;
        }

        private bool IsBlockingStanceOverlap(
            Collider hit_collider,
            Vector3 position,
            MovementSettings settings)
        {
            if (!IsValidCollisionHit(hit_collider))
                return false;

            float feet_y = position.y - settings.StandHeight * 0.5f;
            return hit_collider.bounds.max.y > feet_y + settings.SkinWidth * 2f;
        }

        private static bool IsWalkableNormal(Vector3 normal, MovementSettings settings)
        {
            return Vector3.Angle(normal, Vector3.up) <= settings.SlopeLimit;
        }

        private int RaycastScene(
            Vector3 origin,
            Vector3 direction,
            float distance,
            out RaycastHit[] hits)
        {
            Physics.SyncTransforms();
            hits = _cast_hits;

            PhysicsScene physics_scene = gameObject.scene.GetPhysicsScene();
            if (!physics_scene.IsValid())
            {
                return Physics.RaycastNonAlloc(
                    origin,
                    direction,
                    _cast_hits,
                    distance,
                    _collision_mask,
                    _trigger_interaction);
            }

            return physics_scene.Raycast(
                origin,
                direction,
                _cast_hits,
                distance,
                _collision_mask,
                _trigger_interaction);
        }

        private int CapsuleCastScene(
            Vector3 bottom,
            Vector3 top,
            float radius,
            Vector3 direction,
            float distance,
            out RaycastHit[] hits)
        {
            Physics.SyncTransforms();
            hits = _cast_hits;

            PhysicsScene physics_scene = gameObject.scene.GetPhysicsScene();
            if (!physics_scene.IsValid())
            {
                return Physics.CapsuleCastNonAlloc(
                    bottom,
                    top,
                    radius,
                    direction,
                    _cast_hits,
                    distance,
                    _collision_mask,
                    _trigger_interaction);
            }

            return physics_scene.CapsuleCast(
                bottom,
                top,
                radius,
                direction,
                _cast_hits,
                distance,
                _collision_mask,
                _trigger_interaction);
        }

        private int SphereCastScene(
            Vector3 origin,
            float radius,
            Vector3 direction,
            float distance,
            out RaycastHit[] hits)
        {
            Physics.SyncTransforms();
            hits = _cast_hits;

            PhysicsScene physics_scene = gameObject.scene.GetPhysicsScene();
            if (!physics_scene.IsValid())
            {
                return Physics.SphereCastNonAlloc(
                    origin,
                    radius,
                    direction,
                    _cast_hits,
                    distance,
                    _collision_mask,
                    _trigger_interaction);
            }

            return physics_scene.SphereCast(
                origin,
                radius,
                direction,
                _cast_hits,
                distance,
                _collision_mask,
                _trigger_interaction);
        }

        private int OverlapCapsuleScene(
            Vector3 bottom,
            Vector3 top,
            float radius,
            out Collider[] hits)
        {
            Physics.SyncTransforms();
            hits = _overlap_hits;

            PhysicsScene physics_scene = gameObject.scene.GetPhysicsScene();
            if (!physics_scene.IsValid())
            {
                return Physics.OverlapCapsuleNonAlloc(
                    bottom,
                    top,
                    radius,
                    _overlap_hits,
                    _collision_mask,
                    _trigger_interaction);
            }

            return physics_scene.OverlapCapsule(
                bottom,
                top,
                radius,
                _overlap_hits,
                _collision_mask,
                _trigger_interaction);
        }

        private bool TryGetSimulationStateForTick(int tick, out PlayerState previous_state)
        {
            if (Character.StateBuffer.TryGet(tick, out previous_state))
                return true;

            if (Character.StateBuffer.TryGet(tick - 1, out previous_state))
                return true;

            if (Character.StateBuffer.TryGetLastAtOrBefore(tick - 1, out previous_state))
            {
                return true;
            }

            previous_state = default;
            return false;
        }

        private PlayerState FillMissingStates(PlayerState previous_state, int target_tick, float delta_time)
        {
            while (previous_state.Tick < target_tick)
            {
                int next_tick = previous_state.Tick + 1;
                PlayerInputData input = GetServerInput(next_tick);
                PlayerState state = Simulate(
                    previous_state,
                    input,
                    delta_time,
                    next_tick);

                Character.StateBuffer.Add(state);
                previous_state = state;
            }

            return previous_state;
        }

        private PlayerInputData GetServerInput(int server_tick)
        {
            if (Character.InputBuffet.TryGetFirstAfter(_last_server_processed_input_tick, out PlayerInputData input) &&
                input.Tick <= server_tick)
            {
                _last_server_processed_input_tick = input.Tick;
                _last_server_input = input;
                _has_last_server_input = true;
                return input;
            }

            if (_has_last_server_input)
            {
                return _last_server_input;
            }

            return default;
        }

        public override void ResetSimulation()
        {
            _last_server_processed_input_tick = -1;
            _has_last_server_input = false;
            _last_server_input = default;
        }
    }
}
