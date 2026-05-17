using System;
using Game.TickSystem;
using UnityEngine;

namespace Game.Players.Input
{
    public class PlayerInputController : PlayerCharacterComponent
    {
        public override TickLayer TickLayer => TickLayer.InputCollect;

        public event Action<PlayerInputData> OnInputCaptured;

        public override bool ShouldTick(GameTickContext context)
        {
            return base.ShouldTick(context) && IsClient && IsOwned;
        }

        protected override void OnTick(GameTickContext context)
        {
            PlayerInputData input = GetInput();
            input.Tick = context.Tick;
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
