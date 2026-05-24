using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientUiLayer : MonoBehaviour
    {
        [SerializeField] private ClientUiLayerKind _kind;

        private CanvasGroup _canvas_group;
        private GraphicRaycaster[] _raycasters;
        private bool _is_registered;
        private bool _is_initialized;

        public ClientUiLayerKind Kind => _kind;

        public void Initialize(ClientUiLayerKind kind)
        {
            if (_is_registered)
                Unregister();

            _kind = kind;
            _is_initialized = true;
            CacheReferences();
            Register();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            if (!_is_initialized)
                return;

            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void SetVisible(bool is_visible)
        {
            if (!CacheReferences())
                return;

            _canvas_group.alpha = is_visible ? 1f : 0f;
            _canvas_group.interactable = is_visible;
            _canvas_group.blocksRaycasts = is_visible;

            foreach (GraphicRaycaster raycaster in _raycasters)
            {
                if (raycaster == null)
                    continue;

                raycaster.enabled = is_visible;
            }
        }

        private bool CacheReferences()
        {
            if (_canvas_group == null)
            {
                if (!TryGetComponent(out _canvas_group))
                    _canvas_group = gameObject.AddComponent<CanvasGroup>();
            }

            if (_canvas_group == null)
                return false;

            _raycasters = GetComponentsInChildren<GraphicRaycaster>(true);
            return true;
        }

        private void Register()
        {
            if (!_is_initialized || _is_registered || !ClientAppRoot.HasInstance)
                return;

            ClientAppRoot.Instance.PresentationRoot.RegisterLayer(this);
            _is_registered = true;
        }

        private void Unregister()
        {
            if (!_is_registered || !ClientAppRoot.HasInstance)
                return;

            ClientAppRoot.Instance.PresentationRoot.UnregisterLayer(this);
            _is_registered = false;
        }
    }
}
