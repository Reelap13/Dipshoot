using System;
using UnityEngine;

namespace Game.TickSystem
{
    public class TickManager : MonoBehaviour
    {
        public int TickRate = 60;
        public float TickDelta => 1f / TickRate;

        public int CurrentTick { get; private set; }

        float _accumulator;

        public event Action OnPreTick;
        public event Action OnTick;

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
        }
    }
}