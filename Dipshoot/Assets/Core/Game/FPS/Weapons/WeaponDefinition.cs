using Scripts.Stats;
using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game/Weapons/WeaponDefinition")]
    public class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "weapon";
        [SerializeField] private string _display_name = "Weapon";
        [SerializeField] private WeaponSlot _slot = WeaponSlot.Primary;
        [SerializeField] private WeaponFireMode _fire_mode = WeaponFireMode.SemiAutomatic;
        [SerializeField] private Stat _damage_stat;
        [SerializeField] private Stat _range_stat;
        [SerializeField] private Stat _fire_interval_stat;
        [SerializeField] private Stat _magazine_size_stat;
        [SerializeField] private Stat _reserve_ammo_stat;
        [SerializeField] private Stat _reload_time_stat;
        [SerializeField] private Stat _spread_degrees_stat;
        [SerializeField] private float _tracer_lifetime = 0.12f;
        [SerializeField] private float _tracer_width = 0.03f;
        [SerializeField] private Color _tracer_color = Color.cyan;
        [SerializeField] private float _marker_lifetime = 0.6f;
        [SerializeField] private float _hit_marker_size = 0.18f;
        [SerializeField] private float _miss_marker_size = 0.1f;
        [SerializeField] private Color _miss_color = Color.yellow;
        [SerializeField] private Color _hit_color = Color.red;

        public string Id => _id;
        public string DisplayName => _display_name;
        public WeaponSlot Slot => _slot;
        public WeaponFireMode FireMode => _fire_mode;
        public Stat DamageStat => _damage_stat;
        public Stat RangeStat => _range_stat;
        public Stat FireIntervalStat => _fire_interval_stat;
        public Stat MagazineSizeStat => _magazine_size_stat;
        public Stat ReserveAmmoStat => _reserve_ammo_stat;
        public Stat ReloadTimeStat => _reload_time_stat;
        public Stat SpreadDegreesStat => _spread_degrees_stat;
        public float TracerLifetime => _tracer_lifetime;
        public float TracerWidth => _tracer_width;
        public Color TracerColor => _tracer_color;
        public float MarkerLifetime => _marker_lifetime;
        public float HitMarkerSize => _hit_marker_size;
        public float MissMarkerSize => _miss_marker_size;
        public Color MissColor => _miss_color;
        public Color HitColor => _hit_color;

        public WeaponStats GetStats(StatsController stats_controller)
        {
            return new WeaponStats(
                Mathf.Max(0, Mathf.RoundToInt(GetStat(stats_controller, _damage_stat))),
                Mathf.Max(0f, GetStat(stats_controller, _range_stat)),
                Mathf.Max(0f, GetStat(stats_controller, _fire_interval_stat)),
                Mathf.Max(0, Mathf.RoundToInt(GetStat(stats_controller, _magazine_size_stat))),
                Mathf.Max(0, Mathf.RoundToInt(GetStat(stats_controller, _reserve_ammo_stat))),
                Mathf.Max(0f, GetStat(stats_controller, _reload_time_stat)),
                Mathf.Max(0f, GetStat(stats_controller, _spread_degrees_stat)));
        }

        private static float GetStat(StatsController stats_controller, Stat stat)
        {
            return stats_controller == null
                ? 0f
                : stats_controller.GetStatValue(stat, 0f);
        }
    }
}
