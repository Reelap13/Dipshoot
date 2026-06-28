using Game.Players.Input;
using Game.TickSystem;
using UnityEngine;

namespace Game.Players
{
    public class AimController : PlayerCharacterComponent
    {
        [SerializeField] private float _yaw_sensitivity = 0.15f;
        [SerializeField] private float _pitch_sensitivity = 0.15f;
        [SerializeField] private float _min_camera_pitch = -80f;
        [SerializeField] private float _max_camera_pitch = 80f;
        [SerializeField] private WeaponController _weapon_controller;

        public override TickLayer TickLayer => TickLayer.AimSimulation;

        public override bool ShouldTick(GameTickContext context)
        {
            return base.ShouldTick(context) && IsAlive && IsGameplayActive && !IsServer && IsClient && IsOwned;
        }

        protected override void OnTick(GameTickContext context)
        {
            SimulateTick(context.Tick);
        }

        public bool SimulateTick(int tick)
        {
            if (!Character.InputBuffet.TryGet(tick, out PlayerInputData input))
                return false;

            if (!TryGetPreviousStateForTick(tick, out PlayerState previous_state))
                return false;

            PlayerState new_state = Simulate(previous_state, input, tick);
            Character.StateBuffer.Add(new_state);
            return true;
        }

        public bool SimulateServerResolvedTick(PlayerInputData input, int tick)
        {
            if (!TryGetPreviousStateForTick(tick, out PlayerState previous_state))
                return false;

            PlayerState new_state = Simulate(previous_state, input, tick);
            Character.StateBuffer.Add(new_state);
            return true;
        }

        public PlayerState Simulate(PlayerState previous_state, PlayerInputData input, int tick)
        {
            return AimSimulation.Simulate(
                previous_state,
                input,
                _yaw_sensitivity,
                _pitch_sensitivity,
                _min_camera_pitch,
                _max_camera_pitch,
                GetRecoilRecoveryPerTick(),
                tick);
        }

        public void ApplyRecoil(float pitch, float yaw)
        {
            int tick = Character.TickManager == null ? 0 : Character.TickManager.CurrentTick;
            if (!Character.StateBuffer.TryGetLastAtOrBefore(tick, out PlayerState state))
                return;

            float recoil_max = GetRecoilMax();
            state.RecoilPitch = recoil_max > 0f
                ? Mathf.Min(recoil_max, state.RecoilPitch + pitch)
                : state.RecoilPitch + pitch;
            state.RecoilYaw = recoil_max > 0f
                ? Mathf.Clamp(state.RecoilYaw + yaw, -recoil_max, recoil_max)
                : state.RecoilYaw + yaw;
            Character.StateBuffer.Add(state);
        }

        private bool TryGetPreviousStateForTick(int tick, out PlayerState previous_state)
        {
            if (Character.StateBuffer.TryGet(tick - 1, out previous_state))
                return true;

            if (Character.StateBuffer.TryGetLastAtOrBefore(tick - 1, out previous_state))
                return true;

            previous_state = default;
            return false;
        }

        public override void ResetSimulation()
        {
        }

        private float GetRecoilRecoveryPerTick()
        {
            if (_weapon_controller == null)
                _weapon_controller = GetComponent<WeaponController>();

            if (_weapon_controller == null || Character.TickManager == null)
                return 0f;

            return _weapon_controller.ActiveRecoilRecovery / Character.TickManager.TickRate;
        }

        private float GetRecoilMax()
        {
            if (_weapon_controller == null)
                _weapon_controller = GetComponent<WeaponController>();

            return _weapon_controller == null ? 0f : _weapon_controller.ActiveRecoilMax;
        }
    }
}
