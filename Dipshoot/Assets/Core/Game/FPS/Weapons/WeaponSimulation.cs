using Game.Players.Input;
using Scripts.Stats;
using UnityEngine;

namespace Game.Players
{
    public readonly struct WeaponSimulationResult
    {
        public readonly WeaponRuntimeState State;
        public readonly WeaponDefinition FiredWeapon;
        public readonly WeaponStats FiredWeaponStats;
        public readonly WeaponSlotState FiredSlotState;
        public readonly bool DidFire;

        public WeaponSimulationResult(
            WeaponRuntimeState state,
            WeaponDefinition fired_weapon,
            WeaponStats fired_weapon_stats,
            WeaponSlotState fired_slot_state,
            bool did_fire)
        {
            State = state;
            FiredWeapon = fired_weapon;
            FiredWeaponStats = fired_weapon_stats;
            FiredSlotState = fired_slot_state;
            DidFire = did_fire;
        }
    }

    public static class WeaponSimulation
    {
        public static WeaponRuntimeState CreateInitialState(
            WeaponDefinition primary_weapon,
            WeaponDefinition pistol_weapon,
            StatsController stats_controller,
            int tick)
        {
            return new WeaponRuntimeState
            {
                ActiveSlot = primary_weapon == null ? WeaponSlot.Pistol : WeaponSlot.Primary,
                Primary = CreateSlotState(primary_weapon, stats_controller, tick),
                Pistol = CreateSlotState(pistol_weapon, stats_controller, tick)
            };
        }

        public static WeaponSimulationResult Simulate(
            WeaponRuntimeState previous_state,
            PlayerInputData input,
            bool has_input,
            WeaponDefinition primary_weapon,
            WeaponDefinition pistol_weapon,
            StatsController stats_controller,
            int tick,
            int tick_rate)
        {
            WeaponRuntimeState state = previous_state;
            CompleteReloadIfReady(ref state.Primary, primary_weapon, stats_controller, tick);
            CompleteReloadIfReady(ref state.Pistol, pistol_weapon, stats_controller, tick);

            bool did_switch_slot = false;
            if (has_input)
            {
                WeaponSlot requested_slot = ResolveRequestedSlot(input);
                if (requested_slot != WeaponSlot.None &&
                    requested_slot != state.ActiveSlot &&
                    GetWeapon(requested_slot, primary_weapon, pistol_weapon) != null)
                {
                    state.ActiveSlot = requested_slot;
                    did_switch_slot = true;
                }
            }

            WeaponDefinition active_weapon = GetWeapon(state.ActiveSlot, primary_weapon, pistol_weapon);
            if (active_weapon == null)
                return new WeaponSimulationResult(state, null, default, default, false);

            WeaponStats active_stats = active_weapon.GetStats(stats_controller);
            WeaponSlotState active_slot_state = state.GetSlotState(state.ActiveSlot);

            if (has_input && input.IsReloadPressed)
                TryStartReload(ref active_slot_state, active_stats, tick, tick_rate);

            bool wants_fire = has_input && !did_switch_slot && WantsFire(input, active_weapon);
            bool did_fire = false;
            if (wants_fire)
            {
                if (active_slot_state.IsReloading)
                {
                    CompleteReloadIfReady(ref active_slot_state, active_weapon, stats_controller, tick);
                }

                if (!active_slot_state.IsReloading && active_slot_state.AmmoInMagazine <= 0)
                    TryStartReload(ref active_slot_state, active_stats, tick, tick_rate);

                if (!active_slot_state.IsReloading &&
                    active_slot_state.AmmoInMagazine > 0 &&
                    tick >= active_slot_state.NextFireTick)
                {
                    active_slot_state.AmmoInMagazine--;
                    active_slot_state.NextFireTick = tick + SecondsToTicks(active_stats.FireInterval, tick_rate);
                    did_fire = true;
                }
            }

            state.SetSlotState(state.ActiveSlot, active_slot_state);
            return new WeaponSimulationResult(
                state,
                did_fire ? active_weapon : null,
                did_fire ? active_stats : default,
                did_fire ? active_slot_state : default,
                did_fire);
        }

        private static WeaponSlotState CreateSlotState(
            WeaponDefinition weapon,
            StatsController stats_controller,
            int tick)
        {
            if (weapon == null)
                return new WeaponSlotState(WeaponSlot.None, 0, 0, tick);

            WeaponStats stats = weapon.GetStats(stats_controller);
            return new WeaponSlotState(
                weapon.Slot,
                stats.MagazineSize,
                stats.ReserveAmmo,
                tick);
        }

        private static WeaponSlot ResolveRequestedSlot(PlayerInputData input)
        {
            return input.RequestedWeaponSlot;
        }

        private static WeaponDefinition GetWeapon(
            WeaponSlot slot,
            WeaponDefinition primary_weapon,
            WeaponDefinition pistol_weapon)
        {
            return slot == WeaponSlot.Pistol
                ? pistol_weapon
                : primary_weapon;
        }

        private static bool WantsFire(PlayerInputData input, WeaponDefinition weapon)
        {
            return weapon.FireMode == WeaponFireMode.Automatic
                ? input.IsShootHeld
                : input.IsShootPressed;
        }

        private static void CompleteReloadIfReady(
            ref WeaponSlotState state,
            WeaponDefinition weapon,
            StatsController stats_controller,
            int tick)
        {
            if (!state.IsReloading || weapon == null || tick < state.ReloadEndTick)
                return;

            WeaponStats stats = weapon.GetStats(stats_controller);
            int missing_ammo = Mathf.Max(0, stats.MagazineSize - state.AmmoInMagazine);
            int loaded_ammo = Mathf.Min(missing_ammo, state.ReserveAmmo);
            state.AmmoInMagazine += loaded_ammo;
            state.ReserveAmmo -= loaded_ammo;
            state.IsReloading = false;
            state.ReloadStartTick = -1;
            state.ReloadEndTick = -1;
        }

        private static bool TryStartReload(
            ref WeaponSlotState state,
            WeaponStats stats,
            int tick,
            int tick_rate)
        {
            if (state.IsReloading ||
                state.ReserveAmmo <= 0 ||
                state.AmmoInMagazine >= stats.MagazineSize ||
                stats.MagazineSize <= 0)
            {
                return false;
            }

            state.IsReloading = true;
            state.ReloadStartTick = tick;
            state.ReloadEndTick = tick + SecondsToTicks(stats.ReloadTime, tick_rate);
            return true;
        }

        private static int SecondsToTicks(float seconds, int tick_rate)
        {
            if (seconds <= 0f || tick_rate <= 0)
                return 1;

            return Mathf.Max(1, Mathf.CeilToInt(seconds * tick_rate));
        }
    }
}
