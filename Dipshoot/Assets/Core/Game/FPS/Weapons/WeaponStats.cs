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

        public WeaponStats(
            int damage,
            float range,
            float fire_interval,
            int magazine_size,
            int reserve_ammo,
            float reload_time,
            float spread_degrees)
        {
            Damage = damage;
            Range = range;
            FireInterval = fire_interval;
            MagazineSize = magazine_size;
            ReserveAmmo = reserve_ammo;
            ReloadTime = reload_time;
            SpreadDegrees = spread_degrees;
        }
    }
}
