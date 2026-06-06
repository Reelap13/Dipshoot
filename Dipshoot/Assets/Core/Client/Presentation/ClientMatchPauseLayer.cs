using Game.MatchMode;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMatchPauseLayer : MonoBehaviour
    {
        [SerializeField] private Slider _mouse_sensitivity_slider;
        [SerializeField] private TextMeshProUGUI _mouse_sensitivity_value_text;
        [SerializeField] private Slider _master_volume_slider;
        [SerializeField] private TextMeshProUGUI _master_volume_value_text;
        [SerializeField] private RawImage _map_image;
        [SerializeField] private Camera _map_camera;
        [SerializeField] private AudioMixer _audio_mixer;
        [SerializeField] private string _master_volume_parameter = "MasterVolume";
        [SerializeField] private float _map_padding = 1.12f;
        [SerializeField] private float _map_rotation_degrees = 90f;

        private ClientUiLayer _layer;
        private RenderTexture _map_texture;
        private bool _is_intro_auto_open;
        private int _last_intro_round = -1;

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.MatchPause);

            InitializeSliders();
            InitializeMapCamera();
            ApplyMasterVolume(ClientGameplaySettings.MasterVolume);
        }

        private void OnDestroy()
        {
            if (_mouse_sensitivity_slider != null)
                _mouse_sensitivity_slider.onValueChanged.RemoveListener(SetMouseSensitivity);
            if (_master_volume_slider != null)
                _master_volume_slider.onValueChanged.RemoveListener(SetMasterVolume);

            if (_map_texture != null)
            {
                _map_texture.Release();
                Destroy(_map_texture);
            }
        }

        private void Update()
        {
            HandleEscape();
            HandleIntroAutoOpen();

            bool is_visible = ClientAppRoot.Instance.PresentationRoot.State == ClientPresentationState.MatchPause;
            if (_map_camera != null)
                _map_camera.enabled = is_visible;
        }

        private void HandleEscape()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            ClientPresentationRoot presentation = ClientAppRoot.Instance.PresentationRoot;
            if (presentation.State == ClientPresentationState.Match)
            {
                OpenPause(false);
                return;
            }

            if (presentation.State == ClientPresentationState.MatchPause)
                ClosePause();
        }

        private void HandleIntroAutoOpen()
        {
            TeamControlModeController mode = ClientAppRoot.Instance.MatchStore.ModeController;
            if (mode == null)
                return;

            if (mode.Phase == RoundPhase.Intro && mode.CurrentRound != _last_intro_round)
            {
                if (OpenPause(true))
                    _last_intro_round = mode.CurrentRound;
                return;
            }

            if (_is_intro_auto_open &&
                mode.Phase == RoundPhase.Playing &&
                ClientAppRoot.Instance.PresentationRoot.State == ClientPresentationState.MatchPause)
            {
                ClosePause();
            }
        }

        private bool OpenPause(bool is_intro_auto_open)
        {
            ClientPresentationRoot presentation = ClientAppRoot.Instance.PresentationRoot;
            if (presentation.State != ClientPresentationState.Match)
                return false;

            _is_intro_auto_open = is_intro_auto_open;
            FrameMapCamera();
            presentation.SetState(ClientPresentationState.MatchPause);
            return true;
        }

        private void ClosePause()
        {
            _is_intro_auto_open = false;
            ClientAppRoot.Instance.PresentationRoot.SetState(ClientPresentationState.Match);
        }

        private void InitializeSliders()
        {
            if (_mouse_sensitivity_slider != null)
            {
                _mouse_sensitivity_slider.minValue = ClientGameplaySettings.MinMouseSensitivity;
                _mouse_sensitivity_slider.maxValue = ClientGameplaySettings.MaxMouseSensitivity;
                _mouse_sensitivity_slider.SetValueWithoutNotify(ClientGameplaySettings.MouseSensitivity);
                _mouse_sensitivity_slider.onValueChanged.AddListener(SetMouseSensitivity);
            }

            if (_master_volume_slider != null)
            {
                _master_volume_slider.minValue = 0f;
                _master_volume_slider.maxValue = 1f;
                _master_volume_slider.SetValueWithoutNotify(ClientGameplaySettings.MasterVolume);
                _master_volume_slider.onValueChanged.AddListener(SetMasterVolume);
            }

            UpdateValueTexts();
        }

        private void SetMouseSensitivity(float value)
        {
            ClientGameplaySettings.SetMouseSensitivity(value);
            UpdateValueTexts();
        }

        private void SetMasterVolume(float value)
        {
            ClientGameplaySettings.SetMasterVolume(value);
            ApplyMasterVolume(value);
            UpdateValueTexts();
        }

        private void ApplyMasterVolume(float value)
        {
            if (_audio_mixer == null || string.IsNullOrWhiteSpace(_master_volume_parameter))
                return;

            float decibels = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
            _audio_mixer.SetFloat(_master_volume_parameter, decibels);
        }

        private void UpdateValueTexts()
        {
            if (_mouse_sensitivity_value_text != null)
                _mouse_sensitivity_value_text.text = $"{ClientGameplaySettings.MouseSensitivity:0.00}x";

            if (_master_volume_value_text != null)
                _master_volume_value_text.text = $"{Mathf.RoundToInt(ClientGameplaySettings.MasterVolume * 100f)}%";
        }

        private void InitializeMapCamera()
        {
            if (_map_texture == null)
            {
                _map_texture = new RenderTexture(768, 512, 16, RenderTextureFormat.ARGB32)
                {
                    name = "ClientPauseMap"
                };
                _map_texture.Create();
            }

            if (_map_image != null)
                _map_image.texture = _map_texture;

            if (_map_camera == null)
            {
                GameObject target = new("PauseMapCamera");
                target.transform.SetParent(transform, false);
                _map_camera = target.AddComponent<Camera>();
            }

            _map_camera.enabled = false;
            _map_camera.orthographic = true;
            _map_camera.clearFlags = CameraClearFlags.SolidColor;
            _map_camera.backgroundColor = new Color(0.04f, 0.045f, 0.05f, 1f);
            _map_camera.cullingMask = CreateMapCullingMask();
            _map_camera.targetTexture = _map_texture;
        }

        private void FrameMapCamera()
        {
            if (_map_camera == null)
                return;

            if (!TryGetMapBounds(out Bounds bounds))
            {
                _map_camera.transform.SetPositionAndRotation(
                    new Vector3(0f, 100f, 0f),
                    Quaternion.Euler(90f, 0f, _map_rotation_degrees));
                _map_camera.orthographicSize = 60f;
                return;
            }

            float aspect = _map_texture != null && _map_texture.height > 0
                ? (float)_map_texture.width / _map_texture.height
                : 1.5f;
            bool quarter_turn = Mathf.Abs(Mathf.DeltaAngle(_map_rotation_degrees, 90f)) < 1f ||
                Mathf.Abs(Mathf.DeltaAngle(_map_rotation_degrees, 270f)) < 1f;
            float size = quarter_turn
                ? Mathf.Max(bounds.extents.x, bounds.extents.z / aspect) * _map_padding
                : Mathf.Max(bounds.extents.z, bounds.extents.x / aspect) * _map_padding;
            float height = Mathf.Max(80f, bounds.size.y + size);
            Vector3 center = bounds.center;

            _map_camera.transform.SetPositionAndRotation(
                new Vector3(center.x, bounds.max.y + height, center.z),
                Quaternion.Euler(90f, 0f, _map_rotation_degrees));
            _map_camera.orthographicSize = Mathf.Max(20f, size);
            _map_camera.nearClipPlane = 0.1f;
            _map_camera.farClipPlane = height + bounds.size.y + 50f;
        }

        private bool TryGetMapBounds(out Bounds bounds)
        {
            bounds = default;
            bool has_bounds = false;
            Scene active_scene = SceneManager.GetActiveScene();
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int culling_mask = CreateMapCullingMask();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    renderer.gameObject.scene != active_scene ||
                    !renderer.enabled ||
                    (culling_mask & (1 << renderer.gameObject.layer)) == 0)
                {
                    continue;
                }

                if (!has_bounds)
                {
                    bounds = renderer.bounds;
                    has_bounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return has_bounds;
        }

        private static int CreateMapCullingMask()
        {
            int mask = ~0;
            ClearLayer(ref mask, "UI");
            ClearLayer(ref mask, "Ignore Raycast");
            ClearLayer(ref mask, "CharacterControllers");
            ClearLayer(ref mask, "CharacterFirstPerson");
            ClearLayer(ref mask, "CharacterExternal");
            ClearLayer(ref mask, "CharacterPhysics");
            ClearLayer(ref mask, "CharacterRagdoll");
            ClearLayer(ref mask, "CharacterNonColliding");
            ClearLayer(ref mask, "WieldablesFirstPerson");
            ClearLayer(ref mask, "WieldablesExternal");
            ClearLayer(ref mask, "Effects");
            return mask;
        }

        private static void ClearLayer(ref int mask, string layer_name)
        {
            int layer = LayerMask.NameToLayer(layer_name);
            if (layer >= 0)
                mask &= ~(1 << layer);
        }

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }
    }
}
