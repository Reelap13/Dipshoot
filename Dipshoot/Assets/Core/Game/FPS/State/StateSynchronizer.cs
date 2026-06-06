using Game.Players.Input;
using Game.Players.State;
using Game.TickSystem;
using Mirror;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class StateSynchronizer : NetworkBehaviour, ITickSystem, IPlayerSimulationResettable
    {
        private const string LogPrefix = "[NetTick][StateSync]";
        private const string TickSyncDebugPrefix = "[TickSync][Client]";

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private AimController _aim_controller;
        [SerializeField] private Movement _movement;
        [SerializeField] private InputBufferSynchronizer _input_buffer_synchronizer;
        [SerializeField] private float _position_error_threshold = 0.001f;
        [SerializeField] private float _rotation_error_threshold = 0.1f;
        [SerializeField] private int _remote_interpolation_back_ticks = 2;
        [SerializeField] private float _server_tick_offset_lerp_factor = 0.1f;
        [SerializeField] private bool _tick_sync_debug_enabled = true;
        [SerializeField] private int _tick_sync_debug_max_logs_per_second = 3;
        [SerializeField] private bool _client_tick_correction_enabled = true;
        [SerializeField] private float _target_server_lead_ticks = 3f;
        [SerializeField] private float _tick_correction_deadzone = 1f;
        [SerializeField] private float _tick_correction_proportional = 0.015f;
        [SerializeField] private float _tick_correction_max_scale_delta = 0.05f;
        [SerializeField] private float _tick_correction_lerp = 0.1f;

        public int LastReceivedStateTick { get; private set; } = -1;
        public int LastAppliedStateTick { get; private set; } = -1;
        public int LastProcessedInputTick { get; private set; } = -1;

        private TickManager _registered_tick_manager;
        private bool _has_server_tick_offset;
        private double _server_tick_offset;
        private bool _has_render_state;
        private PlayerState _render_state;
        private float _last_tick_correction_error;
        private float _tick_sync_debug_window_started_at;
        private int _tick_sync_debug_logged_in_window;
        private int _tick_sync_debug_suppressed;
        private readonly RemoteInterpolationBuffer _remote_interpolation_buffer = new();

        public TickLayer TickLayer => TickLayer.StateSnapshot;
        public int TickOrder => 0;

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            CacheReferences();
            TryRegisterTickSystem();
            UpdateRemoteInterpolation();
        }

        private void OnDisable()
        {
            ResetClientTickCorrection();
            TryUnregisterTickSystem();
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_movement == null)
                _movement = GetComponent<Movement>();

            if (_aim_controller == null)
                _aim_controller = GetComponent<AimController>();

            if (_input_buffer_synchronizer == null)
                _input_buffer_synchronizer = GetComponent<InputBufferSynchronizer>();
        }

        public bool ShouldTick(GameTickContext context)
        {
            return isServer && _character != null && _character.TickManager == context.TickManager;
        }

        public void Tick(GameTickContext context)
        {
            SendAuthoritativeState();
        }

        public bool TryGetRenderState(out PlayerState state)
        {
            state = _render_state;
            return _has_render_state;
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

        private void SendAuthoritativeState()
        {
            if (_character == null || _character.TickManager == null || _movement == null)
            {
                return;
            }

            int server_tick = _character.TickManager.CurrentTick;
            int state_tick = _movement.LastServerProcessedInputTick;
            if (state_tick < 0)
                return;

            if (!_character.StateBuffer.TryGet(state_tick, out PlayerState state) &&
                !_character.StateBuffer.TryGetLastAtOrBefore(state_tick, out state))
            {
                return;
            }

            PlayerStateSnapshot snapshot = PlayerStateSnapshot.Create(
                state,
                server_tick,
                state.Tick);

            if (connectionToClient != null)
                TargetReceiveAuthoritativeState(snapshot);

            RpcReceiveRemoteAuthoritativeState(snapshot);
        }

        [TargetRpc]
        private void TargetReceiveAuthoritativeState(PlayerStateSnapshot snapshot)
        {
            if (!isOwned)
                return;

            ReceiveOwnerAuthoritativeState(snapshot);
        }

        [ClientRpc]
        private void RpcReceiveRemoteAuthoritativeState(PlayerStateSnapshot snapshot)
        {
            if (isOwned)
                return;

            ReceiveRemoteAuthoritativeState(snapshot);
        }

        private void ReceiveOwnerAuthoritativeState(PlayerStateSnapshot snapshot)
        {
            LastReceivedStateTick = snapshot.ServerTick;
            LastProcessedInputTick = snapshot.LastProcessedInputTick;
            UpdateServerTickEstimate(snapshot.ServerTick);
            ApplyClientTickCorrection();
            LogTickSyncSnapshot(snapshot, true);

            if (_character == null || _movement == null)
                return;

            if (snapshot.LastProcessedInputTick < 0)
                return;

            PlayerState state = snapshot.ToState(snapshot.LastProcessedInputTick);

            if (_character.TickManager == null)
            {
                ApplyAuthoritativeState(state);
                AcknowledgeProcessedInputs(snapshot.LastProcessedInputTick);
                return;
            }

            int predicted_tick = snapshot.LastProcessedInputTick;
            if (!_character.StateBuffer.TryGet(predicted_tick, out PlayerState predicted_state))
            {
                ApplyAuthoritativeState(state);
                AcknowledgeProcessedInputs(snapshot.LastProcessedInputTick);
                return;
            }

            if (!IsReconciliationRequired(predicted_state, state))
            {
                AcknowledgeProcessedInputs(snapshot.LastProcessedInputTick);
                return;
            }

            _character.StateBuffer.Add(state);

            int current_tick = _character.TickManager.CurrentTick;
            int replay_from_tick = predicted_tick + 1;
            ReplayFromTick(replay_from_tick, current_tick);

            if (_character.StateBuffer.TryGet(current_tick, out PlayerState replayed_state))
            {
                _movement.ApplyState(replayed_state);
                SetRenderState(replayed_state);
            }
            else
            {
                _movement.ApplyState(state);
                SetRenderState(state);
            }

            AcknowledgeProcessedInputs(snapshot.LastProcessedInputTick);
        }

        private void ReplayFromTick(int from_tick, int to_tick)
        {
            if (to_tick < from_tick)
                return;

            for (int tick = from_tick; tick <= to_tick; tick++)
            {
                if (_aim_controller != null)
                    _aim_controller.SimulateTick(tick);

                _movement.SimulateTick(tick);
            }
        }

        private void ReceiveRemoteAuthoritativeState(PlayerStateSnapshot snapshot)
        {
            if (_character == null || _movement == null)
                return;

            LastReceivedStateTick = snapshot.ServerTick;
            UpdateServerTickEstimate(snapshot.ServerTick);
            LogTickSyncSnapshot(snapshot, false);
            PlayerState state = snapshot.ToState(snapshot.ServerTick);
            _remote_interpolation_buffer.Add(state);
        }

        private void ApplyAuthoritativeState(PlayerState state)
        {
            _character.StateBuffer.Add(state);
            _movement.ApplyState(state);
            SetRenderState(state);
        }

        private void AcknowledgeProcessedInputs(int last_processed_input_tick)
        {
            if (_input_buffer_synchronizer == null)
                return;

            _input_buffer_synchronizer.AcknowledgeInputsUpTo(last_processed_input_tick);
        }

        private bool IsReconciliationRequired(PlayerState predicted_state, PlayerState authoritative_state)
        {
            float position_error =
                (predicted_state.Position - authoritative_state.Position).sqrMagnitude;
            float max_position_error = _position_error_threshold * _position_error_threshold;

            if (position_error > max_position_error)
                return true;

            float rotation_error =
                Quaternion.Angle(predicted_state.Rotation, authoritative_state.Rotation);

            return rotation_error > _rotation_error_threshold;
        }

        private void UpdateRemoteInterpolation()
        {
            if (!isClient || isOwned || _character == null || _movement == null || _character.TickManager == null)
                return;

            double estimated_server_tick = GetEstimatedServerTick();
            float render_tick = (float)(estimated_server_tick - _remote_interpolation_back_ticks);

            if (!_remote_interpolation_buffer.TryGetInterpolatedState(render_tick, out PlayerState interpolated_state))
                return;

            _movement.ApplyState(interpolated_state);
            SetRenderState(interpolated_state);
            _remote_interpolation_buffer.RemoveUpTo(Mathf.FloorToInt(render_tick) - 1);
        }

        private void SetRenderState(PlayerState state)
        {
            _render_state = state;
            _has_render_state = true;
            LastAppliedStateTick = state.Tick;
        }

        private void UpdateServerTickEstimate(int server_tick)
        {
            if (!isClient || _character == null || _character.TickManager == null)
                return;

            double network_time_tick = NetworkTime.time / _character.TickManager.TickDelta;
            double sample_offset = server_tick - network_time_tick;

            if (!_has_server_tick_offset)
            {
                _server_tick_offset = sample_offset;
                _has_server_tick_offset = true;
                return;
            }

            _server_tick_offset = Mathf.Lerp(
                (float)_server_tick_offset,
                (float)sample_offset,
                _server_tick_offset_lerp_factor);
        }

        private void ApplyClientTickCorrection()
        {
            if (_character == null || _character.TickManager == null)
                return;

            TickManager tick_manager = _character.TickManager;
            if (!_client_tick_correction_enabled || !_has_server_tick_offset)
            {
                tick_manager.TickRateScale = Mathf.Lerp(
                    tick_manager.TickRateScale,
                    1f,
                    _tick_correction_lerp);
                _last_tick_correction_error = 0f;
                return;
            }

            _last_tick_correction_error = (float)(GetEstimatedServerTick() -
                tick_manager.CurrentTick -
                _target_server_lead_ticks);

            float target_scale = 1f;
            if (Mathf.Abs(_last_tick_correction_error) > _tick_correction_deadzone)
            {
                float scale_delta = Mathf.Clamp(
                    _last_tick_correction_error * _tick_correction_proportional,
                    -_tick_correction_max_scale_delta,
                    _tick_correction_max_scale_delta);
                target_scale += scale_delta;
            }

            tick_manager.TickRateScale = Mathf.Lerp(
                tick_manager.TickRateScale,
                target_scale,
                _tick_correction_lerp);
        }

        private void ResetClientTickCorrection()
        {
            if (isOwned && _character != null && _character.TickManager != null)
                _character.TickManager.TickRateScale = 1f;
        }

        public void ResetSimulation()
        {
            LastReceivedStateTick = -1;
            LastAppliedStateTick = -1;
            LastProcessedInputTick = -1;
            _has_render_state = false;
            _remote_interpolation_buffer.Clear();
        }

        private double GetEstimatedServerTick()
        {
            if (_character == null || _character.TickManager == null)
                return 0d;

            double network_time_tick = NetworkTime.time / _character.TickManager.TickDelta;
            if (!_has_server_tick_offset)
                return network_time_tick;

            return network_time_tick + _server_tick_offset;
        }

        private void LogTickSyncSnapshot(PlayerStateSnapshot snapshot, bool owner_snapshot)
        {
            if (!_tick_sync_debug_enabled ||
                _character == null ||
                _character.TickManager == null ||
                !ShouldLogTickSyncDebug())
            {
                return;
            }

            int suppressed = _tick_sync_debug_suppressed;
            _tick_sync_debug_suppressed = 0;
            int client_tick = _character.TickManager.CurrentTick;
            float tick_delta = _character.TickManager.TickDelta;
            double network_time_tick = tick_delta <= 0f ? 0d : NetworkTime.time / tick_delta;
            double estimated_server_tick = GetEstimatedServerTick();
            double offset = _has_server_tick_offset ? _server_tick_offset : 0d;
            double rtt_ms = NetworkTime.rtt * 1000d;
            float tick_rate_scale = _character.TickManager.CurrentTickRateScale;

            Debug.Log(
                $"{TickSyncDebugPrefix} owner={owner_snapshot} netId={netId} " +
                $"clientTick={client_tick} serverTick={snapshot.ServerTick} " +
                $"lastProcessedInputTick={snapshot.LastProcessedInputTick} " +
                $"serverMinusClient={snapshot.ServerTick - client_tick} " +
                $"serverMinusInput={snapshot.ServerTick - snapshot.LastProcessedInputTick} " +
                $"networkTimeTick={network_time_tick:0.##} estimatedServerTick={estimated_server_tick:0.##} " +
                $"offset={offset:0.##} estimatedMinusClient={estimated_server_tick - client_tick:0.##} " +
                $"tickRateScale={tick_rate_scale:0.###} tickCorrectionError={_last_tick_correction_error:0.##} " +
                $"rttMs={rtt_ms:0.#} suppressed={suppressed}");
        }

        private bool ShouldLogTickSyncDebug()
        {
            int max_logs = Mathf.Max(1, _tick_sync_debug_max_logs_per_second);
            if (Time.unscaledTime - _tick_sync_debug_window_started_at >= 1f)
            {
                _tick_sync_debug_window_started_at = Time.unscaledTime;
                _tick_sync_debug_logged_in_window = 0;
            }

            if (_tick_sync_debug_logged_in_window >= max_logs)
            {
                _tick_sync_debug_suppressed++;
                return false;
            }

            _tick_sync_debug_logged_in_window++;
            return true;
        }
    }
}
