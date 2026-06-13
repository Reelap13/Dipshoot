using System;
using System.Collections.Generic;
using Game.TickSystem;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class PlayerHitboxLagCompensation : MonoBehaviour, ITickSystem, IPlayerSimulationResettable
    {
        private const float MinDistance = 0.0001f;
        private const int DefaultMaxHistoryTicks = 64;

        private static readonly List<PlayerHitboxLagCompensation> Instances = new();

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerHitboxRigController _hitbox_rig;
        [SerializeField] private float _history_seconds = 0.35f;
        [SerializeField] private int _max_history_ticks = DefaultMaxHistoryTicks;
        [SerializeField] private float _hitbox_retry_interval_seconds = 0.25f;

        private PlayerHitbox[] _hitboxes = Array.Empty<PlayerHitbox>();
        private PlayerHitboxSnapshotFrame[] _frames = Array.Empty<PlayerHitboxSnapshotFrame>();
        private TickManager _registered_tick_manager;
        private float _next_hitbox_retry_at;

        public TickLayer TickLayer => TickLayer.HitboxSnapshot;
        public int TickOrder => 0;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            RegisterInstance();
            CacheReferences();
            TryRegisterTickSystem();
        }

        private void OnDisable()
        {
            UnregisterInstance();
            TryUnregisterTickSystem();
        }

        private void Update()
        {
            CacheReferences();
            TryRegisterTickSystem();
        }

        public bool ShouldTick(GameTickContext context)
        {
            return _character != null &&
                _character.isServer &&
                _character.TickManager == context.TickManager &&
                _health != null;
        }

        public void Tick(GameTickContext context)
        {
            CaptureFrame(context.Tick);
        }

        public void CaptureCurrentFrame(int tick)
        {
            CaptureFrame(tick);
        }

        public void ResetSimulation()
        {
            ClearHistory();
        }

        public void RefreshHitboxes()
        {
            PlayerHitbox[] hitboxes = GetComponentsInChildren<PlayerHitbox>(true);
            int count = 0;
            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (!IsUsableHitbox(hitboxes[i]))
                    continue;

                hitboxes[count++] = hitboxes[i];
            }

            if (count == hitboxes.Length)
            {
                _hitboxes = hitboxes;
                return;
            }

            _hitboxes = new PlayerHitbox[count];
            Array.Copy(hitboxes, _hitboxes, count);
        }

        public static bool TryRaycast(
            PlayerCharacter shooter,
            int target_tick,
            Vector3 origin,
            Vector3 direction,
            float max_distance,
            out PlayerHitboxSnapshotHit closest_hit)
        {
            closest_hit = default;
            if (shooter == null || max_distance <= 0f)
                return false;

            float direction_length = direction.magnitude;
            if (direction_length <= MinDistance)
                return false;

            direction /= direction_length;
            float closest_distance = max_distance;
            bool has_hit = false;

            for (int i = 0; i < Instances.Count; i++)
            {
                PlayerHitboxLagCompensation target = Instances[i];
                if (!IsValidTarget(shooter, target))
                    continue;

                if (!target.TryRaycastFrame(
                        target_tick,
                        origin,
                        direction,
                        closest_distance,
                        out PlayerHitboxSnapshotHit hit))
                {
                    continue;
                }

                closest_distance = hit.Distance;
                closest_hit = hit;
                has_hit = true;
            }

            return has_hit;
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_health == null)
                _health = GetComponent<PlayerHealth>();

            if (_hitbox_rig == null)
                _hitbox_rig = GetComponent<PlayerHitboxRigController>();
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

        private void RegisterInstance()
        {
            if (!Instances.Contains(this))
                Instances.Add(this);
        }

        private void UnregisterInstance()
        {
            Instances.Remove(this);
        }

        private void CaptureFrame(int tick)
        {
            if (_health != null && !_health.IsAlive)
                return;

            TryEnsureHitboxes();
            if (_hitboxes.Length == 0)
                return;

            int history_capacity = GetHistoryCapacity();
            EnsureFrameCapacity(history_capacity, _hitboxes.Length);

            int frame_index = GetFrameIndex(tick, _frames.Length);
            ref PlayerHitboxSnapshotFrame frame = ref _frames[frame_index];
            frame.Tick = tick;
            frame.Count = 0;
            frame.EnsureCapacity(_hitboxes.Length);

            for (int i = 0; i < _hitboxes.Length; i++)
            {
                if (!TryCreateSnapshot(_hitboxes[i], tick, out PlayerHitboxSnapshot snapshot))
                    continue;

                frame.Snapshots[frame.Count++] = snapshot;
            }
        }

        private void TryEnsureHitboxes()
        {
            if (_hitboxes.Length > 0)
                return;

            if (Time.unscaledTime < _next_hitbox_retry_at)
                return;

            _next_hitbox_retry_at = Time.unscaledTime + Mathf.Max(0.05f, _hitbox_retry_interval_seconds);

            if (_hitbox_rig != null)
                _hitbox_rig.RequestRebuild();

            RefreshHitboxes();
        }

        private int GetHistoryCapacity()
        {
            int tick_rate = _character == null || _character.TickManager == null
                ? 0
                : _character.TickManager.TickRate;
            int seconds_capacity = tick_rate <= 0
                ? 0
                : Mathf.CeilToInt(_history_seconds * tick_rate);

            return Mathf.Max(1, Mathf.Min(_max_history_ticks, Mathf.Max(seconds_capacity, 1)) + 1);
        }

        private void EnsureFrameCapacity(int history_capacity, int hitbox_capacity)
        {
            if (_frames.Length == history_capacity)
                return;

            _frames = new PlayerHitboxSnapshotFrame[history_capacity];
            for (int i = 0; i < _frames.Length; i++)
                _frames[i].EnsureCapacity(hitbox_capacity);
        }

        private bool TryRaycastFrame(
            int target_tick,
            Vector3 origin,
            Vector3 direction,
            float max_distance,
            out PlayerHitboxSnapshotHit closest_hit)
        {
            closest_hit = default;
            if (!TryGetFrame(target_tick, out PlayerHitboxSnapshot[] snapshots, out int count, out int snapshot_tick))
                return false;

            float closest_distance = max_distance;
            bool has_hit = false;
            for (int i = 0; i < count; i++)
            {
                PlayerHitboxSnapshot snapshot = snapshots[i];
                if (!TryRaycastSnapshot(snapshot, origin, direction, closest_distance, out float distance))
                    continue;

                closest_distance = distance;
                closest_hit = new PlayerHitboxSnapshotHit
                {
                    Health = _health,
                    HitNetId = _health == null ? 0 : _health.netId,
                    HitboxType = snapshot.Type,
                    DamageMultiplier = snapshot.DamageMultiplier,
                    Point = origin + direction * distance,
                    Distance = distance,
                    SnapshotTick = snapshot_tick,
                };
                has_hit = true;
            }

            return has_hit;
        }

        private bool TryGetFrame(
            int target_tick,
            out PlayerHitboxSnapshot[] snapshots,
            out int count,
            out int snapshot_tick)
        {
            snapshots = null;
            count = 0;
            snapshot_tick = -1;

            int best_index = -1;
            int best_tick = int.MinValue;
            int nearest_index = -1;
            int nearest_delta = int.MaxValue;

            for (int i = 0; i < _frames.Length; i++)
            {
                PlayerHitboxSnapshotFrame frame = _frames[i];
                if (!frame.IsValid)
                    continue;

                if (frame.Tick <= target_tick && frame.Tick > best_tick)
                {
                    best_tick = frame.Tick;
                    best_index = i;
                }

                int delta = Mathf.Abs(frame.Tick - target_tick);
                if (delta < nearest_delta)
                {
                    nearest_delta = delta;
                    nearest_index = i;
                }
            }

            int frame_index = best_index >= 0 ? best_index : nearest_index;
            if (frame_index < 0)
                return false;

            PlayerHitboxSnapshotFrame result = _frames[frame_index];
            snapshots = result.Snapshots;
            count = result.Count;
            snapshot_tick = result.Tick;
            return result.IsValid;
        }

        private void ClearHistory()
        {
            for (int i = 0; i < _frames.Length; i++)
                _frames[i].Count = 0;
        }

        private static bool TryCreateSnapshot(
            PlayerHitbox hitbox,
            int tick,
            out PlayerHitboxSnapshot snapshot)
        {
            snapshot = default;
            if (!IsUsableHitbox(hitbox) || hitbox.HitboxCollider == null || !hitbox.HitboxCollider.enabled)
                return false;

            Collider collider = hitbox.HitboxCollider;
            Transform collider_transform = collider.transform;
            Vector3 scale = Abs(collider_transform.lossyScale);

            snapshot.Tick = tick;
            snapshot.Type = hitbox.Type;
            snapshot.Rotation = collider_transform.rotation;
            snapshot.DamageMultiplier = hitbox.DamageMultiplier;

            if (collider is SphereCollider sphere)
            {
                snapshot.Shape = PlayerHitboxSnapshotShape.Sphere;
                snapshot.Center = collider_transform.TransformPoint(sphere.center);
                snapshot.Radius = sphere.radius * Max(scale.x, scale.y, scale.z);
                return true;
            }

            if (collider is CapsuleCollider capsule)
            {
                snapshot.Shape = PlayerHitboxSnapshotShape.Capsule;
                snapshot.Center = collider_transform.TransformPoint(capsule.center);
                snapshot.Radius = capsule.radius * GetCapsuleRadiusScale(scale, capsule.direction);
                snapshot.Height = Mathf.Max(
                    snapshot.Radius * 2f,
                    capsule.height * GetAxisScale(scale, capsule.direction));
                snapshot.Direction = capsule.direction;
                return true;
            }

            if (collider is BoxCollider box)
            {
                snapshot.Shape = PlayerHitboxSnapshotShape.Box;
                snapshot.Center = collider_transform.TransformPoint(box.center);
                snapshot.Size = Vector3.Scale(box.size, scale);
                return true;
            }

            return false;
        }

        private static bool TryRaycastSnapshot(
            PlayerHitboxSnapshot snapshot,
            Vector3 origin,
            Vector3 direction,
            float max_distance,
            out float distance)
        {
            return snapshot.Shape switch
            {
                PlayerHitboxSnapshotShape.Sphere => TryRaycastSphere(
                    origin,
                    direction,
                    max_distance,
                    snapshot.Center,
                    snapshot.Radius,
                    out distance),
                PlayerHitboxSnapshotShape.Capsule => TryRaycastCapsule(
                    origin,
                    direction,
                    max_distance,
                    snapshot,
                    out distance),
                _ => TryRaycastBox(
                    origin,
                    direction,
                    max_distance,
                    snapshot,
                    out distance),
            };
        }

        private static bool TryRaycastSphere(
            Vector3 origin,
            Vector3 direction,
            float max_distance,
            Vector3 center,
            float radius,
            out float distance)
        {
            distance = 0f;
            Vector3 origin_to_center = origin - center;
            float b = Vector3.Dot(origin_to_center, direction);
            float c = Vector3.Dot(origin_to_center, origin_to_center) - radius * radius;
            float discriminant = b * b - c;
            if (discriminant < 0f)
                return false;

            float sqrt = Mathf.Sqrt(discriminant);
            float first = -b - sqrt;
            float second = -b + sqrt;
            distance = first >= 0f ? first : second;
            return distance >= 0f && distance <= max_distance;
        }

        private static bool TryRaycastBox(
            Vector3 origin,
            Vector3 direction,
            float max_distance,
            PlayerHitboxSnapshot snapshot,
            out float distance)
        {
            distance = 0f;
            Quaternion inverse_rotation = Quaternion.Inverse(snapshot.Rotation);
            Vector3 local_origin = inverse_rotation * (origin - snapshot.Center);
            Vector3 local_direction = inverse_rotation * direction;
            Vector3 extents = snapshot.Size * 0.5f;

            float min = 0f;
            float max = max_distance;
            if (!IntersectSlab(local_origin.x, local_direction.x, extents.x, ref min, ref max) ||
                !IntersectSlab(local_origin.y, local_direction.y, extents.y, ref min, ref max) ||
                !IntersectSlab(local_origin.z, local_direction.z, extents.z, ref min, ref max))
            {
                return false;
            }

            distance = min;
            return distance >= 0f && distance <= max_distance;
        }

        private static bool IntersectSlab(
            float origin,
            float direction,
            float extent,
            ref float min,
            ref float max)
        {
            if (Mathf.Abs(direction) <= MinDistance)
                return origin >= -extent && origin <= extent;

            float inverse_direction = 1f / direction;
            float near = (-extent - origin) * inverse_direction;
            float far = (extent - origin) * inverse_direction;
            if (near > far)
                (near, far) = (far, near);

            min = Mathf.Max(min, near);
            max = Mathf.Min(max, far);
            return min <= max;
        }

        private static bool TryRaycastCapsule(
            Vector3 origin,
            Vector3 direction,
            float max_distance,
            PlayerHitboxSnapshot snapshot,
            out float distance)
        {
            GetCapsuleSegment(snapshot, out Vector3 start, out Vector3 end);
            if ((end - start).sqrMagnitude <= MinDistance * MinDistance)
            {
                return TryRaycastSphere(
                    origin,
                    direction,
                    max_distance,
                    snapshot.Center,
                    snapshot.Radius,
                    out distance);
            }

            if (TryRaycastInfiniteCapsuleSegment(
                    origin,
                    direction,
                    max_distance,
                    start,
                    end,
                    snapshot.Radius,
                    out distance))
            {
                return true;
            }

            bool has_start_hit = TryRaycastSphere(
                origin,
                direction,
                max_distance,
                start,
                snapshot.Radius,
                out float start_distance);
            bool has_end_hit = TryRaycastSphere(
                origin,
                direction,
                max_distance,
                end,
                snapshot.Radius,
                out float end_distance);

            if (has_start_hit && has_end_hit)
            {
                distance = Mathf.Min(start_distance, end_distance);
                return true;
            }

            if (has_start_hit)
            {
                distance = start_distance;
                return true;
            }

            distance = end_distance;
            return has_end_hit;
        }

        private static bool TryRaycastInfiniteCapsuleSegment(
            Vector3 origin,
            Vector3 direction,
            float max_distance,
            Vector3 start,
            Vector3 end,
            float radius,
            out float distance)
        {
            distance = 0f;
            Vector3 segment = end - start;
            Vector3 origin_to_start = origin - start;
            float segment_dot = Vector3.Dot(segment, segment);
            float segment_ray_dot = Vector3.Dot(segment, direction);
            float segment_origin_dot = Vector3.Dot(segment, origin_to_start);
            float ray_origin_dot = Vector3.Dot(direction, origin_to_start);
            float origin_dot = Vector3.Dot(origin_to_start, origin_to_start);

            float a = segment_dot - segment_ray_dot * segment_ray_dot;
            float b = segment_dot * ray_origin_dot - segment_origin_dot * segment_ray_dot;
            float c = segment_dot * origin_dot - segment_origin_dot * segment_origin_dot - radius * radius * segment_dot;
            float discriminant = b * b - a * c;
            if (Mathf.Abs(a) <= MinDistance || discriminant < 0f)
                return false;

            float candidate = (-b - Mathf.Sqrt(discriminant)) / a;
            float segment_projection = segment_origin_dot + candidate * segment_ray_dot;
            if (segment_projection <= 0f || segment_projection >= segment_dot)
                return false;

            distance = candidate;
            return distance >= 0f && distance <= max_distance;
        }

        private static void GetCapsuleSegment(
            PlayerHitboxSnapshot snapshot,
            out Vector3 start,
            out Vector3 end)
        {
            Vector3 axis = snapshot.Rotation * GetCapsuleAxis(snapshot.Direction);
            float half_segment = Mathf.Max(0f, snapshot.Height * 0.5f - snapshot.Radius);
            start = snapshot.Center - axis * half_segment;
            end = snapshot.Center + axis * half_segment;
        }

        private static bool IsValidTarget(PlayerCharacter shooter, PlayerHitboxLagCompensation target)
        {
            return target != null &&
                target._character != null &&
                target._health != null &&
                target._health.IsAlive &&
                target._character != shooter &&
                target.gameObject.scene == shooter.gameObject.scene;
        }

        private static bool IsUsableHitbox(PlayerHitbox hitbox)
        {
            return hitbox != null &&
                hitbox.isActiveAndEnabled &&
                hitbox.gameObject.activeInHierarchy &&
                hitbox.HitboxCollider != null;
        }

        private static int GetFrameIndex(int tick, int frame_count)
        {
            if (frame_count <= 0)
                return 0;

            int index = tick % frame_count;
            return index < 0 ? index + frame_count : index;
        }

        private static Vector3 GetCapsuleAxis(int direction)
        {
            return direction switch
            {
                0 => Vector3.right,
                2 => Vector3.forward,
                _ => Vector3.up,
            };
        }

        private static float GetAxisScale(Vector3 scale, int direction)
        {
            return direction switch
            {
                0 => scale.x,
                2 => scale.z,
                _ => scale.y,
            };
        }

        private static float GetCapsuleRadiusScale(Vector3 scale, int direction)
        {
            return direction switch
            {
                0 => Max(scale.y, scale.z),
                2 => Max(scale.x, scale.y),
                _ => Max(scale.x, scale.z),
            };
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
        }

        private static float Max(float a, float b, float c)
        {
            return Mathf.Max(a, Mathf.Max(b, c));
        }

        private static float Max(float a, float b)
        {
            return Mathf.Max(a, b);
        }
    }
}
