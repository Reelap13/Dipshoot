using Game.Players.Input;
using UnityEngine;

namespace Game.Players
{
    public static class MovementSimulation
    {
        public static PlayerState Simulate(
            PlayerState previous_state,
            PlayerInputData input,
            float delta_time,
            float speed,
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

            Vector3 wish_dir =
                previous_state.Rotation * new Vector3(input.Move.x, 0f, input.Move.y);

            previous_state.Velocity = wish_dir * speed;
            previous_state.Position += previous_state.Velocity * delta_time;
            previous_state.Tick = tick;

            return previous_state;
        }
    }
}
