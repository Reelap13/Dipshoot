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
            int tick)
        {
            Vector3 wish_dir =
                previous_state.Rotation * new Vector3(input.Move.x, 0f, input.Move.y);

            previous_state.Velocity = wish_dir * speed;
            previous_state.Position += previous_state.Velocity * delta_time;
            previous_state.Tick = tick;

            return previous_state;
        }
    }
}
