using System;
using System.Collections.Generic;
using Game.MatchMode;
using Game.Players.State;
using Game.TickSystem;
using Mirror;
using UnityEngine;

namespace Game.Players
{
    public class PlayerCharacter : NetworkBehaviour
    {
        private const string LogPrefix = "[NetTick][Character]";

        [SerializeField] private List<PlayerCharacterComponent> _components;
        [SerializeField] private PlayerHealth _health;

        public TickManager TickManager { get; private set; }
        public PlayerHealth Health => _health == null ? _health = GetComponent<PlayerHealth>() : _health;
        public bool IsGameplayActive
        {
            get
            {
                TeamControlModeController mode_controller = ResolveSceneMatchModeController();
                return mode_controller == null || mode_controller.IsGameplayActive;
            }
        }
        public InputBuffer InputBuffet { get; } = new();
        public StateBuffer StateBuffer { get; } = new();

        public bool IsServerSimulationInitialized { get; private set; }
        public bool IsClientSimulationInitialized { get; private set; }
        private TeamControlModeController _match_mode_controller;

        public override void OnStartServer()
        {
            base.OnStartServer();
            TryInitializeForServerSimulation();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            TryInitializeForClientSimulation();
        }

        private void Update()
        {
            TryInitializeForServerSimulation();
            TryInitializeForClientSimulation();
        }

        public bool TryInitializeForServerSimulation()
        {
            if (!isServer || IsServerSimulationInitialized)
                return false;

            TickManager tick_manager = ResolveSceneTickManager();
            if (tick_manager == null)
                return false;

            InitializeServerSimulation(tick_manager);
            return true;
        }

        public bool TryInitializeForClientSimulation()
        {
            if (!isClient || IsClientSimulationInitialized)
                return false;

            TickManager tick_manager = ResolveSceneTickManager();
            if (tick_manager == null)
                return false;

            InitializeClientSimulation(tick_manager);
            return true;
        }

        public void InitializeServerSimulation(TickManager tick_manager)
        {
            InitializeInternal(tick_manager);
            IsServerSimulationInitialized = true;
            Debug.Log(
                $"{LogPrefix} Server simulation initialized. netId={netId} " +
                $"tick={TickManager?.CurrentTick}");
        }

        public void InitializeClientSimulation(TickManager tick_manager)
        {
            InitializeInternal(tick_manager);
            IsClientSimulationInitialized = true;
            Debug.Log(
                $"{LogPrefix} Client simulation initialized. netId={netId} " +
                $"owned={isOwned} tick={TickManager?.CurrentTick}");
        }

        private void InitializeInternal(TickManager tick_manager)
        {
            if (tick_manager == null)
                return;

            TickManager = tick_manager;

            SeedInitialState();

            foreach (var component in _components)
                component.Initialize(this);
        }

        private void SeedInitialState()
        {
            if (TickManager == null)
                return;

            AddInitialState(transform.position, transform.rotation);

            Debug.Log(
                $"{LogPrefix} Seeded initial state. netId={netId} " +
                $"tick={TickManager.CurrentTick} position={transform.position}");
        }

        public void ResetSimulationState(Vector3 position, Quaternion rotation)
        {
            if (TickManager == null)
                return;

            InputBuffet.Clear();
            StateBuffer.Clear();
            ResetSimulationComponents();
            transform.SetPositionAndRotation(position, rotation);
            AddInitialState(position, rotation);

            Debug.Log(
                $"{LogPrefix} Reset simulation state. netId={netId} " +
                $"tick={TickManager.CurrentTick} position={position}");
        }

        private void AddInitialState(Vector3 position, Quaternion rotation)
        {
            StateBuffer.Add(new()
            {
                Tick = TickManager.CurrentTick,
                Position = position,
                Velocity = Vector3.zero,
                Rotation = rotation,
                CameraPitch = 0f,
                IsGrounded = false,
                Stance = MovementStance.Standing,
                TimeSinceGrounded = float.MaxValue,
                TimeSinceJumpPressed = float.MaxValue,
            });
        }

        private void ResetSimulationComponents()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IPlayerSimulationResettable resettable)
                    resettable.ResetSimulation();
            }
        }

        private TickManager ResolveSceneTickManager()
        {
            GameObject[] root_objects = gameObject.scene.GetRootGameObjects();
            foreach (GameObject root_object in root_objects)
            {
                TickManager tick_manager = root_object.GetComponentInChildren<TickManager>(true);
                if (tick_manager != null)
                    return tick_manager;
            }

            return null;
        }

        private TeamControlModeController ResolveSceneMatchModeController()
        {
            if (_match_mode_controller != null)
                return _match_mode_controller;

            GameObject[] root_objects = gameObject.scene.GetRootGameObjects();
            foreach (GameObject root_object in root_objects)
            {
                TeamControlModeController mode_controller =
                    root_object.GetComponentInChildren<TeamControlModeController>(true);
                if (mode_controller == null)
                    continue;

                _match_mode_controller = mode_controller;
                return _match_mode_controller;
            }

            return null;
        }
    }
}
