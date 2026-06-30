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
        public ClientPopulationStore PopulationStore { get; private set; }
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
            PopulationStore = GetOrAddComponent<ClientPopulationStore>();
            InputRouter = GetOrAddComponent<ClientInputRouter>();
            CameraRouter = GetOrAddComponent<ClientCameraRouter>();
            PresentationRoot = GetOrAddComponent<ClientPresentationRoot>();
            SceneFlow = GetOrAddComponent<ClientSceneFlowController>();
            SceneLifecycle = GetOrAddComponent<ClientSceneLifecycleController>();
            LobbyActions = GetOrAddComponent<ClientLobbyActions>();

            EnsureEventSystem();
            CreatePersistentPrefabLayer<ClientMainMenuLayer>("ClientUI/MainMenu/ClientMainMenuLayer", "ClientMainMenuLayer");
            CreatePersistentPrefabLayer<ClientLobbyLayer>("ClientUI/Lobby/ClientLobbyLayer", "ClientLobbyLayer");
            CreatePersistentPrefabLayer<ClientErrorLayer>("ClientUI/System/ClientErrorLayer", "ClientErrorLayer");
            CreateMatchHudLayer();
            CreatePersistentPrefabLayer<ClientMatchScoreboardLayer>(
                "ClientUI/Match/Hud/Scoreboard/ClientMatchScoreboard",
                "ClientMatchScoreboard");
            CreatePersistentPrefabLayer<ClientKillFeedController>(
                "ClientUI/Match/Hud/KillFeed/ClientKillFeed",
                "ClientKillFeed");
            CreatePersistentPrefabLayer<ClientMatchPauseLayer>("ClientUI/Match/Pause/ClientMatchPauseLayer", "ClientMatchPauseLayer");
            CreatePersistentPrefabLayer<ClientMatchEndLayer>("ClientUI/Match/End/ClientMatchEndLayer", "ClientMatchEndLayer");
            CreatePersistentPrefabLayer<ClientDebugOverlayLayer>("ClientUI/System/ClientDebugOverlayLayer", "ClientDebugOverlayLayer");

            PresentationRoot.SetState(ClientPresentationState.MainMenu);
        }

        private void CreateMatchHudLayer()
        {
            if (Resources.Load<GameObject>("ClientUI/Match/Hud/ClientMatchHudV2Layer") != null)
            {
                CreatePersistentPrefabLayer<ClientMatchHudV2Layer>(
                    "ClientUI/Match/Hud/ClientMatchHudV2Layer",
                    "ClientMatchHudV2Layer");
                return;
            }

            CreatePersistentPrefabLayer<ClientMatchHudLayer>(
                "ClientUI/Match/Hud/Legacy/ClientMatchHudLayer",
                "ClientMatchHudLayer");
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
                GameObject fallback = new(fallback_name);
                fallback.transform.SetParent(transform, false);
                return fallback.AddComponent<T>();
            }

            GameObject instance = Instantiate(prefab, transform);
            instance.name = fallback_name;
            T component = instance.GetComponent<T>();
            if (component != null)
                return component;

            Debug.LogError($"Client UI prefab '{resources_path}' has no {typeof(T).Name} component");
            return instance.AddComponent<T>();
        }

        public void EnsureEventSystem()
        {
            EventSystem event_system = GetComponentInChildren<EventSystem>(true);
            if (event_system == null)
            {
                event_system = FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
                if (event_system != null)
                {
                    event_system.name = "ClientEventSystem";
                    event_system.transform.SetParent(transform, false);
                }
            }

            if (event_system == null)
            {
                GameObject target = new("ClientEventSystem");
                target.transform.SetParent(transform, false);
                event_system = target.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (event_system.GetComponent<InputSystemUIInputModule>() == null)
                event_system.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            if (event_system.GetComponent<StandaloneInputModule>() == null)
                event_system.gameObject.AddComponent<StandaloneInputModule>();
#endif
            event_system.gameObject.SetActive(true);
            event_system.enabled = true;
            EnableInputModules(event_system);
            DisableOtherEventSystems(event_system);
        }

        private static void EnableInputModules(EventSystem event_system)
        {
            BaseInputModule[] modules = event_system.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
                modules[i].enabled = true;
        }

        private static void DisableOtherEventSystems(EventSystem active_event_system)
        {
            EventSystem[] event_systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < event_systems.Length; i++)
            {
                EventSystem event_system = event_systems[i];
                if (event_system == null || event_system == active_event_system)
                    continue;

                event_system.enabled = false;
                BaseInputModule[] modules = event_system.GetComponents<BaseInputModule>();
                for (int j = 0; j < modules.Length; j++)
                    modules[j].enabled = false;
            }
        }
    }
}
