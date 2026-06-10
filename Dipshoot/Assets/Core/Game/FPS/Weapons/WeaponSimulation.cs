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
        public readonly float FiredSpreadDegrees;
        public readonly float RecoilPitch;
        public readonly float RecoilYaw;
        public readonly bool DidFire;

        public WeaponSimulationResult(
            WeaponRuntimeState state,
            WeaponDefinition fired_weapon,
            WeaponStats fired_weapon_stats,
            WeaponSlotState fired_slot_state,
            float fired_spread_degrees,
            float recoil_pitch,
            float recoil_yaw,
            bool did_fire)
        {
            State = state;
            FiredWeapon = fired_weapon;
            FiredWeaponStats = fired_weapon_stats;
            FiredSlotState = fired_slot_state;
            FiredSpreadDegrees = fired_spread_degrees;
            RecoilPitch = recoil_pitch;
            RecoilYaw = recoil_yaw;
            DidFire = did_fire;
        }
    }

    public static class WeaponSimulation
    {
        private const float SemiAutomaticFireBufferFraction = 0.15f;

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
            int tick_rate,
            PlayerState player_state)
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
                    state.Primary.BufferedFireTick = -1;
                    state.Pistol.BufferedFireTick = -1;
                    did_switch_slot = true;
                }
            }

            WeaponDefinition active_weapon = GetWeapon(state.ActiveSlot, primary_weapon, pistol_weapon);
            if (active_weapon == null)
                return new WeaponSimulationResult(state, null, default, default, 0f, 0f, 0f, false);

            WeaponStats active_stats = active_weapon.GetStats(stats_controller);
            WeaponSlotState active_slot_state = state.GetSlotState(state.ActiveSlot);
            RecoverSpread(ref active_slot_state, active_stats, tick_rate);

            if (has_input && input.IsReloadPressed)
                TryStartReload(ref active_slot_state, active_stats, tick, tick_rate);

            int fire_interval_ticks = SecondsToTicks(active_stats.FireInterval, tick_rate);
            if (has_input && !did_switch_slot)
                TryBufferFireIntent(ref active_slot_state, input, active_weapon, fire_interval_ticks, tick);

            bool wants_fire = has_input && !did_switch_slot && WantsFire(input, active_weapon) ||
                ShouldConsumeBufferedFire(active_slot_state, active_weapon, tick);
            bool did_fire = false;
            float fired_spread = active_slot_state.SpreadDegrees;
            float recoil_pitch = 0f;
            float recoil_yaw = 0f;
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
                    active_slot_state.NextFireTick = tick + fire_interval_ticks;
                    active_slot_state.BufferedFireTick = -1;
                    active_slot_state.ConsecutiveShots = IsSprayReset(active_slot_state, active_stats, tick, tick_rate)
                        ? 1
                        : active_slot_state.ConsecutiveShots + 1;
                    active_slot_state.LastShotTick = tick;
                    fired_spread = GetEffectiveSpread(active_slot_state, active_stats, input, player_state);
                    IncreaseSpread(ref active_slot_state, active_stats);
                    GetRecoil(active_slot_state, active_stats, out recoil_pitch, out recoil_yaw);
                    did_fire = true;
                }
            }

            state.SetSlotState(state.ActiveSlot, active_slot_state);
            return new WeaponSimulationResult(
                state,
                did_fire ? active_weapon : null,
                did_fire ? active_stats : default,
                did_fire ? active_slot_state : default,
                fired_spread,
                recoil_pitch,
                recoil_yaw,
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

        private static void TryBufferFireIntent(
            ref WeaponSlotState state,
            PlayerInputData input,
            WeaponDefinition weapon,
            int fire_interval_ticks,
            int tick)
        {
            if (weapon.FireMode == WeaponFireMode.Automatic || !input.IsShootPressed || tick >= state.NextFireTick)
                return;

            int buffer_ticks = Mathf.Max(1, Mathf.CeilToInt(fire_interval_ticks * SemiAutomaticFireBufferFraction));
            if (state.NextFireTick - tick <= buffer_ticks)
                state.BufferedFireTick = tick;
        }

        private static bool ShouldConsumeBufferedFire(
            WeaponSlotState state,
            WeaponDefinition weapon,
            int tick)
        {
            return weapon.FireMode != WeaponFireMode.Automatic &&
                state.BufferedFireTick >= 0 &&
                tick >= state.NextFireTick;
        }

        public static float GetEffectiveSpread(
            WeaponSlotState state,
            WeaponStats stats,
            PlayerInputData input,
            PlayerState player_state)
        {
            float spread = stats.SpreadDegrees + state.SpreadDegrees;
            Vector3 horizontal_velocity = new(player_state.Velocity.x, 0f, player_state.Velocity.z);
            spread += stats.MoveSpread * Mathf.Clamp01(horizontal_velocity.magnitude / stats.MoveSpreadFullSpeed);

            if (!player_state.IsGrounded)
                spread += stats.AirSpread * Mathf.Clamp01(Mathf.Abs(player_state.Velocity.y) / stats.FallSpreadFullSpeed);

            if (player_state.Stance == MovementStance.Crouching)
                spread *= stats.CrouchSpreadMultiplier;

            return Mathf.Min(stats.MaxSpread, spread);
        }

        private static void IncreaseSpread(ref WeaponSlotState state, WeaponStats stats)
        {
            state.SpreadDegrees = Mathf.Min(
                stats.MaxSpread,
                state.SpreadDegrees + stats.SpreadPerShot);
        }

        private static void RecoverSpread(
            ref WeaponSlotState state,
            WeaponStats stats,
            int tick_rate)
        {
            if (tick_rate <= 0)
                return;

            state.SpreadDegrees = Mathf.MoveTowards(
                state.SpreadDegrees,
                0f,
                stats.SpreadRecovery / tick_rate);
        }

        private static bool IsSprayReset(
            WeaponSlotState state,
            WeaponStats stats,
            int tick,
            int tick_rate)
        {
            if (state.LastShotTick < 0 || tick_rate <= 0)
                return true;

            float reset_time = stats.RecoilPattern == null
                ? stats.FireInterval * 2.5f
                : stats.RecoilPattern.PatternResetTime;
            return tick - state.LastShotTick > SecondsToTicks(reset_time, tick_rate);
        }

        private static void GetRecoil(
            WeaponSlotState state,
            WeaponStats stats,
            out float pitch,
            out float yaw)
        {
            if (TryGetPatternRecoil(state, stats, out pitch, out yaw))
                return;

            float shot_index = Mathf.Max(1, state.ConsecutiveShots);
            pitch = Mathf.Min(stats.RecoilMax, stats.RecoilPitch * Mathf.Sqrt(shot_index));
            yaw = stats.RecoilYaw *
                Mathf.Min(1f, shot_index / 5f) *
                GetRecoilYawPattern(state.ConsecutiveShots);
        }

        private static bool TryGetPatternRecoil(
            WeaponSlotState state,
            WeaponStats stats,
            out float pitch,
            out float yaw)
        {
            pitch = 0f;
            yaw = 0f;
            WeaponRecoilPatternDefinition recoil_pattern = stats.RecoilPattern;
            Vector2[] pattern = recoil_pattern == null ? null : recoil_pattern.Pattern;
            if (pattern == null || pattern.Length == 0)
                return false;

            int index = Mathf.Clamp(state.ConsecutiveShots - 1, 0, pattern.Length - 1);
            Vector2 recoil = pattern[index];
            int seed = state.LastShotTick * 397 ^ state.ConsecutiveShots * 7919;
            yaw = (recoil.x + LerpHash(seed, -recoil_pattern.RandomYaw, recoil_pattern.RandomYaw)) *
                recoil_pattern.GameplayScale;
            pitch = Mathf.Max(0f, recoil.y + LerpHash(seed + 1, -recoil_pattern.RandomPitch, recoil_pattern.RandomPitch)) *
                recoil_pattern.GameplayScale;
            return true;
        }

        private static float GetRecoilYawPattern(int shot_index)
        {
            return ((shot_index - 1) % 8) switch
            {
                0 => 0.35f,
                1 => 0.65f,
                2 => 0.45f,
                3 => -0.25f,
                4 => -0.55f,
                5 => -0.35f,
                6 => 0.15f,
                _ => 0.4f
            };
        }

        private static float LerpHash(int seed, float min, float max)
        {
            return Mathf.Lerp(min, max, Hash01(seed));
        }

        private static float Hash01(int seed)
        {
            uint value = unchecked((uint)seed);
            value ^= value >> 16;
            value *= 0x7feb352d;
            value ^= value >> 15;
            value *= 0x846ca68b;
            value ^= value >> 16;
            return (value & 0x00ffffff) / 16777215f;
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
            state.BufferedFireTick = -1;
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
