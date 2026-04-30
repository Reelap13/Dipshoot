using System;
using System.Collections.Generic;
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

        public TickManager TickManager { get; private set; }
        public InputBuffer InputBuffet { get; } = new();
        public StateBuffer StateBuffer { get; } = new();

        public bool IsServerSimulationInitialized { get; private set; }
        public bool IsClientSimulationInitialized { get; private set; }

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

            StateBuffer.Add(new()
            {
                Tick = TickManager.CurrentTick,
                Position = transform.position,
                Velocity = Vector3.zero,
                Rotation = transform.rotation,
                IsGrounded = false,
            });

            Debug.Log(
                $"{LogPrefix} Seeded initial state. netId={netId} " +
                $"tick={TickManager.CurrentTick} position={transform.position}");
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
    }
}
