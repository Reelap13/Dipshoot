using Game.Players.Input;
using Game.TickSystem;
using Mirror;
using Scripts.Stats;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class WeaponController : NetworkBehaviour, ITickSystem, IPlayerSimulationResettable
    {
        private const string LogPrefix = "[NetTick][Weapon]";
        private const string ShotCompareLocalPrefix = "[ShotCompare][Local]";
        private const string ShotCompareServerPrefix = "[ShotCompare][Server]";
        private const int MaxShotHits = 32;
        private const float PredictedShotPointWarningThreshold = 0.35f;
        private const float PredictedShotAngleWarningThreshold = 0.5f;
        private const int PredictedShotTimeoutTicks = 96;

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private StatsController _stats;
        [SerializeField] private WeaponDefinition _primary_weapon;
        [SerializeField] private WeaponDefinition _pistol_weapon;
        [SerializeField] private Vector3 _eye_offset = new(0f, 0.49f, 0.359f);
        [SerializeField] private LayerMask _hit_mask = ~0;
        [SerializeField] private QueryTriggerInteraction _trigger_interaction = QueryTriggerInteraction.Collide;
        [SerializeField] private float _lag_compensation_visual_back_ms = 33.3f;
        [SerializeField] private bool _shot_compare_debug_enabled = true;
        [SerializeField] private int _shot_compare_debug_max_logs_per_second = 20;

        [SyncVar] private WeaponSlot _active_slot = WeaponSlot.Primary;
        [SyncVar] private int _primary_ammo;
        [SyncVar] private int _primary_reserve_ammo;
        [SyncVar] private int _pistol_ammo;
        [SyncVar] private int _pistol_reserve_ammo;
        [SyncVar] private bool _primary_is_reloading;
        [SyncVar] private int _primary_reload_start_tick = -1;
        [SyncVar] private int _primary_reload_end_tick = -1;
        [SyncVar] private bool _pistol_is_reloading;
        [SyncVar] private int _pistol_reload_start_tick = -1;
        [SyncVar] private int _pistol_reload_end_tick = -1;

        private readonly RaycastHit[] _hits = new RaycastHit[MaxShotHits];
        private TickManager _registered_tick_manager;
        private WeaponRuntimeState _weapon_state;
        private bool _has_weapon_state;
        private int _last_processed_input_tick = -1;
        private WeaponRuntimeState _predicted_weapon_state;
        private bool _has_predicted_weapon_state;
        private int _predicted_shot_sequence;
        private float _shot_compare_debug_window_started_at;
        private int _shot_compare_debug_logged_in_window;
        private int _shot_compare_debug_suppressed;
        private readonly Dictionary<PredictedShotKey, PredictedShot> _predicted_shots = new();

        public TickLayer TickLayer => TickLayer.WeaponSimulation;
        public int TickOrder => 0;
        public WeaponSlot ActiveSlot => _active_slot;
        public int PrimaryAmmo => _primary_ammo;
        public int PrimaryReserveAmmo => _primary_reserve_ammo;
        public int PistolAmmo => _pistol_ammo;
        public int PistolReserveAmmo => _pistol_reserve_ammo;
        public int ActiveAmmo => TryGetPredictedActiveSlotState(out WeaponSlotState state) ? state.AmmoInMagazine : _active_slot == WeaponSlot.Pistol ? _pistol_ammo : _primary_ammo;
        public int ActiveReserveAmmo => TryGetPredictedActiveSlotState(out WeaponSlotState state) ? state.ReserveAmmo : _active_slot == WeaponSlot.Pistol ? _pistol_reserve_ammo : _primary_reserve_ammo;
        public bool IsActiveReloading => GetIsReloading(_active_slot);
        public float ActiveReloadProgress => GetReloadProgress(_active_slot);
        public string ActiveWeaponDisplayName => GetWeaponDefinition(_active_slot)?.DisplayName ?? _active_slot.ToString();
        public float ActiveRecoilRecovery => GetWeaponDefinition(_active_slot)?.GetStats(_stats).RecoilRecovery ?? 0f;
        public float ActiveRecoilMax => GetWeaponDefinition(_active_slot)?.GetStats(_stats).RecoilMax ?? 0f;
        public float CurrentEffectiveSpreadDegrees { get; private set; }
        public WeaponDefinition PrimaryWeaponDefinition => _primary_weapon;
        public WeaponDefinition PistolWeaponDefinition => _pistol_weapon;

        private void Awake()
        {
            CacheReferences();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            InitializeWeaponState(0);
        }

        private void Update()
        {
            CacheReferences();
            TryRegisterTickSystem();
            CleanupPredictedShots();
        }

        private void OnDisable()
        {
            TryUnregisterTickSystem();
        }

        public bool ShouldTick(GameTickContext context)
        {
            return _character != null &&
                (_character.Health == null || _character.Health.IsAlive) &&
                _character.IsGameplayActive &&
                _character.TickManager == context.TickManager &&
                (isServer || isClient && isOwned && !isServer);
        }

        public void Tick(GameTickContext context)
        {
            if (isServer)
            {
                TryProcessWeaponTick(context.Tick);
                return;
            }

            TryPredictOwnerTick(context.Tick);
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_stats == null)
                _stats = GetComponent<StatsController>();
        }

        private void TryRegisterTickSystem()
        {
            if (_registered_tick_manager != null || _character == null || _character.TickManager == null)
                return;

            _registered_tick_manager = _character.TickManager;
            _registered_tick_manager.RegisterSystem(this);
        }

        private void TryUnregisterTickSystem()
        {
            if (_registered_tick_manager == null)
                return;

            _registered_tick_manager.UnregisterSystem(this);
            _registered_tick_manager = null;
        }

        private void TryProcessWeaponTick(int server_tick)
        {
            EnsureWeaponState(server_tick);

            bool processed_input = false;
            while (_character.InputBuffet.TryGetFirstAfter(_last_processed_input_tick, out PlayerInputData input) &&
                   input.Tick <= server_tick)
            {
                _last_processed_input_tick = input.Tick;
                processed_input = true;
                ProcessWeaponInput(input, server_tick);
            }

            if (!processed_input)
                ProcessWeaponInput(default, server_tick, false);
        }

        private void ProcessWeaponInput(PlayerInputData input, int server_tick, bool has_input = true)
        {
            int simulation_tick = has_input ? input.Tick : server_tick;
            bool has_simulation_state = TryGetPlayerState(simulation_tick, out PlayerState simulation_state);
            WeaponSimulationResult simulation_result = WeaponSimulation.Simulate(
                _weapon_state,
                input,
                has_input,
                _primary_weapon,
                _pistol_weapon,
                _stats,
                simulation_tick,
                _character.TickManager == null ? 0 : _character.TickManager.TickRate,
                simulation_state);

            _weapon_state = simulation_result.State;
            SyncWeaponState();
            if (isOwned)
                UpdateCurrentSpread(input, simulation_state);

            if (!simulation_result.DidFire)
                return;

            if (!has_simulation_state)
            {
                return;
            }

            int lag_compensation_visual_back_ticks = GetLagCompensationVisualBackTicks();
            ShotResult shot_result = WeaponShotResolver.Resolve(
                _character,
                simulation_result.FiredWeapon,
                simulation_result.FiredWeaponStats.WithSpread(simulation_result.FiredSpreadDegrees),
                simulation_state,
                input,
                simulation_result.FiredSlotState,
                server_tick,
                lag_compensation_visual_back_ticks,
                _eye_offset,
                _hit_mask,
                _trigger_interaction,
                _hits);

            LogShotCompare(
                ShotCompareServerPrefix,
                shot_result,
                simulation_state,
                simulation_result.FiredSpreadDegrees,
                simulation_result.FiredSlotState.ConsecutiveShots,
                WeaponShotResolver.GetSprayPatternOffset(
                    simulation_result.FiredWeaponStats,
                    simulation_result.FiredSlotState.ConsecutiveShots),
                lag_compensation_visual_back_ticks);

            if (isOwned)
                ApplyViewRecoil(
                    simulation_result.RecoilPitch,
                    simulation_result.RecoilYaw,
                    simulation_result.FiredWeaponStats);

            RpcRegisterShot(shot_result);
        }

        private void TryPredictOwnerTick(int tick)
        {
            if (!isClient || !isOwned || isServer || _character == null || _character.TickManager == null)
                return;

            if (!_character.InputBuffet.TryGet(tick, out PlayerInputData input))
                return;

            PredictOwnerInput(ref input, tick);
            _character.InputBuffet.Add(input);
        }

        private void PredictOwnerInput(ref PlayerInputData input, int tick)
        {
            if (!isClient || !isOwned || isServer || _character == null || _character.TickManager == null)
                return;

            EnsurePredictedWeaponState(tick);
            if (WillSwitchPredictedSlot(input))
                WeaponPresentation.ClearTransientShotVfx(this);

            input.ShotSequence = _predicted_shot_sequence + 1;

            bool has_simulation_state = TryGetPlayerState(tick, out PlayerState simulation_state);
            WeaponSimulationResult simulation_result = WeaponSimulation.Simulate(
                _predicted_weapon_state,
                input,
                true,
                _primary_weapon,
                _pistol_weapon,
                _stats,
                tick,
                _character.TickManager.TickRate,
                simulation_state);

            _predicted_weapon_state = simulation_result.State;
            UpdateCurrentSpread(input, simulation_state);
            if (!simulation_result.DidFire)
            {
                input.ShotSequence = 0;
                return;
            }

            _predicted_shot_sequence = input.ShotSequence;
            if (!has_simulation_state)
            {
                return;
            }

            ShotResult result = WeaponShotResolver.ResolvePredicted(
                _character,
                simulation_result.FiredWeapon,
                simulation_result.FiredWeaponStats.WithSpread(simulation_result.FiredSpreadDegrees),
                simulation_state,
                input,
                simulation_result.FiredSlotState,
                _eye_offset,
                _hit_mask,
                _trigger_interaction,
                _hits);

            _predicted_shots[new PredictedShotKey(result.WeaponSlot, result.ShotSequence)] =
                new PredictedShot(result, tick);
            LogShotCompare(
                ShotCompareLocalPrefix,
                result,
                simulation_state,
                simulation_result.FiredSpreadDegrees,
                simulation_result.FiredSlotState.ConsecutiveShots,
                WeaponShotResolver.GetSprayPatternOffset(
                    simulation_result.FiredWeaponStats,
                    simulation_result.FiredSlotState.ConsecutiveShots),
                0);
            ApplyViewRecoil(
                simulation_result.RecoilPitch,
                simulation_result.RecoilYaw,
                simulation_result.FiredWeaponStats);
            WeaponPresentation.PlayPredictedShot(this, result, simulation_result.FiredWeapon);
        }

        private void EnsureWeaponState(int tick)
        {
            if (_has_weapon_state)
                return;

            InitializeWeaponState(tick);
        }

        private void InitializeWeaponState(int tick)
        {
            CacheReferences();
            _weapon_state = WeaponSimulation.CreateInitialState(
                _primary_weapon,
                _pistol_weapon,
                _stats,
                tick);
            _has_weapon_state = true;
            SyncWeaponState();
        }

        private void SyncWeaponState()
        {
            _active_slot = _weapon_state.ActiveSlot;
            _primary_ammo = _weapon_state.Primary.AmmoInMagazine;
            _primary_reserve_ammo = _weapon_state.Primary.ReserveAmmo;
            _pistol_ammo = _weapon_state.Pistol.AmmoInMagazine;
            _pistol_reserve_ammo = _weapon_state.Pistol.ReserveAmmo;
            _primary_is_reloading = _weapon_state.Primary.IsReloading;
            _primary_reload_start_tick = _weapon_state.Primary.ReloadStartTick;
            _primary_reload_end_tick = _weapon_state.Primary.ReloadEndTick;
            _pistol_is_reloading = _weapon_state.Pistol.IsReloading;
            _pistol_reload_start_tick = _weapon_state.Pistol.ReloadStartTick;
            _pistol_reload_end_tick = _weapon_state.Pistol.ReloadEndTick;
        }

        public WeaponDefinition GetWeaponDefinition(WeaponSlot slot)
        {
            return slot == WeaponSlot.Pistol
                ? _pistol_weapon
                : _primary_weapon;
        }

        private bool GetIsReloading(WeaponSlot slot)
        {
            return slot == WeaponSlot.Pistol
                ? _pistol_is_reloading
                : _primary_is_reloading;
        }

        private float GetReloadProgress(WeaponSlot slot)
        {
            if (!GetIsReloading(slot))
                return 1f;

            int start_tick = slot == WeaponSlot.Pistol
                ? _pistol_reload_start_tick
                : _primary_reload_start_tick;
            int end_tick = slot == WeaponSlot.Pistol
                ? _pistol_reload_end_tick
                : _primary_reload_end_tick;
            int current_tick = _character == null || _character.TickManager == null
                ? start_tick
                : _character.TickManager.CurrentTick;

            return Mathf.Clamp01((current_tick - start_tick) / (float)Mathf.Max(1, end_tick - start_tick));
        }

        [ClientRpc]
        private void RpcRegisterShot(ShotResult result)
        {
            if (isOwned && TryConsumePredictedShot(result, out ShotResult predicted_result))
            {
                WarnIfPredictedShotMismatch(predicted_result, result);
                WeaponPresentation.PlayConfirmedOwnerShot(this, result, GetWeaponDefinition(result.WeaponSlot));
                WeaponPresentation.PlayOwnerHitFeedback(result);
                return;
            }

            if (isOwned && isServer)
                WeaponPresentation.PlayOwnerHitFeedback(result);

            WeaponPresentation.PlayRemoteShot(this, result, GetWeaponDefinition(result.WeaponSlot));
        }

        public void ResetSimulation()
        {
            _last_processed_input_tick = -1;
            _has_weapon_state = false;

            if (isServer)
                InitializeWeaponState(_character == null || _character.TickManager == null
                    ? 0
                    : _character.TickManager.CurrentTick);

            _has_predicted_weapon_state = false;
            _predicted_shot_sequence = 0;
            _predicted_shots.Clear();
        }

        private void EnsurePredictedWeaponState(int tick)
        {
            if (_has_predicted_weapon_state)
                return;

            ResetPredictedStateFromSync();
            _has_predicted_weapon_state = true;
        }

        private void ResetPredictedStateFromSync()
        {
            _predicted_weapon_state = new WeaponRuntimeState
            {
                ActiveSlot = _active_slot,
                Primary = new WeaponSlotState(WeaponSlot.Primary, _primary_ammo, _primary_reserve_ammo, GetCurrentTick()),
                Pistol = new WeaponSlotState(WeaponSlot.Pistol, _pistol_ammo, _pistol_reserve_ammo, GetCurrentTick()),
            };
            _predicted_weapon_state.Primary.IsReloading = _primary_is_reloading;
            _predicted_weapon_state.Primary.ReloadStartTick = _primary_reload_start_tick;
            _predicted_weapon_state.Primary.ReloadEndTick = _primary_reload_end_tick;
            _predicted_weapon_state.Pistol.IsReloading = _pistol_is_reloading;
            _predicted_weapon_state.Pistol.ReloadStartTick = _pistol_reload_start_tick;
            _predicted_weapon_state.Pistol.ReloadEndTick = _pistol_reload_end_tick;
            _has_predicted_weapon_state = true;
        }

        private bool TryGetPredictedActiveSlotState(out WeaponSlotState state)
        {
            state = default;
            if (!isClient || !isOwned || !_has_predicted_weapon_state)
                return false;

            state = _predicted_weapon_state.GetSlotState(_predicted_weapon_state.ActiveSlot);
            return true;
        }

        private bool WillSwitchPredictedSlot(PlayerInputData input)
        {
            return input.RequestedWeaponSlot != WeaponSlot.None &&
                input.RequestedWeaponSlot != _predicted_weapon_state.ActiveSlot &&
                GetWeaponDefinition(input.RequestedWeaponSlot) != null;
        }

        private void ApplyRecoil(float pitch, float yaw)
        {
            if ((pitch <= 0f && Mathf.Abs(yaw) <= 0f) ||
                !TryGetComponent(out AimController aim_controller))
            {
                return;
            }

            aim_controller.ApplyRecoil(pitch, yaw);
        }

        private void ApplyViewRecoil(float pitch, float yaw, WeaponStats stats)
        {
            if (pitch <= 0f && Mathf.Abs(yaw) <= 0f)
                return;

            if (!TryGetComponent(out PlayerViewRecoilController view_recoil))
                view_recoil = gameObject.AddComponent<PlayerViewRecoilController>();

            float scale = stats.RecoilPattern == null ? 1f : stats.RecoilPattern.VisualScale;
            view_recoil.AddImpulse(pitch * scale, yaw * scale);
        }

        private bool TryGetPlayerState(int tick, out PlayerState state)
        {
            state = default;
            if (_character == null)
                return false;

            if (_character.StateBuffer.TryGet(tick, out state) ||
                _character.StateBuffer.TryGetLastAtOrBefore(tick, out state))
            {
                return true;
            }

            state.IsGrounded = true;
            return false;
        }

        private void UpdateCurrentSpread(PlayerInputData input, PlayerState player_state)
        {
            WeaponRuntimeState runtime_state = isClient && isOwned && _has_predicted_weapon_state
                ? _predicted_weapon_state
                : _weapon_state;
            WeaponDefinition weapon = GetWeaponDefinition(runtime_state.ActiveSlot);
            if (weapon == null)
            {
                CurrentEffectiveSpreadDegrees = 0f;
                return;
            }

            WeaponSlotState slot_state = runtime_state.GetSlotState(runtime_state.ActiveSlot);
            CurrentEffectiveSpreadDegrees = WeaponSimulation.GetEffectiveSpread(
                slot_state,
                weapon.GetStats(_stats),
                input,
                player_state);
        }

        private void LogShotCompare(
            string prefix,
            ShotResult result,
            PlayerState state,
            float spread_degrees,
            int shot_index,
            Vector2 pattern_offset,
            int lag_compensation_visual_back_ticks)
        {
            if (!ShouldLogShotCompare())
                return;

            int suppressed = _shot_compare_debug_suppressed;
            _shot_compare_debug_suppressed = 0;

            Debug.Log(
                $"{prefix} shotId={GetShotDebugId(result)} " +
                $"inputTick={result.InputTick} serverTick={result.ServerTick} " +
                $"serverMinusInput={result.ServerTick - result.InputTick} " +
                $"stateTick={state.Tick} hitboxQueryTick={result.HitboxQueryTick} " +
                $"hitboxSnapshotTick={result.HitboxSnapshotTick} visualBackTicks={lag_compensation_visual_back_ticks} " +
                $"pos={FormatVector(state.Position)} origin={FormatVector(result.Origin)} " +
                $"dir={FormatVector(result.Direction)} point={FormatVector(result.Point)} " +
                $"rotY={FormatFloat(state.Rotation.eulerAngles.y)} pitch={FormatFloat(state.CameraPitch)} " +
                $"spread={FormatFloat(spread_degrees)} shotIndex={shot_index} " +
                $"patternOffset={FormatVector2(pattern_offset)} seed={result.SpreadSeed} " +
                $"hit={result.HasHit} damage={result.DidDamage} target={result.HitNetId} " +
                $"hitbox={result.HitboxType} suppressed={suppressed}");
        }

        private bool ShouldLogShotCompare()
        {
            if (!_shot_compare_debug_enabled)
                return false;

            int max_logs = Mathf.Max(1, _shot_compare_debug_max_logs_per_second);
            if (Time.unscaledTime - _shot_compare_debug_window_started_at >= 1f)
            {
                _shot_compare_debug_window_started_at = Time.unscaledTime;
                _shot_compare_debug_logged_in_window = 0;
            }

            if (_shot_compare_debug_logged_in_window >= max_logs)
            {
                _shot_compare_debug_suppressed++;
                return false;
            }

            _shot_compare_debug_logged_in_window++;
            return true;
        }

        private int GetLagCompensationVisualBackTicks()
        {
            if (_character == null || _character.TickManager == null || _lag_compensation_visual_back_ms <= 0f)
                return 0;

            return Mathf.Max(
                0,
                Mathf.RoundToInt(_lag_compensation_visual_back_ms * 0.001f * _character.TickManager.TickRate));
        }

        private static string GetShotDebugId(ShotResult result)
        {
            return $"{result.ShooterNetId}:{result.WeaponSlot}:{result.ShotSequence}";
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({FormatFloat(value.x)},{FormatFloat(value.y)},{FormatFloat(value.z)})";
        }

        private static string FormatVector2(Vector2 value)
        {
            return $"({FormatFloat(value.x)},{FormatFloat(value.y)})";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private bool TryConsumePredictedShot(ShotResult confirmed_result, out ShotResult predicted_result)
        {
            PredictedShotKey key = new(confirmed_result.WeaponSlot, confirmed_result.ShotSequence);
            if (_predicted_shots.TryGetValue(key, out PredictedShot shot))
            {
                predicted_result = shot.Result;
                _predicted_shots.Remove(key);
                return true;
            }

            predicted_result = default;
            return false;
        }

        private void WarnIfPredictedShotMismatch(ShotResult predicted_result, ShotResult server_result)
        {
            float point_distance = Vector3.Distance(predicted_result.Point, server_result.Point);
            float direction_angle = Vector3.Angle(predicted_result.Direction, server_result.Direction);
            if (point_distance <= PredictedShotPointWarningThreshold &&
                direction_angle <= PredictedShotAngleWarningThreshold)
            {
                return;
            }

            Debug.LogWarning(
                $"{LogPrefix} Predicted shot mismatch. netId={netId} slot={server_result.WeaponSlot} " +
                $"inputTick={server_result.InputTick} sequence={server_result.ShotSequence} " +
                $"pointDistance={point_distance:0.###} directionAngle={direction_angle:0.###}");
        }

        private void CleanupPredictedShots()
        {
            if (!isClient || !isOwned || _predicted_shots.Count == 0 || _character == null || _character.TickManager == null)
                return;

            int current_tick = _character.TickManager.CurrentTick;
            List<PredictedShotKey> expired_keys = null;
            foreach (KeyValuePair<PredictedShotKey, PredictedShot> pair in _predicted_shots)
            {
                if (current_tick - pair.Value.CreatedTick <= PredictedShotTimeoutTicks)
                    continue;

                expired_keys ??= new List<PredictedShotKey>();
                expired_keys.Add(pair.Key);
            }

            if (expired_keys == null)
                return;

            for (int i = 0; i < expired_keys.Count; i++)
                _predicted_shots.Remove(expired_keys[i]);

            Debug.LogWarning($"{LogPrefix} Predicted shot was not confirmed. netId={netId} count={expired_keys.Count}");
            if (_predicted_shots.Count == 0)
                ResetPredictedStateFromSync();
        }

        private int GetCurrentTick()
        {
            return _character == null || _character.TickManager == null
                ? 0
                : _character.TickManager.CurrentTick;
        }

        private readonly struct PredictedShot
        {
            public readonly ShotResult Result;
            public readonly int CreatedTick;

            public PredictedShot(ShotResult result, int created_tick)
            {
                Result = result;
                CreatedTick = created_tick;
            }
        }

        private readonly struct PredictedShotKey : IEquatable<PredictedShotKey>
        {
            private readonly WeaponSlot _slot;
            private readonly int _sequence;

            public PredictedShotKey(WeaponSlot slot, int sequence)
            {
                _slot = slot;
                _sequence = sequence;
            }

            public bool Equals(PredictedShotKey other)
            {
                return _slot == other._slot && _sequence == other._sequence;
            }

            public override bool Equals(object obj)
            {
                return obj is PredictedShotKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return ((int)_slot * 397) ^ _sequence;
            }
        }
    }
}
