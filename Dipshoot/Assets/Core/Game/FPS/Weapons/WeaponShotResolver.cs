using Game.MatchMode;
using Game.Players.Input;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            int server_tick,
            Vector3 eye_offset,
            LayerMask hit_mask,
            QueryTriggerInteraction trigger_interaction,
            RaycastHit[] hits)
        {
            Vector3 origin = GetShotOrigin(shot_state, eye_offset);
            Vector3 direction = GetShotDirection(shot_state, weapon_stats, shooter.netId, input.Tick, weapon.Slot);
            bool has_hit = TryGetShotHit(
                shooter,
                origin,
                direction,
                weapon_stats.Range,
                hit_mask,
                trigger_interaction,
                hits,
                out RaycastHit hit);
            Vector3 point = has_hit ? hit.point : origin + direction * weapon_stats.Range;

            ShotResult result = new()
            {
                ShooterNetId = shooter.netId,
                HitNetId = has_hit ? GetHitNetId(hit) : 0,
                WeaponSlot = weapon.Slot,
                InputTick = input.Tick,
                ServerTick = server_tick,
                Origin = origin,
                Direction = direction,
                Point = point,
                Damage = weapon_stats.Damage,
                HasHit = has_hit,
                DidDamage = false,
            };

            TryApplyDamage(shooter, ref result, hit);
            return result;
        }

        private static Vector3 GetShotOrigin(PlayerState state, Vector3 eye_offset)
        {
            return state.Position + state.Rotation * eye_offset;
        }

        private static Vector3 GetShotDirection(
            PlayerState state,
            WeaponStats weapon_stats,
            uint shooter_net_id,
            int input_tick,
            WeaponSlot weapon_slot)
        {
            Quaternion pitch_rotation = Quaternion.Euler(state.CameraPitch, 0f, 0f);
            Quaternion spread_rotation = GetSpreadRotation(
                weapon_stats.SpreadDegrees,
                shooter_net_id,
                input_tick,
                weapon_slot);

            return state.Rotation * pitch_rotation * spread_rotation * Vector3.forward;
        }

        private static Quaternion GetSpreadRotation(
            float spread_degrees,
            uint shooter_net_id,
            int input_tick,
            WeaponSlot weapon_slot)
        {
            if (spread_degrees <= 0f)
                return Quaternion.identity;

            int seed = unchecked((int)shooter_net_id * 73856093 ^ input_tick * 19349663 ^ (int)weapon_slot * 83492791);
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

        private static bool TryGetShotHit(
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
            RaycastHit hit)
        {
            if (!result.HasHit || hit.collider == null)
                return;

            PlayerHealth health = hit.collider.GetComponentInParent<PlayerHealth>();
            if (health == null || IsFriendlyTarget(shooter, health))
                return;

            result.HitNetId = health.netId;
            result.DidDamage = health.TryApplyDamage(result.Damage, result.ShooterNetId);
        }

        private static bool IsOwnCollider(Transform shooter, Collider target)
        {
            return target.transform == shooter || target.transform.IsChildOf(shooter);
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
