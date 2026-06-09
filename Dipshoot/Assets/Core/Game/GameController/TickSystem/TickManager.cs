using System.Collections.Generic;
using UnityEngine;

namespace Game.TickSystem
{
    public enum TickLayer
    {
        InputCollect = 0,
        AimSimulation = 50,
        Movement = 75,
        WeaponInput = 100,
        WeaponSimulation = 110,
        InputSend = 150,
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
        [SerializeField] private float _min_tick_rate_scale = 0.95f;
        [SerializeField] private float _max_tick_rate_scale = 1.05f;

        public float TickDelta => TickRate <= 0 ? 0f : 1f / TickRate;
        public float TickProgress => GetScaledTickDelta() <= 0f ? 0f : _accumulator / GetScaledTickDelta();
        public float TickRateScale { get; set; } = 1f;
        public float CurrentTickRateScale => Mathf.Clamp(TickRateScale, _min_tick_rate_scale, _max_tick_rate_scale);

        public int CurrentTick { get; private set; } = 0;

        private float _accumulator;
        private int _register_order;
        private readonly List<TickSystemEntry> _systems = new();

        void Update()
        {
            if (TickRate <= 0)
                return;

            float scaled_tick_delta = GetScaledTickDelta();
            float max_accumulator = scaled_tick_delta * MaxTicksPerFrame;
            _accumulator = Mathf.Min(_accumulator + Time.deltaTime, max_accumulator);

            while (_accumulator >= scaled_tick_delta)
            {
                _accumulator -= scaled_tick_delta;
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

        public int SkipTicks(int ticks)
        {
            int skipped_ticks = Mathf.Max(0, ticks);
            if (skipped_ticks == 0)
                return 0;

            CurrentTick += skipped_ticks;
            _accumulator = 0f;
            return skipped_ticks;
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

        private float GetScaledTickDelta()
        {
            float tick_delta = TickDelta;
            if (tick_delta <= 0f)
                return 0f;

            return tick_delta / CurrentTickRateScale;
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
