namespace Game.Players
{
    public readonly struct DamageInfo
    {
        public readonly uint SourceNetId;
        public readonly int Damage;
        public readonly PlayerHitboxType HitboxType;
        public readonly float DamageMultiplier;
        public readonly WeaponSlot WeaponSlot;

        public DamageInfo(uint source_net_id, int damage)
            : this(source_net_id, damage, PlayerHitboxType.None, 1f, WeaponSlot.None)
        {
        }

        public DamageInfo(
            uint source_net_id,
            int damage,
            PlayerHitboxType hitbox_type,
            float damage_multiplier)
            : this(
                source_net_id,
                damage,
                hitbox_type,
                damage_multiplier,
                WeaponSlot.None)
        {
        }

        public DamageInfo(
            uint source_net_id,
            int damage,
            PlayerHitboxType hitbox_type,
            float damage_multiplier,
            WeaponSlot weapon_slot)
        {
            SourceNetId = source_net_id;
            Damage = damage;
            HitboxType = hitbox_type;
            DamageMultiplier = damage_multiplier;
            WeaponSlot = weapon_slot;
        }
    }
}
