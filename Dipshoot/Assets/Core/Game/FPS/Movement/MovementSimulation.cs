using Game.Players.Input;
using UnityEngine;

namespace Game.Players
{
    public readonly struct MovementSettings
    {
        public readonly float WalkSpeed;
        public readonly float SprintSpeed;
        public readonly float CrouchSpeed;
        public readonly float GroundAcceleration;
        public readonly float AirAcceleration;
        public readonly float GroundFriction;
        public readonly float AirControl;
        public readonly float Gravity;
        public readonly float JumpSpeed;
        public readonly float CoyoteTime;
        public readonly float JumpBufferTime;
        public readonly float StandHeight;
        public readonly float CrouchHeight;
        public readonly float CapsuleRadius;
        public readonly float SlopeLimit;
        public readonly float StepHeight;
        public readonly float SkinWidth;
        public readonly float SprintForwardDot;
        public readonly float SprintSideMultiplier;
        public readonly float SprintBackMultiplier;

        public MovementSettings(
            float walk_speed,
            float sprint_speed,
            float crouch_speed,
            float ground_acceleration,
            float air_acceleration,
            float ground_friction,
            float air_control,
            float gravity,
            float jump_speed,
            float coyote_time,
            float jump_buffer_time,
            float stand_height,
            float crouch_height,
            float capsule_radius,
            float slope_limit,
            float step_height,
            float skin_width,
            float sprint_forward_dot,
            float sprint_side_multiplier,
            float sprint_back_multiplier)
        {
            WalkSpeed = walk_speed;
            SprintSpeed = sprint_speed;
            CrouchSpeed = crouch_speed;
            GroundAcceleration = ground_acceleration;
            AirAcceleration = air_acceleration;
            GroundFriction = ground_friction;
            AirControl = air_control;
            Gravity = gravity;
            JumpSpeed = jump_speed;
            CoyoteTime = coyote_time;
            JumpBufferTime = jump_buffer_time;
            StandHeight = stand_height;
            CrouchHeight = crouch_height;
            CapsuleRadius = capsule_radius;
            SlopeLimit = slope_limit;
            StepHeight = step_height;
            SkinWidth = skin_width;
            SprintForwardDot = sprint_forward_dot;
            SprintSideMultiplier = sprint_side_multiplier;
            SprintBackMultiplier = sprint_back_multiplier;
        }
    }

    public static class MovementSimulation
    {
        public static PlayerState Simulate(
            PlayerState previous_state,
            PlayerInputData input,
            float delta_time,
            MovementSettings settings,
            int tick)
        {
            PlayerState state = previous_state;
            state.Tick = tick;
            state.Stance = input.IsCrouchHeld
                ? MovementStance.Crouching
                : MovementStance.Standing;

            state.TimeSinceGrounded = previous_state.IsGrounded
                ? 0f
                : previous_state.TimeSinceGrounded + delta_time;

            state.TimeSinceJumpPressed = input.IsJumpPressed
                ? 0f
                : previous_state.TimeSinceJumpPressed + delta_time;

            Vector3 horizontal_velocity = new(previous_state.Velocity.x, 0f, previous_state.Velocity.z);
            Vector3 target_velocity = GetTargetVelocity(previous_state.Rotation, input, state.Stance, settings);

            if (previous_state.IsGrounded)
            {
                horizontal_velocity = MoveGroundVelocity(
                    horizontal_velocity,
                    target_velocity,
                    delta_time,
                    settings);
            }
            else
            {
                horizontal_velocity = Vector3.MoveTowards(
                    horizontal_velocity,
                    target_velocity,
                    settings.AirAcceleration * settings.AirControl * delta_time);
            }

            float vertical_velocity = previous_state.Velocity.y;
            bool can_jump = state.TimeSinceJumpPressed <= settings.JumpBufferTime &&
                            state.TimeSinceGrounded <= settings.CoyoteTime;

            if (can_jump)
            {
                vertical_velocity = settings.JumpSpeed;
                state.IsGrounded = false;
                state.TimeSinceJumpPressed = settings.JumpBufferTime + delta_time;
                state.TimeSinceGrounded = settings.CoyoteTime + delta_time;
            }
            else if (previous_state.IsGrounded && vertical_velocity <= 0f)
            {
                vertical_velocity = 0f;
            }
            else
            {
                vertical_velocity -= settings.Gravity * delta_time;
            }

            state.Velocity = new Vector3(horizontal_velocity.x, vertical_velocity, horizontal_velocity.z);
            state.Position += state.Velocity * delta_time;

            return state;
        }

        public static void ApplyLandingGroundControl(
            ref PlayerState state,
            PlayerInputData input,
            float delta_time,
            MovementSettings settings)
        {
            Vector3 horizontal_velocity = new(state.Velocity.x, 0f, state.Velocity.z);
            Vector3 target_velocity = GetTargetVelocity(state.Rotation, input, state.Stance, settings);
            horizontal_velocity = MoveGroundVelocity(
                horizontal_velocity,
                target_velocity,
                delta_time,
                settings);

            state.Velocity = new Vector3(horizontal_velocity.x, state.Velocity.y, horizontal_velocity.z);
        }

        private static Vector3 GetTargetVelocity(
            Quaternion rotation,
            PlayerInputData input,
            MovementStance stance,
            MovementSettings settings)
        {
            Vector2 local_move = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 wish_dir = rotation * new Vector3(local_move.x, 0f, local_move.y);
            if (wish_dir.sqrMagnitude > 0f)
                wish_dir.Normalize();

            return wish_dir * ResolveTargetSpeed(local_move, input, stance, settings);
        }

        private static Vector3 MoveGroundVelocity(
            Vector3 horizontal_velocity,
            Vector3 target_velocity,
            float delta_time,
            MovementSettings settings)
        {
            horizontal_velocity = Vector3.MoveTowards(
                horizontal_velocity,
                Vector3.zero,
                settings.GroundFriction * delta_time);

            return Vector3.MoveTowards(
                horizontal_velocity,
                target_velocity,
                settings.GroundAcceleration * delta_time);
        }

        private static float ResolveTargetSpeed(
            Vector2 local_move,
            PlayerInputData input,
            MovementStance stance,
            MovementSettings settings)
        {
            if (local_move.sqrMagnitude <= 0f)
                return 0f;

            if (stance == MovementStance.Crouching)
                return settings.CrouchSpeed;

            if (!input.IsSprintHeld)
                return settings.WalkSpeed;

            float forward_amount = local_move.normalized.y;
            if (forward_amount < 0f)
                return settings.WalkSpeed * settings.SprintBackMultiplier;

            if (forward_amount < settings.SprintForwardDot)
                return settings.SprintSpeed * settings.SprintSideMultiplier;

            return settings.SprintSpeed;
        }
    }
}
