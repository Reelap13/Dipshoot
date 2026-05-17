using Game.TickSystem;
using UnityEngine;

namespace Game.Players
{
    public class PlayerCharacterComponent : MonoBehaviour, ITickSystem
    {
        public PlayerCharacter Character { get; private set; }

        public TickManager TickManager => Character == null ? null : Character.TickManager;

        public bool IsClient => Character.isClient;
        public bool IsServer => Character.isServer;
        public bool IsOwned => Character.isOwned;

        private bool _is_initialized;
        private TickManager _registered_tick_manager;

        public virtual TickLayer TickLayer => TickLayer.Movement;
        public virtual int TickOrder => 0;

        public void Initialize(PlayerCharacter character)
        {
            if (_registered_tick_manager != null && _registered_tick_manager != character.TickManager)
                TryUnregister();

            Character = character;
            _is_initialized = true;

            TryRegister();
        }

        private void OnEnable()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            TryUnregister();
        }

        public virtual bool ShouldTick(GameTickContext context)
        {
            return _is_initialized && Character != null && Character.TickManager == context.TickManager;
        }

        public void Tick(GameTickContext context)
        {
            OnTick(context);
        }

        private void TryRegister()
        {
            if (!_is_initialized || _registered_tick_manager != null || Character == null || Character.TickManager == null)
                return;

            _registered_tick_manager = Character.TickManager;
            _registered_tick_manager.RegisterSystem(this);
        }

        private void TryUnregister()
        {
            if (_registered_tick_manager == null)
                return;

            _registered_tick_manager.UnregisterSystem(this);
            _registered_tick_manager = null;
        }

        protected virtual void OnTick(GameTickContext context) { }
    }
}
