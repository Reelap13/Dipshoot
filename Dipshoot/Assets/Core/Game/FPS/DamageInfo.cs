namespace Game.Players
{
    public readonly struct DamageInfo
    {
        public readonly uint SourceNetId;
        public readonly int Damage;

        public DamageInfo(uint source_net_id, int damage)
        {
            SourceNetId = source_net_id;
            Damage = damage;
        }
    }
}
