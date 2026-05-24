using System;
using Game.TickSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Players.Input
{
    public class PlayerInputController : PlayerCharacterComponent
    {
        public override TickLayer TickLayer => TickLayer.InputCollect;

        public event Action<PlayerInputData> OnInputCaptured;

        private bool _was_shoot_pressed;
        private bool _was_jump_pressed;
        private InputAction _jump_action;
        private InputAction _sprint_action;
        private InputAction _crouch_action;

        public override bool ShouldTick(GameTickContext context)
        {
            return base.ShouldTick(context) && IsClient && IsOwned && IsAlive && IsGameplayActive;
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
            CacheInputActions(controls);

            bool is_shoot_pressed = controls.Player.Attack.IsPressed();
            bool is_jump_pressed = _jump_action.IsPressed();

            input.Move = controls.Player.Move.ReadValue<Vector2>();
            input.Look = controls.Player.Look.ReadValue<Vector2>();
            input.IsShootPressed = is_shoot_pressed && !_was_shoot_pressed;
            input.IsShootHeld = is_shoot_pressed;
            input.IsJumpPressed = is_jump_pressed && !_was_jump_pressed;
            input.IsJumpHeld = is_jump_pressed;
            input.IsSprintHeld = _sprint_action.IsPressed();
            input.IsCrouchHeld = _crouch_action.IsPressed();

            _was_shoot_pressed = is_shoot_pressed;
            _was_jump_pressed = is_jump_pressed;

            return input;
        }

        public override void ResetSimulation()
        {
            _was_shoot_pressed = false;
            _was_jump_pressed = false;
        }

        private void CacheInputActions(Controls controls)
        {
            if (_jump_action != null && _sprint_action != null && _crouch_action != null)
                return;

            _jump_action = controls.FindAction("Player/Jump", true);
            _sprint_action = controls.FindAction("Player/Sprint", true);
            _crouch_action = controls.FindAction("Player/Crouch", true);
        }
    }
}
