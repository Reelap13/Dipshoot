using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Core.ClientPresentation
{
    public class ClientAppRoot : MonoBehaviour
    {
        private static ClientAppRoot _instance;

        private bool _is_bootstrapped;

        public static ClientAppRoot Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                ClientAppRoot root = FindAnyObjectByType<ClientAppRoot>();
                if (root != null)
                {
                    _instance = root;
                    root.Bootstrap();
                    return root;
                }

                GameObject target = new("ClientAppRoot");
                return target.AddComponent<ClientAppRoot>();
            }
        }

        public static bool HasInstance => _instance != null;

        public ClientSessionStore SessionStore { get; private set; }
        public ClientLobbyStore LobbyStore { get; private set; }
        public ClientMatchStore MatchStore { get; private set; }
        public ClientInputRouter InputRouter { get; private set; }
        public ClientCameraRouter CameraRouter { get; private set; }
        public ClientPresentationRoot PresentationRoot { get; private set; }
        public ClientSceneFlowController SceneFlow { get; private set; }
        public ClientSceneLifecycleController SceneLifecycle { get; private set; }
        public ClientLobbyActions LobbyActions { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Bootstrap();
        }

        private void Bootstrap()
        {
            if (_is_bootstrapped)
                return;

            _is_bootstrapped = true;
            SessionStore = GetOrAddComponent<ClientSessionStore>();
            LobbyStore = GetOrAddComponent<ClientLobbyStore>();
            MatchStore = GetOrAddComponent<ClientMatchStore>();
            InputRouter = GetOrAddComponent<ClientInputRouter>();
            CameraRouter = GetOrAddComponent<ClientCameraRouter>();
            PresentationRoot = GetOrAddComponent<ClientPresentationRoot>();
            SceneFlow = GetOrAddComponent<ClientSceneFlowController>();
            SceneLifecycle = GetOrAddComponent<ClientSceneLifecycleController>();
            LobbyActions = GetOrAddComponent<ClientLobbyActions>();

            EnsureEventSystem();
            CreatePersistentPrefabLayer<ClientMainMenuLayer>("ClientUI/ClientMainMenuLayer", "ClientMainMenuLayer");
            CreatePersistentPrefabLayer<ClientLobbyLayer>("ClientUI/ClientLobbyLayer", "ClientLobbyLayer");
            CreatePersistentPrefabLayer<ClientErrorLayer>("ClientUI/ClientErrorLayer", "ClientErrorLayer");
            CreatePersistentPrefabLayer<ClientMatchHudLayer>("ClientUI/ClientMatchHudLayer", "ClientMatchHudLayer");
            CreatePersistentPrefabLayer<ClientLoadingLayer>("ClientUI/ClientLoadingLayer", "ClientLoadingLayer");

            PresentationRoot.SetState(ClientPresentationState.MainMenu);
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private T CreatePersistentPrefabLayer<T>(string resources_path, string fallback_name) where T : Component
        {
            T existing = GetComponentInChildren<T>(true);
            if (existing != null)
                return existing;

            GameObject prefab = Resources.Load<GameObject>(resources_path);
            if (prefab == null)
            {
                Debug.LogError($"Missing client UI prefab at Resources/{resources_path}");
                return null;
            }

            GameObject instance = Instantiate(prefab, transform);
            instance.name = fallback_name;
            T component = instance.GetComponent<T>();
            if (component != null)
                return component;

            Debug.LogError($"Client UI prefab '{resources_path}' has no {typeof(T).Name} component");
            return null;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject target = new("ClientEventSystem");
            target.transform.SetParent(transform, false);
            target.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            target.AddComponent<InputSystemUIInputModule>();
#else
            target.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
