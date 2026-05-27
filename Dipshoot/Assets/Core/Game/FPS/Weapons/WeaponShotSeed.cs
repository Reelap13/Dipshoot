namespace Game.Players
{
    public static class WeaponShotSeed
    {
        public static int Get(uint shooter_net_id, int input_tick, WeaponSlot weapon_slot, int shot_sequence)
        {
            return unchecked(
                (int)shooter_net_id * 73856093 ^
                input_tick * 19349663 ^
                (int)weapon_slot * 83492791 ^
                shot_sequence * 265443576);
        }
    }
}
