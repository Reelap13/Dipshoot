using UnityEngine;

namespace Game.Players
{
    public static class PlayerAimUtility
    {
        public static float GetEffectiveCameraPitch(PlayerState state)
        {
            return state.CameraPitch - state.RecoilPitch;
        }

        public static Quaternion GetEffectivePitchRotation(PlayerState state)
        {
            return Quaternion.Euler(GetEffectiveCameraPitch(state), state.RecoilYaw, 0f);
        }
    }
}
