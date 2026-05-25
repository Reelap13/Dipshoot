using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class PlayerHitbox : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private Collider _collider;
        [SerializeField] private PlayerHitboxType _type = PlayerHitboxType.Body;
        [SerializeField] private float _damage_multiplier = 1f;
        [SerializeField] private bool _force_trigger = true;

        public PlayerHealth Health => GetHealth();
        public PlayerHitboxType Type => _type;
        public float DamageMultiplier => Mathf.Max(0f, _damage_multiplier);

        private void Awake()
        {
            CacheReferences();
            ConfigureCollider();
        }

        private void OnValidate()
        {
            CacheReferences();
            ConfigureCollider();
        }

        public bool TryApplyDamage(int base_damage, uint damage_source_net_id, out int applied_damage)
        {
            applied_damage = CalculateDamage(base_damage);
            PlayerHealth health = GetHealth();
            return health != null &&
                health.TryApplyDamage(applied_damage, damage_source_net_id, _type, DamageMultiplier);
        }

        public int CalculateDamage(int base_damage)
        {
            if (base_damage <= 0 || DamageMultiplier <= 0f)
                return 0;

            return Mathf.Max(1, Mathf.RoundToInt(base_damage * DamageMultiplier));
        }

        private void CacheReferences()
        {
            if (_health == null)
                _health = GetComponentInParent<PlayerHealth>();

            if (_collider == null)
                _collider = GetComponent<Collider>();
        }

        private PlayerHealth GetHealth()
        {
            if (_health == null)
                _health = GetComponentInParent<PlayerHealth>();

            return _health;
        }

        private void ConfigureCollider()
        {
            if (!_force_trigger || _collider == null)
                return;

            _collider.isTrigger = true;
        }
    }
}
