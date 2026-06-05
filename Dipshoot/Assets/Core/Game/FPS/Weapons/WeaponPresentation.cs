using System.Collections.Generic;
using Core.ClientPresentation;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Players
{
    public static class WeaponPresentation
    {
        private static readonly Dictionary<int, List<GameObject>> TransientShotVfx = new();
        private static Material _tracer_material;

        public static void DrawShot(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon)
        {
            PlayRemoteShot(owner, result, weapon);
        }

        public static void PlayPredictedShot(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon)
        {
            PlayShotVisual(owner, result, weapon, true, true, false);
        }

        public static void PlayOwnerHitFeedback(ShotResult result)
        {
            if (!result.DidDamage)
                return;

            ClientMatchHudLayer.PlayLocalHitMarker();

            if (result.HitNetId == 0 ||
                !NetworkClient.spawned.TryGetValue(result.HitNetId, out NetworkIdentity identity) ||
                identity == null)
            {
                return;
            }

            PlayerHitHighlightController highlight_controller =
                identity.GetComponent<PlayerHitHighlightController>();
            if (highlight_controller == null)
                highlight_controller = identity.gameObject.AddComponent<PlayerHitHighlightController>();

            highlight_controller.Play();
        }

        public static void PlayConfirmedOwnerShot(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon)
        {
            PlayShotVisual(owner, result, weapon, false, false, true);
        }

        public static void PlayRemoteShot(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon)
        {
            PlayShotVisual(owner, result, weapon, true, true, true);
        }

        public static void ClearTransientShotVfx(MonoBehaviour owner)
        {
            if (owner == null || !TransientShotVfx.TryGetValue(owner.GetInstanceID(), out List<GameObject> vfx))
                return;

            for (int i = 0; i < vfx.Count; i++)
            {
                if (vfx[i] != null)
                    Object.Destroy(vfx[i]);
            }

            vfx.Clear();
        }

        private static void PlayShotVisual(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon,
            bool play_weapon_visual,
            bool draw_tracer,
            bool draw_marker)
        {
            if (play_weapon_visual &&
                owner != null &&
                owner.TryGetComponent(out PlayerWeaponVisualController weapon_visual_controller))
            {
                weapon_visual_controller.PlayShot(result);
            }

            if (play_weapon_visual &&
                owner != null &&
                owner.TryGetComponent(out PlayerAnimationController animation_controller))
            {
                animation_controller.PlayShot(result);
            }

            if (draw_tracer)
                DrawShotTracer(owner, result, weapon);

            if (draw_marker)
                DrawShotImpact(owner, result, weapon);
        }

        private static void DrawShotTracer(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon)
        {
            WeaponVisualDefinition visual = weapon == null ? null : weapon.Visual;
            if (visual != null && visual.TracerPrefab != null)
            {
                SpawnProjectileTracer(owner, result, visual);
                return;
            }

            if (weapon != null && !weapon.ShowDebugTracer)
                return;

            WeaponVfxUtility.LogWarningOnce(
                owner,
                $"MissingTracer:{weapon?.name}",
                $"[WeaponVFX] Missing tracer prefab for {weapon?.name}. Using fallback tracer.");

            GameObject tracer = new("ShotTracer");
            MoveToObjectScene(owner, tracer);
            RegisterTransient(owner, tracer);

            LineRenderer line_renderer = tracer.AddComponent<LineRenderer>();
            line_renderer.positionCount = 2;
            line_renderer.useWorldSpace = true;
            Vector3 origin = GetTracerOrigin(owner, result);
            Vector3 direction = (result.Point - origin).normalized;
            float distance = Vector3.Distance(origin, result.Point);
            line_renderer.SetPosition(0, origin);
            line_renderer.SetPosition(1, origin + direction * Mathf.Min(3f, distance));
            line_renderer.startWidth = visual == null ? weapon == null ? 0.03f : weapon.TracerWidth : visual.TracerStartWidth;
            line_renderer.endWidth = visual == null ? weapon == null ? 0.03f : weapon.TracerWidth : visual.TracerEndWidth;
            line_renderer.numCapVertices = 2;

            Material tracer_material = GetTracerMaterial();
            if (tracer_material != null)
                line_renderer.material = tracer_material;

            Color tracer_color = visual == null ? weapon == null ? Color.cyan : weapon.TracerColor : visual.TracerColor;
            line_renderer.startColor = tracer_color;
            line_renderer.endColor = tracer_color;

            float speed = visual == null ? 420f : visual.TracerVisualSpeed;
            float min_visible_time = visual == null ? weapon == null ? 0.045f : weapon.TracerLifetime : visual.TracerMinVisibleTime;
            float fade_time = visual == null ? 0.08f : visual.TracerFadeTime;
            float length = visual == null ? 2.2f : visual.TracerLength;
            tracer.AddComponent<ProjectileTracerVfx>().Initialize(
                origin,
                result.Point,
                speed,
                min_visible_time,
                fade_time,
                length);
        }

        private static void SpawnProjectileTracer(
            MonoBehaviour owner,
            ShotResult result,
            WeaponVisualDefinition visual)
        {
            Vector3 origin = GetTracerOrigin(owner, result);
            GameObject tracer = Object.Instantiate(visual.TracerPrefab, origin, Quaternion.identity);
            tracer.name = "ShotTracer";
            MoveToObjectScene(owner, tracer);
            RegisterTransient(owner, tracer);
            WeaponVfxUtility.PlayParticles(tracer);
            tracer.AddComponent<ProjectileTracerVfx>().Initialize(
                origin,
                result.Point,
                visual.TracerVisualSpeed,
                visual.TracerMinVisibleTime,
                visual.TracerFadeTime,
                visual.TracerLength);
        }

        private static Vector3 GetTracerOrigin(MonoBehaviour owner, ShotResult result)
        {
            if (owner != null &&
                owner.TryGetComponent(out PlayerWeaponVisualController weapon_visual_controller) &&
                weapon_visual_controller.TryGetShotTracerOrigin(result.WeaponSlot, out Vector3 origin))
            {
                return origin;
            }

            return result.Origin;
        }

        private static void DrawShotImpact(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon)
        {
            if (!result.HasHit)
                return;

            WeaponVisualDefinition visual = weapon == null ? null : weapon.Visual;
            GameObject prefab = result.HitboxType == PlayerHitboxType.None
                ? visual == null ? null : visual.WorldImpactPrefab
                : visual == null ? null : visual.PlayerImpactPrefab;

            if (prefab != null)
            {
                Quaternion rotation = result.Normal.sqrMagnitude < 0.0001f
                    ? Quaternion.LookRotation(-result.Direction)
                    : Quaternion.LookRotation(result.Normal);
                GameObject impact = Object.Instantiate(prefab, result.Point, rotation);
                impact.name = result.HitboxType == PlayerHitboxType.None ? "WorldImpact" : "PlayerImpact";
                MoveToObjectScene(owner, impact);
                WeaponVfxUtility.PlayParticles(impact);
                impact.AddComponent<SelfDestroyer>().Initialize(visual.ImpactLifetime);
                return;
            }

            if (weapon != null && !weapon.ShowDebugHitMarker)
                return;

            WeaponVfxUtility.LogWarningOnce(
                owner,
                $"MissingImpact:{weapon?.name}:{result.HitboxType}",
                $"[WeaponVFX] Missing impact prefab for {weapon?.name}. Using fallback marker.");

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = result.HasHit ? "ShotHitConfirm" : "ShotMissConfirm";
            marker.transform.position = result.Point;
            marker.transform.localScale = Vector3.one * GetMarkerSize(result, weapon);

            MoveToObjectScene(owner, marker);

            if (marker.TryGetComponent(out Collider marker_collider))
                Object.Destroy(marker_collider);

            if (marker.TryGetComponent(out Renderer marker_renderer))
                marker_renderer.material.color = GetMarkerColor(result, weapon);

            marker.AddComponent<SelfDestroyer>().Initialize(weapon == null ? 0.6f : weapon.MarkerLifetime);
        }

        private static float GetMarkerSize(ShotResult result, WeaponDefinition weapon)
        {
            if (weapon == null)
                return result.HasHit ? 0.18f : 0.1f;

            return result.HasHit ? weapon.HitMarkerSize : weapon.MissMarkerSize;
        }

        private static Color GetMarkerColor(ShotResult result, WeaponDefinition weapon)
        {
            if (weapon == null)
                return result.HasHit ? Color.red : Color.yellow;

            return result.HasHit ? weapon.HitColor : weapon.MissColor;
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

        private static void MoveToObjectScene(MonoBehaviour owner, GameObject target)
        {
            if (owner == null || target == null)
                return;

            Scene scene = owner.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.MoveGameObjectToScene(target, scene);
        }

        private static void RegisterTransient(MonoBehaviour owner, GameObject target)
        {
            if (owner == null || target == null)
                return;

            int id = owner.GetInstanceID();
            if (!TransientShotVfx.TryGetValue(id, out List<GameObject> vfx))
            {
                vfx = new List<GameObject>();
                TransientShotVfx[id] = vfx;
            }

            for (int i = vfx.Count - 1; i >= 0; i--)
            {
                if (vfx[i] == null)
                    vfx.RemoveAt(i);
            }

            vfx.Add(target);
        }
    }
}
