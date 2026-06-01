using System;
using Game.Players;
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
        private bool _was_reload_pressed;
        private bool _was_primary_weapon_pressed;
        private bool _was_pistol_weapon_pressed;
        private bool _was_jump_pressed;
        private bool _is_look_action_subscribed;
        private Vector2 _mouse_look_delta;
        private Vector2 _analog_look;
        private InputAction _look_action;
        private InputAction _jump_action;
        private InputAction _sprint_action;
        private InputAction _crouch_action;
        private InputAction _reload_action;
        private InputAction _primary_weapon_action;
        private InputAction _pistol_weapon_action;
        private WeaponController _weapon_controller;

        public override bool ShouldTick(GameTickContext context)
        {
            return base.ShouldTick(context) && IsClient && IsOwned && IsAlive && IsGameplayActive;
        }

        protected override void OnTick(GameTickContext context)
        {
            PlayerInputData input = GetInput();
            input.Tick = context.Tick;
            if (_weapon_controller == null)
                _weapon_controller = GetComponent<WeaponController>();

            _weapon_controller?.PredictOwnerInput(ref input, context.Tick);
            Character.InputBuffet.Add(input);

            OnInputCaptured?.Invoke(input);
        }

        public PlayerInputData GetInput()
        {
            PlayerInputData input = new();

            var controls = InputManager.Instance.Controls;
            CacheInputActions(controls);

            bool is_shoot_pressed = controls.Player.Attack.IsPressed();
            bool is_reload_pressed = IsActionPressed(_reload_action);
            bool is_primary_weapon_pressed = IsActionPressed(_primary_weapon_action);
            bool is_pistol_weapon_pressed = IsActionPressed(_pistol_weapon_action);
            bool is_jump_pressed = _jump_action.IsPressed();

            input.Move = controls.Player.Move.ReadValue<Vector2>();
            input.Look = ConsumeLookInput();
            input.IsShootPressed = is_shoot_pressed && !_was_shoot_pressed;
            input.IsShootHeld = is_shoot_pressed;
            input.IsReloadPressed = is_reload_pressed && !_was_reload_pressed;
            input.RequestedWeaponSlot = ResolveRequestedWeaponSlot(
                is_primary_weapon_pressed && !_was_primary_weapon_pressed,
                is_pistol_weapon_pressed && !_was_pistol_weapon_pressed);
            input.IsJumpPressed = is_jump_pressed && !_was_jump_pressed;
            input.IsJumpHeld = is_jump_pressed;
            input.IsSprintHeld = _sprint_action.IsPressed();
            input.IsCrouchHeld = _crouch_action.IsPressed();

            _was_shoot_pressed = is_shoot_pressed;
            _was_reload_pressed = is_reload_pressed;
            _was_primary_weapon_pressed = is_primary_weapon_pressed;
            _was_pistol_weapon_pressed = is_pistol_weapon_pressed;
            _was_jump_pressed = is_jump_pressed;

            return input;
        }

        public override void ResetSimulation()
        {
            _was_shoot_pressed = false;
            _was_reload_pressed = false;
            _was_primary_weapon_pressed = false;
            _was_pistol_weapon_pressed = false;
            _was_jump_pressed = false;
            _mouse_look_delta = Vector2.zero;
            _analog_look = Vector2.zero;
        }

        private void OnDestroy()
        {
            UnsubscribeLookAction();
        }

        private void CacheInputActions(Controls controls)
        {
            if (_look_action != null &&
                _jump_action != null &&
                _sprint_action != null &&
                _crouch_action != null &&
                _reload_action != null &&
                _primary_weapon_action != null &&
                _pistol_weapon_action != null)
            {
                return;
            }

            _look_action = controls.FindAction("Player/Look", true);
            _jump_action = controls.FindAction("Player/Jump", true);
            _sprint_action = controls.FindAction("Player/Sprint", true);
            _crouch_action = controls.FindAction("Player/Crouch", true);
            _reload_action = controls.FindAction("Player/Reload", false);
            _primary_weapon_action = controls.FindAction("Player/PrimaryWeapon", false);
            _pistol_weapon_action = controls.FindAction("Player/PistolWeapon", false);
            SubscribeLookAction();
        }

        private Vector2 ConsumeLookInput()
        {
            Vector2 look = _mouse_look_delta + _analog_look;
            _mouse_look_delta = Vector2.zero;
            return look;
        }

        private void SubscribeLookAction()
        {
            if (_is_look_action_subscribed || _look_action == null)
                return;

            _look_action.performed += HandleLookPerformed;
            _look_action.canceled += HandleLookCanceled;
            _is_look_action_subscribed = true;
        }

        private void UnsubscribeLookAction()
        {
            if (!_is_look_action_subscribed || _look_action == null)
                return;

            _look_action.performed -= HandleLookPerformed;
            _look_action.canceled -= HandleLookCanceled;
            _is_look_action_subscribed = false;
        }

        private void HandleLookPerformed(InputAction.CallbackContext context)
        {
            if (context.control != null && context.control.device is Pointer)
            {
                _mouse_look_delta += context.ReadValue<Vector2>();
                return;
            }

            _analog_look = context.ReadValue<Vector2>();
        }

        private void HandleLookCanceled(InputAction.CallbackContext context)
        {
            if (context.control != null && context.control.device is Pointer)
                return;

            _analog_look = Vector2.zero;
        }

        private static bool IsActionPressed(InputAction action)
        {
            return action != null && action.IsPressed();
        }

        private static WeaponSlot ResolveRequestedWeaponSlot(
            bool is_primary_weapon_pressed,
            bool is_pistol_weapon_pressed)
        {
            if (is_primary_weapon_pressed)
                return WeaponSlot.Primary;

            return is_pistol_weapon_pressed
                ? WeaponSlot.Pistol
                : WeaponSlot.None;
        }
    }
}
