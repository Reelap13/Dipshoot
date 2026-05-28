using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Players
{
    public class PlayerHitHighlightController : MonoBehaviour
    {
        private const string MaterialResourcePath = "Presentation/Materials/M_PlayerHitHighlight";
        private const float FadeInDuration = 0.2f;
        private const float HoldDuration = 0.2f;
        private const float FadeOutDuration = 0.2f;
        private const float MaxAlpha = 0.7f;
        private const float EndAlpha = 0.2f;

        [SerializeField] private Material _highlight_material;

        private readonly List<RenderTarget> _targets = new();
        private readonly MaterialPropertyBlock _properties = new();
        private float _started_at;
        private float _fade_in_start_alpha;
        private float _fade_in_duration = FadeInDuration;
        private float _current_alpha;
        private bool _is_playing;

        public void Play()
        {
            CacheTargets();
            EnsureMaterial();

            _fade_in_start_alpha = _is_playing ? _current_alpha : 0f;
            _fade_in_duration = Mathf.Max(
                0.001f,
                FadeInDuration * Mathf.Clamp01((MaxAlpha - _fade_in_start_alpha) / MaxAlpha));
            _started_at = Time.unscaledTime;
            _is_playing = true;
            enabled = true;
        }

        private void LateUpdate()
        {
            if (!_is_playing)
                return;

            _current_alpha = GetAlpha(Time.unscaledTime - _started_at);
            if (_current_alpha <= 0f)
            {
                _is_playing = false;
                enabled = false;
                return;
            }

            DrawHighlight(_current_alpha);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i].BakedMesh != null)
                    Destroy(_targets[i].BakedMesh);
            }
        }

        private float GetAlpha(float elapsed)
        {
            if (elapsed < _fade_in_duration)
                return Mathf.Lerp(_fade_in_start_alpha, MaxAlpha, elapsed / _fade_in_duration);

            elapsed -= _fade_in_duration;
            if (elapsed < HoldDuration)
                return MaxAlpha;

            elapsed -= HoldDuration;
            if (elapsed < FadeOutDuration)
                return Mathf.Lerp(MaxAlpha, EndAlpha, elapsed / FadeOutDuration);

            return 0f;
        }

        private void DrawHighlight(float alpha)
        {
            if (_highlight_material == null || _targets.Count == 0)
                return;

            _properties.SetFloat("_HitAlpha", alpha);

            for (int i = 0; i < _targets.Count; i++)
            {
                RenderTarget target = _targets[i];
                if (!target.IsValid)
                    continue;

                Mesh mesh = target.GetMesh();
                if (mesh == null)
                    continue;

                int submesh_count = Mathf.Max(1, mesh.subMeshCount);
                for (int submesh = 0; submesh < submesh_count; submesh++)
                {
                    Graphics.DrawMesh(
                        mesh,
                        target.LocalToWorldMatrix,
                        _highlight_material,
                        target.Layer,
                        null,
                        submesh,
                        _properties,
                        ShadowCastingMode.Off,
                        false,
                        null,
                        LightProbeUsage.Off,
                        null);
                }
            }
        }

        private void CacheTargets()
        {
            _targets.Clear();

            Transform root = transform;
            if (TryGetComponent(out PlayerVisualController visual_controller) &&
                visual_controller.ThirdPersonRoot != null)
            {
                root = visual_controller.ThirdPersonRoot;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is SkinnedMeshRenderer skinned_mesh_renderer)
                {
                    _targets.Add(RenderTarget.Create(skinned_mesh_renderer));
                    continue;
                }

                if (renderers[i] is MeshRenderer mesh_renderer &&
                    mesh_renderer.TryGetComponent(out MeshFilter mesh_filter) &&
                    mesh_filter.sharedMesh != null)
                {
                    _targets.Add(RenderTarget.Create(mesh_renderer, mesh_filter));
                }
            }
        }

        private void EnsureMaterial()
        {
            if (_highlight_material != null)
                return;

            _highlight_material = Resources.Load<Material>(MaterialResourcePath);
        }

        private sealed class RenderTarget
        {
            private readonly MeshRenderer _mesh_renderer;
            private readonly MeshFilter _mesh_filter;
            private readonly SkinnedMeshRenderer _skinned_renderer;

            public readonly Mesh BakedMesh;

            public bool IsValid => Renderer != null && Renderer.enabled && Renderer.gameObject.activeInHierarchy;
            public int Layer => Renderer.gameObject.layer;
            public Matrix4x4 LocalToWorldMatrix => Renderer.transform.localToWorldMatrix;
            private Renderer Renderer => _skinned_renderer != null ? _skinned_renderer : _mesh_renderer;

            private RenderTarget(MeshRenderer mesh_renderer, MeshFilter mesh_filter)
            {
                _mesh_renderer = mesh_renderer;
                _mesh_filter = mesh_filter;
            }

            private RenderTarget(SkinnedMeshRenderer skinned_renderer)
            {
                _skinned_renderer = skinned_renderer;
                BakedMesh = new Mesh { name = $"{skinned_renderer.name}_HitHighlight" };
                BakedMesh.hideFlags = HideFlags.DontSave;
            }

            public static RenderTarget Create(MeshRenderer mesh_renderer, MeshFilter mesh_filter)
            {
                return new RenderTarget(mesh_renderer, mesh_filter);
            }

            public static RenderTarget Create(SkinnedMeshRenderer skinned_renderer)
            {
                return new RenderTarget(skinned_renderer);
            }

            public Mesh GetMesh()
            {
                if (_skinned_renderer != null)
                {
                    _skinned_renderer.BakeMesh(BakedMesh);
                    return BakedMesh;
                }

                return _mesh_filter == null ? null : _mesh_filter.sharedMesh;
            }
        }
    }
}
