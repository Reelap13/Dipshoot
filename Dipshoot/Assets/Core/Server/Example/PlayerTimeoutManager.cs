using System.Collections.Generic;
using Server.Data;
using Server.ServerSide;
using UnityEngine;

namespace Server.GameExamples
{
    public class PlayerTimeoutManager : MonoBehaviour
    {
        [SerializeField] private float _time_to_delete_disconnected_player = 4f;

        private Dictionary<Player, float> _disconnection_times = new();

        private void Awake()
        {
            PlayersController.Instance.OnDisconnected.AddListener(StartTimer);
            PlayersController.Instance.OnReconnected.AddListener(EndTimer);
        }

        private void Update()
        {
            List<Player> deleted_players = new();
            foreach (var timer in _disconnection_times)
                if (Time.time - timer.Value > _time_to_delete_disconnected_player)
                    deleted_players.Add(timer.Key);

            foreach (var player in deleted_players)
                DeletePlayer(player);
        }

        private void StartTimer(Player player)
        {
            _disconnection_times[player] = Time.time;
        }

        private void EndTimer(Player player)
        {
            _disconnection_times.Remove(player);
        }

        private void DeletePlayer(Player player)
        {
            EndTimer(player);
            PlayersController.Instance.DeletePlayer(player);
        }
    }
}
