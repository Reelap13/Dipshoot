using Game.Players;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Players.Input
{
    [DisallowMultipleComponent]
    public class ClientMoveInputTestOverride : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerInputController _input_controller;
        [SerializeField] private bool _is_active;
        [SerializeField] private float _switch_interval_seconds = 5f;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            if (_input_controller != null)
                _input_controller.SetInputOverrideProvider(ProvideInputOverride);
        }

        private void OnDisable()
        {
            if (_input_controller != null)
                _input_controller.ClearInputOverrideProvider(ProvideInputOverride);

            _is_active = false;
        }

        private void Update()
        {
            CacheReferences();
            if (_character == null || !_character.isClient || !_character.isOwned)
                return;

            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
                _is_active = !_is_active;
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_input_controller == null)
                _input_controller = GetComponent<PlayerInputController>();
        }

        private bool ProvideInputOverride(int tick, out PlayerInputData input)
        {
            input = default;
            if (!_is_active || _character == null || !_character.isClient || !_character.isOwned)
                return false;

            float interval = Mathf.Max(0.01f, _switch_interval_seconds);
            int phase = Mathf.FloorToInt(Time.unscaledTime / interval) % 2;
            input = new PlayerInputData
            {
                Move = phase == 0 ? Vector2.left : Vector2.right,
                Look = Vector2.zero,
                RequestedWeaponSlot = WeaponSlot.None
            };

            return true;
        }
    }
}
