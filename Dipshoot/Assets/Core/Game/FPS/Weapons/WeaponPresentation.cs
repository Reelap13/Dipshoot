using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Players
{
    public static class WeaponPresentation
    {
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

            if (draw_tracer && (weapon == null || weapon.ShowDebugTracer))
                DrawShotTracer(owner, result, weapon);

            if (draw_marker && (weapon == null || weapon.ShowDebugHitMarker))
                DrawShotMarker(owner, result, weapon);
        }

        private static void DrawShotTracer(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon)
        {
            GameObject tracer = new("ShotTracer");
            MoveToObjectScene(owner, tracer);

            LineRenderer line_renderer = tracer.AddComponent<LineRenderer>();
            line_renderer.positionCount = 2;
            line_renderer.useWorldSpace = true;
            line_renderer.SetPosition(0, GetTracerOrigin(owner, result));
            line_renderer.SetPosition(1, result.Point);
            line_renderer.startWidth = weapon == null ? 0.03f : weapon.TracerWidth;
            line_renderer.endWidth = weapon == null ? 0.03f : weapon.TracerWidth;
            line_renderer.numCapVertices = 2;

            Material tracer_material = GetTracerMaterial();
            if (tracer_material != null)
                line_renderer.material = tracer_material;

            Color tracer_color = weapon == null ? Color.cyan : weapon.TracerColor;
            line_renderer.startColor = tracer_color;
            line_renderer.endColor = tracer_color;

            tracer.AddComponent<SelfDestroyer>().Initialize(weapon == null ? 0.12f : weapon.TracerLifetime);
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

        private static void DrawShotMarker(
            MonoBehaviour owner,
            ShotResult result,
            WeaponDefinition weapon)
        {
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
            Scene scene = owner.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.MoveGameObjectToScene(target, scene);
        }
    }
}
