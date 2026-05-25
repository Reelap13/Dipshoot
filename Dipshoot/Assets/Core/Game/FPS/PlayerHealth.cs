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
        private bool[] _initial_renderer_enabled = Array.Empty<bool>();
        private bool[] _initial_collider_enabled = Array.Empty<bool>();

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
            _is_alive = true;
            ResetHealth();
            CachePresentationTargets();
            ApplyAlive(true);

            if (_character != null)
                _character.ResetSimulationState(position, rotation);

            Debug.Log($"{LogPrefix} Respawn. netId={netId} health={_current_health}/{MaxHealth}");
        }

        public void RefreshPresentationTargets()
        {
            CachePresentationTargets();
            ApplyAlive(_is_alive);
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
            Renderer[] previous_renderers = _renderers;
            Collider[] previous_colliders = _colliders;
            bool[] previous_renderer_enabled = _initial_renderer_enabled;
            bool[] previous_collider_enabled = _initial_collider_enabled;

            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            _initial_renderer_enabled = new bool[_renderers.Length];
            _initial_collider_enabled = new bool[_colliders.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                _initial_renderer_enabled[i] = TryGetPreviousEnabled(
                    _renderers[i],
                    previous_renderers,
                    previous_renderer_enabled,
                    out bool is_enabled)
                    ? is_enabled
                    : _renderers[i] != null && _renderers[i].enabled;
            }

            for (int i = 0; i < _colliders.Length; i++)
            {
                _initial_collider_enabled[i] = TryGetPreviousEnabled(
                    _colliders[i],
                    previous_colliders,
                    previous_collider_enabled,
                    out bool is_enabled)
                    ? is_enabled
                    : _colliders[i] != null && _colliders[i].enabled;
            }
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
            {
                if (_renderers[i] != null)
                    _renderers[i].enabled = is_alive && GetInitialRendererEnabled(i);
            }

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                    _colliders[i].enabled = is_alive && GetInitialColliderEnabled(i);
            }
        }

        private bool GetInitialRendererEnabled(int index)
        {
            return index >= 0 &&
                index < _initial_renderer_enabled.Length &&
                _initial_renderer_enabled[index];
        }

        private bool GetInitialColliderEnabled(int index)
        {
            return index >= 0 &&
                index < _initial_collider_enabled.Length &&
                _initial_collider_enabled[index];
        }

        private static bool TryGetPreviousEnabled<T>(
            T target,
            T[] previous_targets,
            bool[] previous_enabled,
            out bool is_enabled)
            where T : UnityEngine.Object
        {
            is_enabled = false;
            if (target == null)
                return false;

            for (int i = 0; i < previous_targets.Length && i < previous_enabled.Length; i++)
            {
                if (previous_targets[i] != target)
                    continue;

                is_enabled = previous_enabled[i];
                return true;
            }

            return false;
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
