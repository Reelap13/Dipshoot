using UnityEngine;

namespace Game.Players.Input
{
    [System.Serializable]
    public class InputData
    {
        public int Tick;

        public Vector2 Move;
        public Vector2 Look;
        public bool IsShoot;
    }
}