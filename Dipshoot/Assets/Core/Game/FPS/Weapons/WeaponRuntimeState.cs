namespace Game.Players
{
    public struct WeaponSlotState
    {
        public WeaponSlot Slot;
        public int AmmoInMagazine;
        public int ReserveAmmo;
        public int NextFireTick;
        public int ReloadStartTick;
        public int ReloadEndTick;
        public bool IsReloading;

        public WeaponSlotState(
            WeaponSlot slot,
            int ammo_in_magazine,
            int reserve_ammo,
            int tick)
        {
            Slot = slot;
            AmmoInMagazine = ammo_in_magazine;
            ReserveAmmo = reserve_ammo;
            NextFireTick = tick;
            ReloadStartTick = -1;
            ReloadEndTick = -1;
            IsReloading = false;
        }
    }

    public struct WeaponRuntimeState
    {
        public WeaponSlot ActiveSlot;
        public WeaponSlotState Primary;
        public WeaponSlotState Pistol;

        public WeaponSlotState GetSlotState(WeaponSlot slot)
        {
            return slot == WeaponSlot.Pistol
                ? Pistol
                : Primary;
        }

        public void SetSlotState(WeaponSlot slot, WeaponSlotState state)
        {
            if (slot == WeaponSlot.Pistol)
                Pistol = state;
            else
                Primary = state;
        }
    }
}
