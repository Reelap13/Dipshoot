using System;
using Game.TickSystem;
using Mirror;
using Server.Match;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Players.Input
{
    public readonly struct PlayerInputBatch
    {
        public readonly int FromTick;
        public readonly int ToTick;
        public readonly PlayerInputData[] Inputs;

        public bool IsEmpty => Inputs == null || Inputs.Length == 0;

        public PlayerInputBatch(int from_tick, int to_tick, PlayerInputData[] inputs)
        {
            FromTick = from_tick;
            ToTick = to_tick;
            Inputs = inputs;
        }
    }

    [DisallowMultipleComponent]
    public class InputBufferSynchronizer : NetworkBehaviour, ITickSystem, IPlayerSimulationResettable
    {
        private const string LogPrefix = "[NetTick][InputSync]";

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerInputController _inputController;
        [FormerlySerializedAs("_redundant_input_ticks")]
        [SerializeField] private int _max_input_batch_ticks = 32;
        [SerializeField] private float _max_buffered_input_age_ms = 1000f;
        [SerializeField] private float _server_max_input_age_ms = 1000f;
        [SerializeField] private float _server_max_future_input_ms = 250f;
        [SerializeField] private int _server_input_warning_age_ticks = 20;
        [SerializeField] private int _server_input_warning_gap_ticks = 3;
        [SerializeField] private int _server_input_warning_batch_size = 32;
        [Header("Server Input Playout")]
        [SerializeField] private int _server_target_buffer_ticks = 3;
        [SerializeField] private int _server_max_repeat_ticks = 3;
        [SerializeField] private int _server_decay_ticks = 4;
        [SerializeField] private int _server_severe_backlog_ticks = 12;
        [SerializeField] private int _server_max_rebuffer_ticks = 6;
        [SerializeField] private int _server_catchup_interval_ticks = 10;
        [SerializeField] private bool _server_adaptive_buffer_enabled = true;
        [SerializeField] private int _server_min_buffer_ticks = 2;
        [SerializeField] private int _server_max_buffer_ticks = 12;
        [SerializeField] private float _server_jitter_buffer_multiplier = 2f;
        [SerializeField] private int _server_target_decrease_interval_ticks = 120;

        public int LastCapturedTick { get; private set; } = -1;
        public int LastSentTick { get; private set; } = -1;
        public int LastAcknowledgedTick { get; private set; } = -1;
        public int LastReceivedByServerTick { get; private set; } = -1;
        public int LastCommittedByServerTick { get; private set; } = -1;
        public int ServerInputQueueDepth => GetServerInputQueueDepth();
        public int ServerTargetBufferTicks => isServer && isClient && isOwned
            ? 1
            : Mathf.Max(1, _server_dynamic_target_buffer_ticks);
        public float ServerInputJitterTicks => Mathf.Sqrt(Mathf.Max(0f, _server_arrival_delay_variance));
        public int ServerMissingInputTicks => _server_missing_input_ticks;
        public int ServerBufferResetCount => _server_buffer_reset_count;
        public ServerInputSource LastServerInputSource => _last_server_frame.Source;

        public bool HasPendingInputs => LastCapturedTick > LastAcknowledgedTick;
        public bool HasUnsentInputs => LastCapturedTick > LastSentTick;

        public event Action<int> OnPendingInputsChanged;
        public event Action<int> OnInputsAcknowledged;
        public event Action<int> OnInputsReceivedByServer;

        private TickManager _registered_tick_manager;
        private bool _server_playout_initialized;
        private int _server_next_input_tick = -1;
        private int _server_missing_input_ticks;
        private int _server_buffer_reset_count;
        private bool _server_rebuffering;
        private int _server_rebuffer_ticks;
        private int _last_server_catchup_tick = -1;
        private int _server_dynamic_target_buffer_ticks;
        private bool _server_target_increase_pending;
        private bool _has_server_arrival_delay_sample;
        private float _server_arrival_delay_mean;
        private float _server_arrival_delay_variance;
        private int _last_server_target_change_tick = -1;
        private bool _has_last_received_server_input;
        private PlayerInputData _last_received_server_input;
        private int _last_resolved_server_tick = -1;
        private ServerInputFrame _last_server_frame;
        private bool _has_logged_server_source;
        private ServerInputSource _last_logged_server_source;

        public TickLayer TickLayer => TickLayer.InputSend;
        public int TickOrder => 0;

        private void Awake()
        {
            CacheReferences();
            ResetServerPlayout();
        }

        private void Update()
        {
            CacheReferences();
            TryRegisterTickSystem();
        }

        private void OnEnable()
        {
            CacheReferences();
            TryRegisterTickSystem();

            if (_inputController != null)
                _inputController.OnInputCaptured += HandleInputCaptured;
        }

        private void OnDisable()
        {
            if (_inputController != null)
                _inputController.OnInputCaptured -= HandleInputCaptured;

            TryUnregisterTickSystem();
        }

        public bool TryBuildPendingBatch(out PlayerInputBatch batch)
        {
            batch = default;

            if (_character == null || !HasPendingInputs)
                return false;

            int from_tick = Mathf.Max(
                LastAcknowledgedTick + 1,
                LastCapturedTick - Mathf.Max(1, _max_input_batch_ticks) + 1);
            int to_tick = LastCapturedTick;
            var inputs = _character.InputBuffet.GetRange(from_tick, to_tick);
            if (inputs.Count == 0)
                return false;

            batch = new PlayerInputBatch(from_tick, to_tick, inputs.ToArray());
            return true;
        }

        public bool TryBuildUnsentBatch(out PlayerInputBatch batch)
        {
            batch = default;

            if (_character == null || LastCapturedTick <= LastAcknowledgedTick)
                return false;

            int from_tick = Mathf.Max(
                LastAcknowledgedTick + 1,
                LastCapturedTick - Mathf.Max(1, _max_input_batch_ticks) + 1);
            int to_tick = LastCapturedTick;
            var inputs = _character.InputBuffet.GetRange(from_tick, to_tick);
            if (inputs.Count == 0)
                return false;

            batch = new PlayerInputBatch(from_tick, to_tick, inputs.ToArray());
            return true;
        }

        public void MarkInputsAsSentUpTo(int tick)
        {
            if (tick <= LastSentTick)
                return;

            LastSentTick = tick;
        }

        public void AcknowledgeInputsUpTo(int tick)
        {
            if (_character == null || tick <= LastAcknowledgedTick)
                return;

            LastAcknowledgedTick = tick;
            if (LastSentTick < LastAcknowledgedTick)
                LastSentTick = LastAcknowledgedTick;

            _character.InputBuffet.RemoveUpTo(tick);
            OnInputsAcknowledged?.Invoke(tick);
        }

        public void DiscardInputsUpTo(int tick)
        {
            if (_character == null || tick <= LastAcknowledgedTick)
                return;

            LastAcknowledgedTick = tick;
            if (LastSentTick < LastAcknowledgedTick)
                LastSentTick = LastAcknowledgedTick;

            if (LastReceivedByServerTick < LastAcknowledgedTick)
                LastReceivedByServerTick = LastAcknowledgedTick;

            _character.InputBuffet.RemoveUpTo(tick);
            OnInputsAcknowledged?.Invoke(tick);
        }

        public void ResetTracking(int tick = -1)
        {
            LastCapturedTick = tick;
            LastSentTick = tick;
            LastAcknowledgedTick = tick;
            LastReceivedByServerTick = isServer ? -1 : tick;
            ResetServerPlayout();
        }

        private void HandleInputCaptured(PlayerInputData input)
        {
            if (!isOwned)
                return;

            if (input.Tick <= LastCapturedTick)
                return;

            LastCapturedTick = input.Tick;
            OnPendingInputsChanged?.Invoke(input.Tick);
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_inputController == null)
                _inputController = GetComponent<PlayerInputController>();
        }

        public bool ShouldTick(GameTickContext context)
        {
            return isOwned && _character != null && _character.TickManager == context.TickManager;
        }

        public void Tick(GameTickContext context)
        {
            DiscardStaleClientInputs(context.Tick);
            TrySendUnsentInputs();
        }

        public void ResetSimulation()
        {
            int tick = _character == null || _character.TickManager == null
                ? -1
                : _character.TickManager.CurrentTick;

            ResetTracking(tick);
        }

        public ServerInputFrame ResolveServerInputFrame(int server_tick)
        {
            if (!isServer || _character == null)
                return CreateUncommittedNeutralFrame(server_tick);

            if (_last_resolved_server_tick == server_tick)
                return _last_server_frame;

            if (!_server_playout_initialized && !TryInitializeServerPlayout())
                return CacheServerFrame(CreateUncommittedNeutralFrame(server_tick));

            int queue_depth = GetServerInputQueueDepth();
            bool did_reset_buffer = false;
            bool did_catch_up = false;
            if (queue_depth > ServerTargetBufferTicks + Mathf.Max(1, _server_severe_backlog_ticks) &&
                _character.InputBuffet.TryGetLastAtOrBefore(int.MaxValue, out PlayerInputData newest_input))
            {
                _character.InputBuffet.RemoveUpTo(newest_input.Tick - 1);
                _server_next_input_tick = newest_input.Tick;
                _server_buffer_reset_count++;
                did_reset_buffer = true;
            }

            if (!did_reset_buffer)
                did_catch_up = TryCatchUpOneInput(server_tick, ref queue_depth);

            int input_tick = _server_next_input_tick;
            bool has_expected_input =
                _character.InputBuffet.TryGet(input_tick, out PlayerInputData received_input);
            if (!did_reset_buffer &&
                has_expected_input &&
                (_server_missing_input_ticks > 0 || _server_target_increase_pending) &&
                queue_depth < ServerTargetBufferTicks)
            {
                _server_rebuffering = true;
            }

            if (_server_rebuffering &&
                queue_depth < ServerTargetBufferTicks &&
                _server_rebuffer_ticks < Mathf.Max(0, _server_max_rebuffer_ticks))
            {
                _server_rebuffer_ticks++;
                return CacheServerFrame(CreateBufferingFrame(server_tick));
            }

            _server_rebuffering = false;
            _server_rebuffer_ticks = 0;
            _server_target_increase_pending = false;
            PlayerInputData resolved_input;
            ServerInputSource source;

            if (has_expected_input)
            {
                resolved_input = received_input;
                source = did_reset_buffer
                    ? ServerInputSource.BufferReset
                    : did_catch_up
                        ? ServerInputSource.CatchUp
                        : ServerInputSource.Received;
                _last_received_server_input = received_input;
                _has_last_received_server_input = true;
                _server_missing_input_ticks = 0;
            }
            else
            {
                _server_missing_input_ticks++;
                if (_server_missing_input_ticks == 1)
                    IncreaseServerTargetForUnderflow(server_tick);
                resolved_input = CreatePredictedServerInput(input_tick, _server_missing_input_ticks, out source);
            }

            resolved_input.Tick = input_tick;
            LastCommittedByServerTick = input_tick;
            _server_next_input_tick = input_tick + 1;
            _character.InputBuffet.RemoveUpTo(input_tick);

            return CacheServerFrame(new ServerInputFrame(
                server_tick,
                input_tick,
                resolved_input,
                source));
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

        public bool TrySendUnsentInputs()
        {
            if (!isOwned)
                return false;

            if (!TryBuildUnsentBatch(out PlayerInputBatch batch))
            {
                return false;
            }

            CmdSendInputs(batch.Inputs);
            MarkInputsAsSentUpTo(batch.ToTick);
            return true;
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSendInputs(PlayerInputData[] inputs)
        {
            if (_character == null || inputs == null || inputs.Length == 0)
            {
                return;
            }

            int server_tick = _character.TickManager == null ? 0 : _character.TickManager.CurrentTick;
            int earliest_allowed_tick = server_tick - GetTicksFromMs(_server_max_input_age_ms);
            int latest_allowed_tick = server_tick + GetTicksFromMs(_server_max_future_input_ms);
            int previous_received_tick = LastReceivedByServerTick;
            int newest_received_tick = LastReceivedByServerTick;
            int last_received_tick = LastReceivedByServerTick;
            int accepted_count = 0;
            int duplicate_count = 0;
            int too_old_count = 0;
            int too_new_count = 0;
            foreach (PlayerInputData input in inputs)
            {
                if (input.Tick <= LastCommittedByServerTick ||
                    _character.InputBuffet.TryGet(input.Tick, out _))
                {
                    duplicate_count++;
                    continue;
                }

                if (_character.TickManager != null && input.Tick < earliest_allowed_tick)
                {
                    too_old_count++;
                    continue;
                }

                if (_character.TickManager != null && input.Tick > latest_allowed_tick)
                {
                    too_new_count++;
                    continue;
                }

                _character.InputBuffet.Add(input);
                accepted_count++;
                if (input.Tick > newest_received_tick)
                    newest_received_tick = input.Tick;
                if (input.Tick > last_received_tick)
                    last_received_tick = input.Tick;
            }

            if (newest_received_tick > LastReceivedByServerTick)
                LastReceivedByServerTick = newest_received_tick;

            if (last_received_tick > previous_received_tick)
                UpdateServerTargetBuffer(server_tick, last_received_tick);

            LogServerInputWarnings(
                inputs,
                previous_received_tick,
                newest_received_tick,
                last_received_tick,
                accepted_count,
                duplicate_count,
                too_old_count,
                too_new_count);

            TargetRegisterInputsReceived(newest_received_tick);
        }

        private bool TryInitializeServerPlayout()
        {
            if (_character == null ||
                !_character.InputBuffet.TryGetFirstAfter(-1, out PlayerInputData first_input))
            {
                return false;
            }

            int contiguous_inputs = 0;
            int target = ServerTargetBufferTicks;
            for (int tick = first_input.Tick; contiguous_inputs < target; tick++)
            {
                if (!_character.InputBuffet.TryGet(tick, out _))
                    break;

                contiguous_inputs++;
            }

            if (contiguous_inputs < target)
                return false;

            _server_playout_initialized = true;
            _server_next_input_tick = first_input.Tick;
            _server_missing_input_ticks = 0;
            return true;
        }

        private void UpdateServerTargetBuffer(int server_tick, int newest_input_tick)
        {
            if (!_server_adaptive_buffer_enabled || isClient && isOwned)
                return;

            float sample = server_tick - newest_input_tick;
            const float alpha = 0.1f;
            if (!_has_server_arrival_delay_sample)
            {
                _has_server_arrival_delay_sample = true;
                _server_arrival_delay_mean = sample;
                _server_arrival_delay_variance = 0f;
                return;
            }

            float delta = sample - _server_arrival_delay_mean;
            _server_arrival_delay_mean += alpha * delta;
            _server_arrival_delay_variance =
                (1f - alpha) * (_server_arrival_delay_variance + alpha * delta * delta);

            int min_target = Mathf.Max(1, _server_min_buffer_ticks);
            int max_target = Mathf.Max(min_target, _server_max_buffer_ticks);
            int base_target = Mathf.Clamp(_server_target_buffer_ticks, min_target, max_target);
            int jitter_margin = Mathf.CeilToInt(
                ServerInputJitterTicks * Mathf.Max(0f, _server_jitter_buffer_multiplier));
            int desired_target = Mathf.Clamp(base_target + jitter_margin, min_target, max_target);

            if (desired_target > _server_dynamic_target_buffer_ticks)
            {
                _server_dynamic_target_buffer_ticks = desired_target;
                _server_target_increase_pending = true;
                _last_server_target_change_tick = server_tick;
                return;
            }

            int decrease_interval = Mathf.Max(1, _server_target_decrease_interval_ticks);
            if (desired_target < _server_dynamic_target_buffer_ticks &&
                _server_missing_input_ticks == 0 &&
                (_last_server_target_change_tick < 0 ||
                 server_tick - _last_server_target_change_tick >= decrease_interval))
            {
                _server_dynamic_target_buffer_ticks--;
                _last_server_target_change_tick = server_tick;
            }
        }

        private void IncreaseServerTargetForUnderflow(int server_tick)
        {
            if (!_server_adaptive_buffer_enabled || isClient && isOwned)
                return;

            int max_target = Mathf.Max(
                Mathf.Max(1, _server_min_buffer_ticks),
                _server_max_buffer_ticks);
            if (_server_dynamic_target_buffer_ticks >= max_target)
                return;

            _server_dynamic_target_buffer_ticks++;
            _server_target_increase_pending = true;
            _last_server_target_change_tick = server_tick;
        }

        private PlayerInputData CreatePredictedServerInput(
            int input_tick,
            int missing_ticks,
            out ServerInputSource source)
        {
            return ServerInputPlayoutPolicy.CreatePredictedInput(
                _last_received_server_input,
                _has_last_received_server_input,
                input_tick,
                missing_ticks,
                _server_max_repeat_ticks,
                _server_decay_ticks,
                out source);
        }

        private bool TryCatchUpOneInput(int server_tick, ref int queue_depth)
        {
            int interval_ticks = Mathf.Max(1, _server_catchup_interval_ticks);
            if (queue_depth <= ServerTargetBufferTicks + 1 ||
                _last_server_catchup_tick >= 0 &&
                server_tick - _last_server_catchup_tick < interval_ticks)
            {
                return false;
            }

            int skipped_tick = _server_next_input_tick;
            if (!_character.InputBuffet.TryGet(skipped_tick, out PlayerInputData skipped_input) ||
                !_character.InputBuffet.TryGet(skipped_tick + 1, out PlayerInputData next_input) ||
                !ServerInputPlayoutPolicy.CanCoalesce(skipped_input))
            {
                return false;
            }

            next_input.Look += skipped_input.Look;
            _character.InputBuffet.Add(next_input);
            _character.InputBuffet.RemoveUpTo(skipped_tick);
            LastCommittedByServerTick = skipped_tick;
            _server_next_input_tick = skipped_tick + 1;
            _last_server_catchup_tick = server_tick;
            queue_depth = Mathf.Max(0, queue_depth - 1);
            return true;
        }

        private ServerInputFrame CreateUncommittedNeutralFrame(int server_tick)
        {
            PlayerInputData input = default;
            input.Tick = -1;
            return new ServerInputFrame(
                server_tick,
                -1,
                input,
                ServerInputSource.None);
        }

        private ServerInputFrame CreateBufferingFrame(int server_tick)
        {
            PlayerInputData input = default;
            input.Tick = LastCommittedByServerTick;
            return new ServerInputFrame(
                server_tick,
                LastCommittedByServerTick,
                input,
                ServerInputSource.Buffering);
        }

        private ServerInputFrame CacheServerFrame(ServerInputFrame frame)
        {
            _last_resolved_server_tick = frame.ServerTick;
            _last_server_frame = frame;
            LogServerPlayoutTransition(frame);
            return frame;
        }

        private void LogServerPlayoutTransition(ServerInputFrame frame)
        {
            if (!isServer ||
                _character == null ||
                _has_logged_server_source && frame.Source == _last_logged_server_source)
            {
                return;
            }

            _has_logged_server_source = true;
            _last_logged_server_source = frame.Source;
            MatchLogContext.Get(gameObject.scene)?.Write(
                "input",
                $"[InputPlayout][Server] netId={netId} serverTick={frame.ServerTick} " +
                $"inputTick={frame.SourceInputTick} source={frame.Source} " +
                $"queue={GetServerInputQueueDepth()}/{ServerTargetBufferTicks} " +
                $"latestReceived={LastReceivedByServerTick} committed={LastCommittedByServerTick} " +
                $"missing={_server_missing_input_ticks} resets={_server_buffer_reset_count}");
        }

        private int GetServerInputQueueDepth()
        {
            if (_character == null || !_server_playout_initialized)
                return _character == null ? 0 : _character.InputBuffet.Count;

            int newest_tick = _character.InputBuffet.NewestTick;
            return newest_tick < _server_next_input_tick
                ? 0
                : newest_tick - _server_next_input_tick + 1;
        }

        private void ResetServerPlayout()
        {
            LastCommittedByServerTick = -1;
            int min_target = Mathf.Max(1, _server_min_buffer_ticks);
            int max_target = Mathf.Max(min_target, _server_max_buffer_ticks);
            _server_dynamic_target_buffer_ticks = Mathf.Clamp(
                _server_target_buffer_ticks,
                min_target,
                max_target);
            _server_target_increase_pending = false;
            _server_playout_initialized = false;
            _server_next_input_tick = -1;
            _server_missing_input_ticks = 0;
            _server_buffer_reset_count = 0;
            _server_rebuffering = false;
            _server_rebuffer_ticks = 0;
            _last_server_catchup_tick = -1;
            _has_server_arrival_delay_sample = false;
            _server_arrival_delay_mean = 0f;
            _server_arrival_delay_variance = 0f;
            _last_server_target_change_tick = -1;
            _has_last_received_server_input = false;
            _last_received_server_input = default;
            _last_resolved_server_tick = -1;
            _last_server_frame = default;
            _has_logged_server_source = false;
            _last_logged_server_source = ServerInputSource.None;
        }

        private void LogServerInputWarnings(
            PlayerInputData[] inputs,
            int previous_received_tick,
            int newest_received_tick,
            int last_accepted_tick,
            int accepted_count,
            int duplicate_count,
            int too_old_count,
            int too_new_count)
        {
            if (_character == null || _character.TickManager == null)
                return;

            int server_tick = _character.TickManager.CurrentTick;
            int first_tick = inputs[0].Tick;
            int last_tick = inputs[^1].Tick;
            int age_ticks = server_tick - first_tick;
            int gap_ticks = previous_received_tick < 0 ? 0 : first_tick - previous_received_tick;
            bool is_stale = last_tick <= previous_received_tick;
            bool should_log =
                age_ticks > _server_input_warning_age_ticks ||
                gap_ticks > _server_input_warning_gap_ticks ||
                inputs.Length > _server_input_warning_batch_size ||
                is_stale ||
                too_new_count > 0 ||
                accepted_count == 0;

            if (!should_log)
                return;

            double rtt_ms = connectionToClient == null ? -1d : connectionToClient.rtt * 1000d;
            MatchLogContext.Get(gameObject.scene)?.Write(
                "input",
                $"[InputWarning][Server] netId={netId} serverTick={server_tick} batch={first_tick}-{last_tick} " +
                $"count={inputs.Length} ageTicks={age_ticks} gapTicks={gap_ticks} stale={is_stale} " +
                $"previousReceived={previous_received_tick} newestReceived={newest_received_tick} " +
                $"lastAccepted={last_accepted_tick} accepted={accepted_count} duplicates={duplicate_count} " +
                $"tooOld={too_old_count} tooNew={too_new_count} " +
                $"bufferOldest={_character.InputBuffet.OldestTick} bufferNewest={_character.InputBuffet.NewestTick} " +
                $"bufferCount={_character.InputBuffet.Count} rttMs={rtt_ms:0.#}");
        }

        [TargetRpc]
        private void TargetRegisterInputsReceived(int tick)
        {
            if (tick <= LastReceivedByServerTick)
                return;

            LastReceivedByServerTick = tick;
            OnInputsReceivedByServer?.Invoke(tick);
        }

        private void DiscardStaleClientInputs(int current_tick)
        {
            int max_age_ticks = GetTicksFromMs(_max_buffered_input_age_ms);
            if (max_age_ticks <= 0)
                return;

            int discard_until_tick = current_tick - max_age_ticks;
            DiscardInputsUpTo(discard_until_tick);
        }

        private int GetTicksFromMs(float milliseconds)
        {
            if (_character == null || _character.TickManager == null)
                return 0;

            return Mathf.Max(
                0,
                Mathf.RoundToInt(Mathf.Max(0f, milliseconds) * 0.001f * _character.TickManager.TickRate));
        }
    }
}
