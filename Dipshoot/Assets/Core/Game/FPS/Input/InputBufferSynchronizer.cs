using System;
using Mirror;
using UnityEngine;

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
    public class InputBufferSynchronizer : NetworkBehaviour
    {
        private const string LogPrefix = "[NetTick][InputSync]";

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerInputController _inputController;

        public int LastCapturedTick { get; private set; } = -1;
        public int LastSentTick { get; private set; } = -1;
        public int LastAcknowledgedTick { get; private set; } = -1;
        public int LastReceivedByServerTick { get; private set; } = -1;

        public bool HasPendingInputs => LastCapturedTick > LastAcknowledgedTick;
        public bool HasUnsentInputs => LastCapturedTick > LastSentTick;

        public event Action<int> OnPendingInputsChanged;
        public event Action<int> OnInputsAcknowledged;
        public event Action<int> OnInputsReceivedByServer;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();

            if (_inputController != null)
                _inputController.OnInputCaptured += HandleInputCaptured;
        }

        private void OnDisable()
        {
            if (_inputController != null)
                _inputController.OnInputCaptured -= HandleInputCaptured;
        }

        public bool TryBuildPendingBatch(out PlayerInputBatch batch)
        {
            batch = default;

            if (_character == null || !HasPendingInputs)
                return false;

            int from_tick = LastAcknowledgedTick + 1;
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

            if (_character == null || LastCapturedTick <= LastSentTick)
                return false;

            int from_tick = Mathf.Max(LastAcknowledgedTick + 1, LastSentTick + 1);
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
            Debug.Log(
                $"{LogPrefix} Captured input. netId={netId} localTick={input.Tick} " +
                $"move={input.Move} look={input.Look} shoot={input.IsShoot}");
            OnPendingInputsChanged?.Invoke(input.Tick);
            TrySendUnsentInputs();
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_inputController == null)
                _inputController = GetComponent<PlayerInputController>();
        }

        public bool TrySendUnsentInputs()
        {
            if (!isOwned)
                return false;

            if (!TryBuildUnsentBatch(out PlayerInputBatch batch))
            {
                Debug.Log(
                    $"{LogPrefix} No unsent inputs. netId={netId} " +
                    $"captured={LastCapturedTick} sent={LastSentTick} acked={LastAcknowledgedTick}");
                return false;
            }

            Debug.Log(
                $"{LogPrefix} Sending batch. netId={netId} from={batch.FromTick} " +
                $"to={batch.ToTick} count={batch.Inputs.Length}");
            CmdSendInputs(batch.Inputs);
            MarkInputsAsSentUpTo(batch.ToTick);
            return true;
        }

        [Command]
        private void CmdSendInputs(PlayerInputData[] inputs)
        {
            if (_character == null || inputs == null || inputs.Length == 0)
            {
                Debug.Log(
                    $"{LogPrefix} Server received empty batch. netId={netId} " +
                    $"characterMissing={_character == null}");
                return;
            }

            int last_received_tick = LastReceivedByServerTick;
            foreach (PlayerInputData input in inputs)
            {
                _character.InputBuffet.Add(input);
                if (input.Tick > last_received_tick)
                    last_received_tick = input.Tick;
            }

            Debug.Log(
                $"{LogPrefix} Server stored batch. netId={netId} count={inputs.Length} " +
                $"lastReceivedInputTick={last_received_tick}");
            TargetRegisterInputsReceived(last_received_tick);
        }

        [TargetRpc]
        private void TargetRegisterInputsReceived(int tick)
        {
            if (tick <= LastReceivedByServerTick)
                return;

            LastReceivedByServerTick = tick;
            Debug.Log(
                $"{LogPrefix} Server receipt ack. netId={netId} receivedUpTo={tick}");
            OnInputsReceivedByServer?.Invoke(tick);
        }
    }
}
