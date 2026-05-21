using System;
using Game.TickSystem;
using UnityEngine;

namespace Game.Players.Input
{
    public class PlayerInputController : PlayerCharacterComponent
    {
        public override TickLayer TickLayer => TickLayer.InputCollect;

        public event Action<PlayerInputData> OnInputCaptured;

        private bool _was_shoot_pressed;

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
            bool is_shoot_pressed = controls.Player.Attack.IsPressed();

            input.Move = controls.Player.Move.ReadValue<Vector2>();
            input.Look = controls.Player.Look.ReadValue<Vector2>();
            input.IsShootPressed = is_shoot_pressed && !_was_shoot_pressed;
            input.IsShootHeld = is_shoot_pressed;

            _was_shoot_pressed = is_shoot_pressed;

            return input;
        }
    }
}
