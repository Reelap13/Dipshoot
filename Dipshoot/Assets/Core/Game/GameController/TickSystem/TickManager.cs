using System.Collections.Generic;
using UnityEngine;

namespace Game.TickSystem
{
    public enum TickLayer
    {
        InputCollect = 0,
        InputSend = 10,
        AimSimulation = 50,
        WeaponInput = 100,
        WeaponSimulation = 110,
        Movement = 200,
        PhysicsResolve = 300,
        Objective = 500,
        MatchState = 800,
        AnimationPose = 840,
        HitboxSnapshot = 850,
        StateSnapshot = 900,
        Cleanup = 1000,
    }

    public readonly struct GameTickContext
    {
        public readonly TickManager TickManager;
        public readonly int Tick;
        public readonly float DeltaTime;

        public GameTickContext(TickManager tick_manager, int tick, float delta_time)
        {
            TickManager = tick_manager;
            Tick = tick;
            DeltaTime = delta_time;
        }
    }

    public interface ITickSystem
    {
        TickLayer TickLayer { get; }
        int TickOrder { get; }

        bool ShouldTick(GameTickContext context);
        void Tick(GameTickContext context);
    }

    public class TickManager : MonoBehaviour
    {
        public int TickRate = 60;
        public int MaxTicksPerFrame = 8;

        public float TickDelta => TickRate <= 0 ? 0f : 1f / TickRate;
        public float TickProgress => TickDelta <= 0f ? 0f : _accumulator / TickDelta;

        public int CurrentTick { get; private set; } = 0;

        private float _accumulator;
        private int _register_order;
        private readonly List<TickSystemEntry> _systems = new();

        void Update()
        {
            if (TickRate <= 0)
                return;

            float max_accumulator = TickDelta * MaxTicksPerFrame;
            _accumulator = Mathf.Min(_accumulator + Time.deltaTime, max_accumulator);

            while (_accumulator >= TickDelta)
            {
                _accumulator -= TickDelta;
                RunTick();
            }
        }

        public void RegisterSystem(ITickSystem system)
        {
            if (system == null || ContainsSystem(system))
                return;

            _systems.Add(new TickSystemEntry(system, _register_order++));
            SortSystems();
        }

        public void UnregisterSystem(ITickSystem system)
        {
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                if (_systems[i].System == system)
                    _systems.RemoveAt(i);
            }
        }

        private void RunTick()
        {
            CurrentTick++;
            GameTickContext context = new GameTickContext(this, CurrentTick, TickDelta);

            for (int i = 0; i < _systems.Count; i++)
            {
                ITickSystem system = _systems[i].System;
                if (system == null || !system.ShouldTick(context))
                    continue;

                system.Tick(context);
            }
        }

        private bool ContainsSystem(ITickSystem system)
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].System == system)
                    return true;
            }

            return false;
        }

        private void SortSystems()
        {
            _systems.Sort(CompareSystems);
        }

        private int CompareSystems(TickSystemEntry a, TickSystemEntry b)
        {
            int layer_compare = a.System.TickLayer.CompareTo(b.System.TickLayer);
            if (layer_compare != 0)
                return layer_compare;

            int order_compare = a.System.TickOrder.CompareTo(b.System.TickOrder);
            if (order_compare != 0)
                return order_compare;

            return a.RegisterOrder.CompareTo(b.RegisterOrder);
        }

        private readonly struct TickSystemEntry
        {
            public readonly ITickSystem System;
            public readonly int RegisterOrder;

            public TickSystemEntry(ITickSystem system, int register_order)
            {
                System = system;
                RegisterOrder = register_order;
            }
        }
    }
}
