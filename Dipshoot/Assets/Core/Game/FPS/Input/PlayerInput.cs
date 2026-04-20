using Game.TickSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

namespace Game.Players.Input
{
    public class PlayerInput : MonoBehaviour
    {
        public InputData GetInput()
        {
            InputData input = new();

            var controls = InputManager.Instance.Controls;

            input.Move = controls.Player.Move.ReadValue<Vector2>();
            input.Look = controls.Player.Look.ReadValue<Vector2>();
            input.IsShoot = controls.Player.Attack.IsPressed();

            return input;
        }
    }
}