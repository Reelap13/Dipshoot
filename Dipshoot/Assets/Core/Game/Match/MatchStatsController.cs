using System.Collections.Generic;
using UnityEngine;

namespace Game.MatchMode
{
    public class MatchStatsController : MonoBehaviour
    {
        private readonly Dictionary<int, PlayerRoundStats> _player_stats = new();

        public float RedScore { get; private set; }
        public float BlueScore { get; private set; }
        public int RedRoundWins { get; private set; }
        public int BlueRoundWins { get; private set; }

        public void ResetMatch()
        {
            RedScore = 0f;
            BlueScore = 0f;
            RedRoundWins = 0;
            BlueRoundWins = 0;
            _player_stats.Clear();
        }

        public void ResetRoundScores()
        {
            RedScore = 0f;
            BlueScore = 0f;

            foreach (PlayerRoundStats stats in _player_stats.Values)
                stats.ResetRound();
        }

        public void RegisterPlayer(int player_id, TeamId team_id)
        {
            if (!_player_stats.TryGetValue(player_id, out PlayerRoundStats stats))
            {
                stats = new PlayerRoundStats();
                _player_stats.Add(player_id, stats);
            }

            stats.PlayerId = player_id;
            stats.TeamId = team_id;
        }

        public void AddTeamScore(TeamId team_id, float amount)
        {
            if (amount <= 0f)
                return;

            if (team_id == TeamId.Red)
                RedScore += amount;
            else if (team_id == TeamId.Blue)
                BlueScore += amount;
        }

        public void RegisterRoundWin(TeamId team_id)
        {
            if (team_id == TeamId.Red)
                RedRoundWins++;
            else if (team_id == TeamId.Blue)
                BlueRoundWins++;
        }

        public void RegisterDamage(int attacker_player_id, int victim_player_id, int damage)
        {
            if (damage <= 0)
                return;

            if (_player_stats.TryGetValue(attacker_player_id, out PlayerRoundStats attacker_stats))
                attacker_stats.DamageDealt += damage;

            if (_player_stats.TryGetValue(victim_player_id, out PlayerRoundStats victim_stats))
                victim_stats.DamageTaken += damage;
        }

        public void RegisterDeath(int killer_player_id, int victim_player_id)
        {
            if (_player_stats.TryGetValue(killer_player_id, out PlayerRoundStats killer_stats))
                killer_stats.Kills++;

            if (_player_stats.TryGetValue(victim_player_id, out PlayerRoundStats victim_stats))
                victim_stats.Deaths++;
        }

        public void RegisterCapturePresence(int player_id, float delta_time)
        {
            if (!_player_stats.TryGetValue(player_id, out PlayerRoundStats stats))
                return;

            stats.CapturePresenceTime += delta_time;
        }
    }
}
