using Game.MatchMode;
using Game.Players.Input;
using Mirror;
using UnityEngine;

namespace Game.Players
{
    public static class WeaponShotResolver
    {
        public static ShotResult Resolve(
            PlayerCharacter shooter,
            WeaponDefinition weapon,
            WeaponStats weapon_stats,
            PlayerState shot_state,
            PlayerInputData input,
            WeaponSlotState fired_slot_state,
            int server_tick,
            int hitbox_visual_back_ticks,
            Vector3 eye_offset,
            LayerMask hit_mask,
            QueryTriggerInteraction trigger_interaction,
            RaycastHit[] hits)
        {
            Vector3 origin = GetShotOrigin(shot_state, eye_offset);
            int spread_seed = WeaponShotSeed.Get(shooter.netId, input.Tick, weapon.Slot, input.ShotSequence);
            Vector3 direction = GetShotDirection(shot_state, weapon_stats, spread_seed, fired_slot_state.ConsecutiveShots);
            bool has_world_hit = TryGetWorldHit(
                shooter,
                origin,
                direction,
                weapon_stats.Range,
                hit_mask,
                trigger_interaction,
                hits,
                out RaycastHit world_hit);
            float player_hit_range = has_world_hit ? world_hit.distance : weapon_stats.Range;
            int hitbox_snapshot_tick = ResolveHitboxSnapshotTick(
                input,
                server_tick,
                hitbox_visual_back_ticks);
            bool has_player_hit = PlayerHitboxLagCompensation.TryRaycast(
                shooter,
                hitbox_snapshot_tick,
                origin,
                direction,
                player_hit_range,
                out PlayerHitboxSnapshotHit player_hit);
            bool has_hit = has_player_hit || has_world_hit;
            Vector3 point = has_player_hit
                ? player_hit.Point
                : has_world_hit
                    ? world_hit.point
                    : origin + direction * weapon_stats.Range;
            Vector3 normal = has_player_hit
                ? -direction
                : has_world_hit
                    ? world_hit.normal
                    : -direction;

            ShotResult result = new()
            {
                ShooterNetId = shooter.netId,
                HitNetId = has_player_hit
                    ? player_hit.HitNetId
                    : has_world_hit
                        ? GetHitNetId(world_hit)
                        : 0,
                WeaponSlot = weapon.Slot,
                ShotSequence = input.ShotSequence,
                SpreadSeed = spread_seed,
                InputTick = input.Tick,
                ServerTick = server_tick,
                HitboxQueryTick = hitbox_snapshot_tick,
                HitboxSnapshotTick = has_player_hit ? player_hit.SnapshotTick : -1,
                Origin = origin,
                Direction = direction,
                Point = point,
                Normal = normal,
                Damage = weapon_stats.Damage,
                HitboxType = has_player_hit ? player_hit.HitboxType : PlayerHitboxType.None,
                DamageMultiplier = has_player_hit ? player_hit.DamageMultiplier : 1f,
                HasHit = has_hit,
                DidDamage = false,
            };

            TryApplyDamage(shooter, ref result, has_player_hit, player_hit);
            return result;
        }

        public static ShotResult ResolvePredicted(
            PlayerCharacter shooter,
            WeaponDefinition weapon,
            WeaponStats weapon_stats,
            PlayerState shot_state,
            PlayerInputData input,
            WeaponSlotState fired_slot_state,
            Vector3 eye_offset,
            LayerMask hit_mask,
            QueryTriggerInteraction trigger_interaction,
            RaycastHit[] hits)
        {
            Vector3 origin = GetShotOrigin(shot_state, eye_offset);
            int spread_seed = WeaponShotSeed.Get(shooter.netId, input.Tick, weapon.Slot, input.ShotSequence);
            Vector3 direction = GetShotDirection(shot_state, weapon_stats, spread_seed, fired_slot_state.ConsecutiveShots);
            bool has_hit = TryGetVisualHit(
                shooter,
                origin,
                direction,
                weapon_stats.Range,
                hit_mask,
                trigger_interaction,
                hits,
                out RaycastHit hit);
            PlayerHitbox hitbox = has_hit
                ? hit.collider.GetComponentInParent<PlayerHitbox>()
                : null;
            PlayerHealth hit_health = hitbox == null ? null : hitbox.Health;
            float damage_multiplier = hitbox == null ? 1f : hitbox.DamageMultiplier;
            bool did_damage = hit_health != null && !IsFriendlyTarget(shooter, hit_health);

            return new ShotResult
            {
                ShooterNetId = shooter.netId,
                HitNetId = hit_health != null ? hit_health.netId : has_hit ? GetHitNetId(hit) : 0,
                WeaponSlot = weapon.Slot,
                ShotSequence = input.ShotSequence,
                SpreadSeed = spread_seed,
                InputTick = input.Tick,
                ServerTick = -1,
                HitboxQueryTick = -1,
                HitboxSnapshotTick = -1,
                Origin = origin,
                Direction = direction,
                Point = has_hit ? hit.point : origin + direction * weapon_stats.Range,
                Normal = has_hit ? hit.normal : -direction,
                Damage = did_damage ? CalculateDamage(weapon_stats.Damage, damage_multiplier) : weapon_stats.Damage,
                HitboxType = hitbox == null ? PlayerHitboxType.None : hitbox.Type,
                DamageMultiplier = damage_multiplier,
                HasHit = has_hit,
                DidDamage = did_damage,
            };
        }

