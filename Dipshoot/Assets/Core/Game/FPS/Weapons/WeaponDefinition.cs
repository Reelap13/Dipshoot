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
        [SerializeField] private Stat _spread_per_shot_stat;
        [SerializeField] private Stat _spread_recovery_stat;
        [SerializeField] private Stat _max_spread_stat;
        [SerializeField] private Stat _move_spread_stat;
        [SerializeField] private Stat _air_spread_stat;
        [SerializeField] private Stat _crouch_spread_multiplier_stat;
        [SerializeField] private Stat _recoil_pitch_stat;
        [SerializeField] private Stat _recoil_yaw_stat;
        [SerializeField] private Stat _recoil_recovery_stat;
        [SerializeField] private Stat _recoil_max_stat;
        [SerializeField] private WeaponVisualDefinition _visual;
        [SerializeField] private bool _show_debug_tracer = true;
        [SerializeField] private bool _show_debug_hit_marker;
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
        public Stat SpreadPerShotStat => _spread_per_shot_stat;
        public Stat SpreadRecoveryStat => _spread_recovery_stat;
        public Stat MaxSpreadStat => _max_spread_stat;
        public Stat MoveSpreadStat => _move_spread_stat;
        public Stat AirSpreadStat => _air_spread_stat;
        public Stat CrouchSpreadMultiplierStat => _crouch_spread_multiplier_stat;
        public Stat RecoilPitchStat => _recoil_pitch_stat;
        public Stat RecoilYawStat => _recoil_yaw_stat;
        public Stat RecoilRecoveryStat => _recoil_recovery_stat;
        public Stat RecoilMaxStat => _recoil_max_stat;
        public WeaponVisualDefinition Visual => _visual;
        public bool ShowDebugTracer => _show_debug_tracer;
        public bool ShowDebugHitMarker => _show_debug_hit_marker;
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
                Mathf.Max(0f, GetStat(stats_controller, _spread_degrees_stat)),
                Mathf.Max(0f, GetStat(stats_controller, _spread_per_shot_stat, DefaultSpreadPerShot)),
                Mathf.Max(0f, GetStat(stats_controller, _spread_recovery_stat, DefaultSpreadRecovery)),
                Mathf.Max(0f, GetStat(stats_controller, _max_spread_stat, DefaultMaxSpread)),
                Mathf.Max(0f, GetStat(stats_controller, _move_spread_stat, DefaultMoveSpread)),
                Mathf.Max(0f, GetStat(stats_controller, _air_spread_stat, DefaultAirSpread)),
                Mathf.Max(0f, GetStat(stats_controller, _crouch_spread_multiplier_stat, DefaultCrouchSpreadMultiplier)),
                Mathf.Max(0f, GetStat(stats_controller, _recoil_pitch_stat, DefaultRecoilPitch)),
                Mathf.Max(0f, GetStat(stats_controller, _recoil_yaw_stat, DefaultRecoilYaw)),
                Mathf.Max(0f, GetStat(stats_controller, _recoil_recovery_stat, DefaultRecoilRecovery)),
                Mathf.Max(0f, GetStat(stats_controller, _recoil_max_stat, DefaultRecoilMax)));
        }

        private static float GetStat(StatsController stats_controller, Stat stat)
        {
            return stats_controller == null
                ? 0f
                : stats_controller.GetStatValue(stat, 0f);
        }

        private static float GetStat(StatsController stats_controller, Stat stat, float fallback_value)
        {
            return stat == 0 || stats_controller == null
                ? fallback_value
                : stats_controller.GetStatValue(stat, fallback_value);
        }

        private float DefaultSpreadPerShot => _slot == WeaponSlot.Pistol ? 0.18f : 0.25f;
        private float DefaultSpreadRecovery => _slot == WeaponSlot.Pistol ? 7f : 5f;
        private float DefaultMaxSpread => _slot == WeaponSlot.Pistol ? 3f : 5f;
        private float DefaultMoveSpread => _slot == WeaponSlot.Pistol ? 0.35f : 0.65f;
        private float DefaultAirSpread => _slot == WeaponSlot.Pistol ? 1.2f : 1.8f;
        private float DefaultCrouchSpreadMultiplier => 0.65f;
        private float DefaultRecoilPitch => _slot == WeaponSlot.Pistol ? 0.75f : 0.45f;
        private float DefaultRecoilYaw => _slot == WeaponSlot.Pistol ? 0.28f : 0.22f;
        private float DefaultRecoilRecovery => _slot == WeaponSlot.Pistol ? 12f : 9f;
        private float DefaultRecoilMax => _slot == WeaponSlot.Pistol ? 8f : 12f;
    }
}
