using UnityEngine;

namespace Server.Lobby
{
    public abstract class GameController : MonoBehaviour
    {
        public abstract void Initialize(LobbyData data);

    }
}