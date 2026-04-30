using System;
using UnityEngine;

namespace Game.Players.Input
{
    public class PlayerInputController : PlayerCharacterComponent
    {
        public event Action<PlayerInputData> OnInputCaptured;

        protected override void OnPreTick()
        {
            if (!IsOwned)
                return;

            PlayerInputData input = GetInput();
            input.Tick = TickManager.CurrentTick;
            Character.InputBuffet.Add(input);

            OnInputCaptured?.Invoke(input);
        }

        public PlayerInputData GetInput()
        {
            PlayerInputData input = new();

            var controls = InputManager.Instance.Controls;

            input.Move = controls.Player.Move.ReadValue<Vector2>();
            input.Look = controls.Player.Look.ReadValue<Vector2>();
            input.IsShoot = controls.Player.Attack.IsPressed();

            return input;
        }
    }
}
