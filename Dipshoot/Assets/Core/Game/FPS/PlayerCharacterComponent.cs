using Game.TickSystem;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Players
{
    public class PlayerCharacterComponent : MonoBehaviour
    {
        public PlayerCharacter Character { get; private set; }

        public TickManager TickManager => Character.TickManager;

        private bool _is_initialized;
        private bool _is_subscribed;

        public void Initialize(PlayerCharacter character)
        {
            Character = character;
            _is_initialized = true;

            TrySubscribe();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            TryUnsubscribe();
        }

        private void TrySubscribe()
        {
            if (!_is_initialized || _is_subscribed)
                return;

            Debug.Log($"{Character == null} {Character?.TickManager == null}");
            Character.TickManager.OnPreTick += OnPreTick;
            Character.TickManager.OnTick += OnTick;
            _is_subscribed = true;
        }

        private void TryUnsubscribe()
        {
            if (!_is_subscribed)
                return;

            Character.TickManager.OnPreTick -= OnPreTick;
            Character.TickManager.OnTick -= OnTick;
            _is_subscribed = false;
        }

        protected virtual void OnPreTick() { }
        protected virtual void OnTick() { }
    }
}