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
        public Collider HitboxCollider => GetCollider();
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

        private void OnDrawGizmosSelected()
        {
            CacheReferences();
            if (_collider == null)
                return;

            Gizmos.color = GetGizmoColor(_type);
            Matrix4x4 previous_matrix = Gizmos.matrix;
            Gizmos.matrix = _collider.transform.localToWorldMatrix;

            if (_collider is BoxCollider box)
                Gizmos.DrawWireCube(box.center, box.size);
            else if (_collider is SphereCollider sphere)
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            else if (_collider is CapsuleCollider capsule)
                DrawCapsuleApproximation(capsule);

            Gizmos.matrix = previous_matrix;
        }

        public bool TryApplyDamage(int base_damage, uint damage_source_net_id, out int applied_damage)
        {
            applied_damage = CalculateDamage(base_damage);
            PlayerHealth health = GetHealth();
            return health != null &&
                health.TryApplyDamage(applied_damage, damage_source_net_id, _type, DamageMultiplier);
        }

        public void Initialize(
            PlayerHealth health,
            Collider hitbox_collider,
            PlayerHitboxType type,
            float damage_multiplier)
        {
            _health = health;
            _collider = hitbox_collider;
            _type = type;
            _damage_multiplier = damage_multiplier;
            _force_trigger = true;
            ConfigureCollider();
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

        private Collider GetCollider()
        {
            if (_collider == null)
                _collider = GetComponent<Collider>();

            return _collider;
        }

        private void ConfigureCollider()
        {
            if (!_force_trigger || _collider == null)
                return;

            _collider.isTrigger = true;
        }

        private static Color GetGizmoColor(PlayerHitboxType type)
        {
            return type switch
            {
                PlayerHitboxType.Head => Color.red,
                PlayerHitboxType.Chest => Color.yellow,
                PlayerHitboxType.Pelvis => Color.magenta,
                PlayerHitboxType.Arm => Color.cyan,
                PlayerHitboxType.Leg => Color.green,
                _ => Color.white,
            };
        }

        private static void DrawCapsuleApproximation(CapsuleCollider capsule)
        {
            float radius = capsule.radius;
            float cylinder_height = Mathf.Max(0f, capsule.height - radius * 2f);
            Vector3 center = capsule.center;
            Vector3 half_axis = GetCapsuleAxis(capsule.direction) * (cylinder_height * 0.5f);

            Gizmos.DrawWireSphere(center + half_axis, radius);
            Gizmos.DrawWireSphere(center - half_axis, radius);
            Gizmos.DrawWireCube(center, GetCapsuleBoxSize(capsule.direction, radius, cylinder_height));
        }

        private static Vector3 GetCapsuleAxis(int direction)
        {
            return direction switch
            {
                0 => Vector3.right,
                2 => Vector3.forward,
                _ => Vector3.up,
            };
        }

        private static Vector3 GetCapsuleBoxSize(int direction, float radius, float cylinder_height)
        {
            float diameter = radius * 2f;
            return direction switch
            {
                0 => new Vector3(cylinder_height, diameter, diameter),
                2 => new Vector3(diameter, diameter, cylinder_height),
                _ => new Vector3(diameter, cylinder_height, diameter),
            };
        }
    }
}
