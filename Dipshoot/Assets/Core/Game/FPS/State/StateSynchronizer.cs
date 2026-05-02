using Game.Players.Input;
using Game.Players.State;
using Mirror;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class StateSynchronizer : NetworkBehaviour
    {
        private const string LogPrefix = "[NetTick][StateSync]";

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private Movement _movement;
        [SerializeField] private InputBufferSynchronizer _input_buffer_synchronizer;
        [SerializeField] private float _position_error_threshold = 0.001f;
        [SerializeField] private float _rotation_error_threshold = 0.1f;
        [SerializeField] private int _remote_interpolation_back_ticks = 2;
        [SerializeField] private float _server_tick_offset_lerp_factor = 0.1f;

        public int LastReceivedStateTick { get; private set; } = -1;
        public int LastAppliedStateTick { get; private set; } = -1;
        public int LastProcessedInputTick { get; private set; } = -1;

        private bool _is_subscribed_to_tick;
        private bool _has_server_tick_offset;
        private double _server_tick_offset;
        private readonly RemoteInterpolationBuffer _remote_interpolation_buffer = new();

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            CacheReferences();
            TrySubscribeToServerTick();
            UpdateRemoteInterpolation();
        }

        private void OnDisable()
        {
            TryUnsubscribeFromServerTick();
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_movement == null)
                _movement = GetComponent<Movement>();

            if (_input_buffer_synchronizer == null)
                _input_buffer_synchronizer = GetComponent<InputBufferSynchronizer>();
        }

        private void TrySubscribeToServerTick()
        {
            if (_is_subscribed_to_tick || !isServer || _character == null || _character.TickManager == null)
                return;

            _character.TickManager.OnPostTick += HandleServerPostTick;
            _is_subscribed_to_tick = true;
        }

        private void TryUnsubscribeFromServerTick()
        {
            if (!_is_subscribed_to_tick || _character == null || _character.TickManager == null)
                return;

            _character.TickManager.OnPostTick -= HandleServerPostTick;
            _is_subscribed_to_tick = false;
        }

        private void HandleServerPostTick()
        {
            if (_character == null || _character.TickManager == null || _movement == null)
            {
                return;
            }

            int server_tick = _character.TickManager.CurrentTick;
            if (!_character.StateBuffer.TryGet(server_tick, out PlayerState state))
            {
                return;
            }

            PlayerStateSnapshot snapshot = PlayerStateSnapshot.Create(
                state,
                server_tick,
                _movement.LastServerProcessedInputTick);

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
            _movement.ReplayFromTick(replay_from_tick, current_tick);

            if (_character.StateBuffer.TryGet(current_tick, out PlayerState replayed_state))
            {
                _movement.ApplyState(replayed_state);
                LastAppliedStateTick = replayed_state.Tick;
            }
            else
            {
                _movement.ApplyState(state);
                LastAppliedStateTick = state.Tick;
            }

            AcknowledgeProcessedInputs(snapshot.LastProcessedInputTick);
        }

        private void ReceiveRemoteAuthoritativeState(PlayerStateSnapshot snapshot)
        {
            if (_character == null || _movement == null)
                return;

            LastReceivedStateTick = snapshot.ServerTick;
            UpdateServerTickEstimate(snapshot.ServerTick);
            PlayerState state = snapshot.ToState(snapshot.ServerTick);
            _remote_interpolation_buffer.Add(state);
        }

        private void ApplyAuthoritativeState(PlayerState state)
        {
            _character.StateBuffer.Add(state);
            _movement.ApplyState(state);
            LastAppliedStateTick = state.Tick;
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
            LastAppliedStateTick = interpolated_state.Tick;
            _remote_interpolation_buffer.RemoveUpTo(Mathf.FloorToInt(render_tick) - 1);
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

        private double GetEstimatedServerTick()
        {
            if (_character == null || _character.TickManager == null)
                return 0d;

            double network_time_tick = NetworkTime.time / _character.TickManager.TickDelta;
            if (!_has_server_tick_offset)
                return network_time_tick;

            return network_time_tick + _server_tick_offset;
        }
    }
}
