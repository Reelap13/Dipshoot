using System;
using Mirror;
using Scripts.Stats;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class PlayerHealth : NetworkBehaviour
    {
        private const string LogPrefix = "[NetTick][Health]";

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private StatsController _stats;
        [SerializeField] private int _max_health = 100;

        [SyncVar] private int _current_health;
        [SyncVar(hook = nameof(HandleAliveChanged))] private bool _is_alive = true;

        private Renderer[] _renderers = Array.Empty<Renderer>();
        private Collider[] _colliders = Array.Empty<Collider>();

        public int MaxHealth => GetMaxHealth();
        public int CurrentHealth => _current_health;
        public bool IsAlive => _is_alive;

        public event Action<PlayerHealth, DamageInfo> OnDamageApplied;
        public event Action<PlayerHealth, DamageInfo> OnDied;

        private void Awake()
        {
            CacheReferences();
            CachePresentationTargets();
            ApplyAlive(true);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _is_alive = true;
            ResetHealth();
            ApplyAlive(true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyAlive(_is_alive);
        }

        public bool TryApplyDamage(int damage, uint damage_source_net_id)
        {
            return TryApplyDamage(damage, damage_source_net_id, PlayerHitboxType.None, 1f);
        }

        public bool TryApplyDamage(
            int damage,
            uint damage_source_net_id,
            PlayerHitboxType hitbox_type,
            float damage_multiplier)
        {
            if (!isServer || !_is_alive || damage <= 0)
                return false;

            DamageInfo damage_info = new(damage_source_net_id, damage, hitbox_type, damage_multiplier);
            _current_health = Mathf.Max(0, _current_health - damage);
            Debug.Log(
                $"{LogPrefix} Damage. netId={netId} source={damage_source_net_id} " +
                $"damage={damage} hitbox={hitbox_type} multiplier={damage_multiplier:0.##} " +
                $"health={_current_health}/{MaxHealth}");

            OnDamageApplied?.Invoke(this, damage_info);

            if (_current_health <= 0)
                Die(damage_info);

            return true;
        }

        public void Respawn(Transform spawn_point)
        {
            if (spawn_point == null)
                return;

            Respawn(spawn_point.position, spawn_point.rotation);
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            if (!isServer)
                return;

            CacheReferences();
            CachePresentationTargets();
            _is_alive = true;
            ResetHealth();
            ApplyAlive(true);

            if (_character != null)
                _character.ResetSimulationState(position, rotation);

            Debug.Log($"{LogPrefix} Respawn. netId={netId} health={_current_health}/{MaxHealth}");
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_stats == null)
                _stats = GetComponent<StatsController>();
        }

        private void CachePresentationTargets()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        private void ResetHealth()
        {
            _current_health = MaxHealth;
        }

        private int GetMaxHealth()
        {
            return _stats == null
                ? _max_health
                : Mathf.Max(1, Mathf.RoundToInt(_stats.GetStatValue(Stat.MAX_HEALTH, _max_health)));
        }

        private void HandleAliveChanged(bool old_value, bool new_value)
        {
            ApplyAlive(new_value);
        }

        private void ApplyAlive(bool is_alive)
        {
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].enabled = is_alive;

            for (int i = 0; i < _colliders.Length; i++)
                _colliders[i].enabled = is_alive;
        }

        private void Die(DamageInfo damage_info)
        {
            if (!_is_alive)
                return;

            _is_alive = false;
            ApplyAlive(false);
            Debug.Log($"{LogPrefix} Died. netId={netId} source={damage_info.SourceNetId}");
            OnDied?.Invoke(this, damage_info);
        }
    }
}
