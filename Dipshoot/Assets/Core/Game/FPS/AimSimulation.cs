using Game.Players.Input;
using UnityEngine;

namespace Game.Players
{
    public static class AimSimulation
    {
        public static PlayerState Simulate(
            PlayerState previous_state,
            PlayerInputData input,
            float yaw_sensitivity,
            float pitch_sensitivity,
            float min_camera_pitch,
            float max_camera_pitch,
            int tick)
        {
            float yaw = previous_state.Rotation.eulerAngles.y + input.Look.x * yaw_sensitivity;
            previous_state.Rotation = Quaternion.Euler(0f, yaw, 0f);
            previous_state.CameraPitch = Mathf.Clamp(
                previous_state.CameraPitch - input.Look.y * pitch_sensitivity,
                min_camera_pitch,
                max_camera_pitch);
            previous_state.Tick = tick;

            return previous_state;
        }
    }
}
