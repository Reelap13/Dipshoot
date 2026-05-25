namespace Game.Players
{
    public readonly struct DamageInfo
    {
        public readonly uint SourceNetId;
        public readonly int Damage;
        public readonly PlayerHitboxType HitboxType;
        public readonly float DamageMultiplier;

        public DamageInfo(uint source_net_id, int damage)
            : this(source_net_id, damage, PlayerHitboxType.None, 1f)
        {
        }

        public DamageInfo(
            uint source_net_id,
            int damage,
            PlayerHitboxType hitbox_type,
            float damage_multiplier)
        {
            SourceNetId = source_net_id;
            Damage = damage;
            HitboxType = hitbox_type;
            DamageMultiplier = damage_multiplier;
        }
    }
}
