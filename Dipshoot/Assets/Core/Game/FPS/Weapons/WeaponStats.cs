namespace Game.Players
{
    public readonly struct WeaponStats
    {
        public readonly int Damage;
        public readonly float Range;
        public readonly float FireInterval;
        public readonly int MagazineSize;
        public readonly int ReserveAmmo;
        public readonly float ReloadTime;
        public readonly float SpreadDegrees;
        public readonly float SpreadPerShot;
        public readonly float SpreadRecovery;
        public readonly float MaxSpread;
        public readonly float MoveSpread;
        public readonly float AirSpread;
        public readonly float MoveSpreadFullSpeed;
        public readonly float FallSpreadFullSpeed;
        public readonly float CrouchSpreadMultiplier;
        public readonly float RecoilPitch;
        public readonly float RecoilYaw;
        public readonly float RecoilRecovery;
        public readonly float RecoilMax;
        public readonly WeaponRecoilPatternDefinition RecoilPattern;

        public WeaponStats(
            int damage,
            float range,
            float fire_interval,
            int magazine_size,
            int reserve_ammo,
            float reload_time,
            float spread_degrees,
            float spread_per_shot,
            float spread_recovery,
            float max_spread,
            float move_spread,
            float air_spread,
            float move_spread_full_speed,
            float fall_spread_full_speed,
            float crouch_spread_multiplier,
            float recoil_pitch,
            float recoil_yaw,
            float recoil_recovery,
            float recoil_max,
            WeaponRecoilPatternDefinition recoil_pattern)
        {
            Damage = damage;
            Range = range;
            FireInterval = fire_interval;
            MagazineSize = magazine_size;
            ReserveAmmo = reserve_ammo;
            ReloadTime = reload_time;
            SpreadDegrees = spread_degrees;
            SpreadPerShot = spread_per_shot;
            SpreadRecovery = spread_recovery;
            MaxSpread = max_spread;
            MoveSpread = move_spread;
            AirSpread = air_spread;
            MoveSpreadFullSpeed = move_spread_full_speed;
            FallSpreadFullSpeed = fall_spread_full_speed;
            CrouchSpreadMultiplier = crouch_spread_multiplier;
            RecoilPitch = recoil_pitch;
            RecoilYaw = recoil_yaw;
            RecoilRecovery = recoil_recovery;
            RecoilMax = recoil_max;
            RecoilPattern = recoil_pattern;
        }

        public WeaponStats WithSpread(float spread_degrees)
        {
            return new WeaponStats(
                Damage,
                Range,
                FireInterval,
                MagazineSize,
                ReserveAmmo,
                ReloadTime,
                spread_degrees,
                SpreadPerShot,
                SpreadRecovery,
                MaxSpread,
                MoveSpread,
                AirSpread,
                MoveSpreadFullSpeed,
                FallSpreadFullSpeed,
                CrouchSpreadMultiplier,
                RecoilPitch,
                RecoilYaw,
                RecoilRecovery,
                RecoilMax,
                RecoilPattern);
        }
    }
}
