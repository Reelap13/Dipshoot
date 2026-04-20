using System;
using UnityEngine;

namespace Game.Players.Input
{
    public class PlayerInputController : PlayerCharacterComponent
    {
        [SerializeField] private PlayerInput _input;

        public Action OnBufferUpdated;

        protected override void OnPreTick()
        {
            InputData input = _input.GetInput();
            input.Tick = TickManager.CurrentTick;
            Character.InputBuffet.Add(input);

            OnBufferUpdated?.Invoke();
        }
    }
}