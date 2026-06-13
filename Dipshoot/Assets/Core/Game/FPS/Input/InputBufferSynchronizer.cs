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
        [SerializeField] private int _max_input_batch_ticks = 24;
        [SerializeField] private float _max_buffered_input_age_ms = 1000f;
        [SerializeField] private float _server_max_input_age_ms = 1000f;
        [SerializeField] private int _server_input_warning_age_ticks = 20;
        [SerializeField] private int _server_input_warning_gap_ticks = 3;
        [SerializeField] private int _server_input_warning_batch_size = 32;

        public int LastCapturedTick { get; private set; } = -1;
        public int LastSentTick { get; private set; } = -1;
        public int LastAcknowledgedTick { get; private set; } = -1;
        public int LastReceivedByServerTick { get; private set; } = -1;

        public bool HasPendingInputs => LastCapturedTick > LastAcknowledgedTick;
        public bool HasUnsentInputs => LastCapturedTick > LastSentTick;

        public event Action<int> OnPendingInputsChanged;
        public event Action<int> OnInputsAcknowledged;
        public event Action<int> OnInputsReceivedByServer;

        private TickManager _registered_tick_manager;

        public TickLayer TickLayer => TickLayer.InputSend;
        public int TickOrder => 0;

        private void Awake()
        {
            CacheReferences();
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
            LastReceivedByServerTick = tick;
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
            int previous_received_tick = LastReceivedByServerTick;
            int newest_received_tick = LastReceivedByServerTick;
            int last_received_tick = LastReceivedByServerTick;
            int accepted_count = 0;
            int duplicate_count = 0;
            int too_old_count = 0;
            foreach (PlayerInputData input in inputs)
            {
                if (input.Tick <= LastReceivedByServerTick)
                {
                    duplicate_count++;
                    continue;
                }

                if (input.Tick > newest_received_tick)
                    newest_received_tick = input.Tick;

                if (_character.TickManager != null && input.Tick < earliest_allowed_tick)
                {
                    too_old_count++;
                    continue;
                }

                _character.InputBuffet.Add(input);
                accepted_count++;
                if (input.Tick > last_received_tick)
                    last_received_tick = input.Tick;
            }

            if (newest_received_tick > LastReceivedByServerTick)
                LastReceivedByServerTick = newest_received_tick;

            LogServerInputWarnings(
                inputs,
                previous_received_tick,
                newest_received_tick,
                last_received_tick,
                accepted_count,
                duplicate_count,
                too_old_count);

            TargetRegisterInputsReceived(newest_received_tick);
        }

        private void LogServerInputWarnings(
            PlayerInputData[] inputs,
            int previous_received_tick,
            int newest_received_tick,
            int last_accepted_tick,
            int accepted_count,
            int duplicate_count,
            int too_old_count)
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
                accepted_count == 0;

            if (!should_log)
                return;

            double rtt_ms = connectionToClient == null ? -1d : connectionToClient.rtt * 1000d;
            MatchLogContext.Get(gameObject.scene)?.Write(
                "input",
                $"[InputWarning][Server] netId={netId} serverTick={server_tick} batch={first_tick}-{last_tick} " +
                $"count={inputs.Length} ageTicks={age_ticks} gapTicks={gap_ticks} stale={is_stale} " +
                $"previousReceived={previous_received_tick} newestReceived={newest_received_tick} " +
                $"lastAccepted={last_accepted_tick} accepted={accepted_count} duplicates={duplicate_count} tooOld={too_old_count} " +
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
