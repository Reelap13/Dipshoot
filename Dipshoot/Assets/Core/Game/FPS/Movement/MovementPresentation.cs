using UnityEngine;

namespace Game.Players
{
    public static class MovementPresentation
    {
        public static void ApplyState(Transform target, PlayerState state)
        {
            target.position = state.Position;
            target.rotation = state.Rotation;
        }
    }
}
