using UnityEngine;

namespace Server.Scripts.TickSystem
{
    public interface ITickable
    {
        public int GetTick();
    }
}