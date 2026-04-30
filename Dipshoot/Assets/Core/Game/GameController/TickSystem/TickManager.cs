using System;
using UnityEngine;

namespace Game.TickSystem
{
    public class TickManager : MonoBehaviour
    {
        public int TickRate = 60;
        public float TickDelta => 1f / TickRate;
        public float TickProgress => TickDelta <= 0f ? 0f : _accumulator / TickDelta;

        public int CurrentTick { get; private set; } = 0;

        float _accumulator;

        public event Action OnPreTick;
        public event Action OnTick;
        public event Action OnPostTick;

        void Update()
        {
            _accumulator += Time.deltaTime;

            while (_accumulator >= TickDelta)
            {
                _accumulator -= TickDelta;
                Tick();
            }
        }

        void Tick()
        {
            CurrentTick++;
            OnPreTick?.Invoke();
            OnTick?.Invoke();
            OnPostTick?.Invoke();
        }
    }
}
