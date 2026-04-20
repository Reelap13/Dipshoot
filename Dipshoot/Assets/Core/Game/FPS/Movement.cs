using Game.TickSystem;
using UnityEngine;

namespace Game.Players
{
    public class Movement : PlayerCharacterComponent
    {
        [SerializeField] private float _speed = 5f;

        private Vector3 _velocity;

        protected override void OnTick()
        {
            Simulate();
        }

        void Simulate()
        {
            Debug.Log($"Try get data on tick {TickManager.CurrentTick}");
            if (!Character.InputBuffet.TryGet(TickManager.CurrentTick, out var input))
                return;

            Vector3 move = new Vector3(input.Move.x, 0, input.Move.y);
            _velocity = move * _speed;
            transform.position += _velocity * Character.TickManager.TickDelta;
        }
    }
}