        public static Vector3 GetShotOrigin(PlayerState state, Vector3 eye_offset)
        {
            return state.Position + state.Rotation * eye_offset;
        }

        public static Vector3 GetShotDirection(
            PlayerState state,
            WeaponStats weapon_stats,
            int spread_seed,
            int shot_index)
        {
            Quaternion pitch_rotation = PlayerAimUtility.GetEffectivePitchRotation(state);
            Quaternion pattern_rotation = GetSprayPatternRotation(weapon_stats, shot_index);
            float spread_degrees = GetStateSpread(state, weapon_stats);
            Quaternion spread_rotation = GetSpreadRotation(spread_degrees, spread_seed);

            return state.Rotation * pitch_rotation * pattern_rotation * spread_rotation * Vector3.forward;
        }

        public static Vector2 GetSprayPatternOffset(WeaponStats weapon_stats, int shot_index)
        {
            WeaponRecoilPatternDefinition pattern = weapon_stats.RecoilPattern;
            Vector2[] offsets = pattern == null ? null : pattern.Pattern;
            if (offsets == null || offsets.Length == 0 || shot_index <= 0)
                return Vector2.zero;

            int index = Mathf.Clamp(shot_index - 1, 0, offsets.Length - 1);
            return offsets[index] * pattern.GameplayScale;
        }

        private static Quaternion GetSprayPatternRotation(WeaponStats weapon_stats, int shot_index)
        {
            Vector2 offset = GetSprayPatternOffset(weapon_stats, shot_index);
            return Quaternion.Euler(-offset.y, offset.x, 0f);
        }

        private static float GetStateSpread(PlayerState state, WeaponStats weapon_stats)
        {
            return Mathf.Min(weapon_stats.MaxSpread, weapon_stats.SpreadDegrees);
        }

        private static Quaternion GetSpreadRotation(
            float spread_degrees,
            int seed)
        {
            if (spread_degrees <= 0f)
                return Quaternion.identity;

            float yaw = LerpHash(seed, -spread_degrees, spread_degrees);
            float pitch = LerpHash(seed + 1, -spread_degrees, spread_degrees);
            return Quaternion.Euler(pitch, yaw, 0f);
        }

        private static float LerpHash(int seed, float min, float max)
        {
            return Mathf.Lerp(min, max, Hash01(seed));
        }

        private static float Hash01(int seed)
        {
            uint value = unchecked((uint)seed);
            value ^= value >> 16;
            value *= 0x7feb352d;
            value ^= value >> 15;
            value *= 0x846ca68b;
            value ^= value >> 16;
            return (value & 0x00ffffff) / 16777215f;
        }

        private static int ResolveHitboxSnapshotTick(
            PlayerInputData input,
            int server_tick,
            int hitbox_visual_back_ticks)
        {
            int back_ticks = Mathf.Max(0, hitbox_visual_back_ticks);
            int base_tick = input.Tick > 0 ? input.Tick : server_tick;

            return Mathf.Clamp(base_tick - back_ticks, 0, server_tick);
        }

        private static bool TryGetWorldHit(
            PlayerCharacter shooter,
            Vector3 origin,
            Vector3 direction,
            float range,
            LayerMask hit_mask,
            QueryTriggerInteraction trigger_interaction,
            RaycastHit[] hits,
            out RaycastHit hit)
        {
            hit = default;
            float closest_distance = float.MaxValue;
            bool has_hit = false;
            int hits_count = RaycastScene(
                shooter.gameObject,
                origin,
                direction,
                range,
                hit_mask,
                trigger_interaction,
                hits);

            for (int i = 0; i < hits_count; i++)
            {
                RaycastHit current_hit = hits[i];
                if (current_hit.collider == null || IsOwnCollider(shooter.transform, current_hit.collider))
                    continue;

                if (current_hit.collider.isTrigger)
                    continue;

                if (IsPlayerCollider(current_hit.collider))
                    continue;

                if (current_hit.distance >= closest_distance)
                    continue;

                closest_distance = current_hit.distance;
                hit = current_hit;
                has_hit = true;
            }

            return has_hit;
        }

        private static bool TryGetVisualHit(
            PlayerCharacter shooter,
            Vector3 origin,
            Vector3 direction,
            float range,
            LayerMask hit_mask,
            QueryTriggerInteraction trigger_interaction,
            RaycastHit[] hits,
            out RaycastHit hit)
        {
            hit = default;
            float closest_distance = float.MaxValue;
            bool has_hit = false;
            int hits_count = RaycastScene(
                shooter.gameObject,
                origin,
                direction,
                range,
                hit_mask,
                trigger_interaction,
                hits);

            for (int i = 0; i < hits_count; i++)
            {
                RaycastHit current_hit = hits[i];
                if (current_hit.collider == null ||
                    IsOwnCollider(shooter.transform, current_hit.collider) ||
                    IsNonHitboxPlayerCollider(current_hit.collider) ||
                    IsIgnoredVisualTrigger(current_hit.collider) ||
                    current_hit.distance >= closest_distance)
                {
                    continue;
                }

                closest_distance = current_hit.distance;
                hit = current_hit;
                has_hit = true;
            }

            return has_hit;
        }

        private static int RaycastScene(
            GameObject scene_context,
            Vector3 origin,
            Vector3 direction,
            float range,
            LayerMask hit_mask,
            QueryTriggerInteraction trigger_interaction,
            RaycastHit[] hits)
        {
            Physics.SyncTransforms();

            PhysicsScene physics_scene = scene_context.scene.GetPhysicsScene();
            if (!physics_scene.IsValid())
            {
                return Physics.RaycastNonAlloc(
                    origin,
                    direction,
                    hits,
                    range,
                    hit_mask,
                    trigger_interaction);
            }

            return physics_scene.Raycast(
                origin,
                direction,
                hits,
                range,
                hit_mask,
                trigger_interaction);
        }

        private static void TryApplyDamage(
            PlayerCharacter shooter,
            ref ShotResult result,
            bool has_player_hit,
            PlayerHitboxSnapshotHit hit)
        {
            if (!has_player_hit || hit.Health == null || IsFriendlyTarget(shooter, hit.Health))
                return;

            result.HitNetId = hit.Health.netId;
            result.HitboxType = hit.HitboxType;
            result.DamageMultiplier = hit.DamageMultiplier;
            int applied_damage = CalculateDamage(result.Damage, hit.DamageMultiplier);
            result.DidDamage = hit.Health.TryApplyDamage(
                applied_damage,
                result.ShooterNetId,
                result.HitboxType,
                result.DamageMultiplier);
            result.DidKill = result.DidDamage && !hit.Health.IsAlive;
            result.Damage = applied_damage;
        }

        private static int CalculateDamage(int base_damage, float damage_multiplier)
        {
            if (base_damage <= 0 || damage_multiplier <= 0f)
                return 0;

            return Mathf.Max(1, Mathf.RoundToInt(base_damage * damage_multiplier));
        }

        private static bool IsOwnCollider(Transform shooter, Collider target)
        {
            return target.transform == shooter || target.transform.IsChildOf(shooter);
        }

        private static bool IsPlayerCollider(Collider target)
        {
            return target.GetComponentInParent<PlayerHealth>() != null ||
                target.GetComponentInParent<PlayerHitbox>() != null;
        }

        private static bool IsIgnoredVisualTrigger(Collider target)
        {
            return target.isTrigger && target.GetComponentInParent<PlayerHitbox>() == null;
        }

        private static bool IsNonHitboxPlayerCollider(Collider target)
        {
            return target.GetComponentInParent<PlayerHitbox>() == null &&
                target.GetComponentInParent<PlayerHealth>() != null;
        }

        private static bool IsFriendlyTarget(PlayerCharacter shooter, PlayerHealth target_health)
        {
            PlayerMatchIdentity shooter_identity = shooter.GetComponent<PlayerMatchIdentity>();
            PlayerMatchIdentity target_identity = target_health.GetComponent<PlayerMatchIdentity>();

            return shooter_identity != null &&
                target_identity != null &&
                shooter_identity.TeamId != TeamId.None &&
                shooter_identity.TeamId == target_identity.TeamId;
        }

        private static uint GetHitNetId(RaycastHit hit)
        {
            NetworkIdentity identity = hit.collider.GetComponentInParent<NetworkIdentity>();
            return identity == null ? 0 : identity.netId;
        }
    }
}
