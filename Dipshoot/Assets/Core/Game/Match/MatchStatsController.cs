using System.Collections.Generic;
using Game.Players;
using UnityEngine;

namespace Game.MatchMode
{
    public class MatchStatsController : MonoBehaviour
    {
        private readonly Dictionary<int, PlayerRoundStats> _player_stats = new();
        private readonly List<MatchKillEvent> _kill_events = new();
        private int _next_kill_id = 1;

        public IEnumerable<PlayerRoundStats> PlayerStats => _player_stats.Values;
        public IReadOnlyList<MatchKillEvent> KillEvents => _kill_events;
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
            _kill_events.Clear();
            _next_kill_id = 1;
        }

        public void ResetRoundScores()
        {
            RedScore = 0f;
            BlueScore = 0f;
        }

        public void RegisterPlayer(int player_id, TeamId team_id, string nickname = null)
        {
            if (!_player_stats.TryGetValue(player_id, out PlayerRoundStats stats))
            {
                stats = new PlayerRoundStats();
                _player_stats.Add(player_id, stats);
            }

            stats.PlayerId = player_id;
            stats.TeamId = team_id;
            if (!string.IsNullOrWhiteSpace(nickname))
                stats.Nickname = nickname;
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
            RegisterDeath(
                killer_player_id,
                victim_player_id,
                WeaponSlot.None,
                PlayerHitboxType.None);
        }

        public void RegisterDeath(
            int killer_player_id,
            int victim_player_id,
            WeaponSlot weapon_slot,
            PlayerHitboxType hitbox_type)
        {
            _player_stats.TryGetValue(killer_player_id, out PlayerRoundStats killer_stats);
            _player_stats.TryGetValue(victim_player_id, out PlayerRoundStats victim_stats);

            if (killer_player_id != victim_player_id &&
                killer_stats != null)
            {
                killer_stats.Kills++;
            }

            if (victim_stats != null)
                victim_stats.Deaths++;

            MatchKillWeapon weapon = MatchKillEvent.GetWeapon(weapon_slot);
            if (killer_stats == null ||
                victim_stats == null ||
                killer_player_id == victim_player_id ||
                weapon == MatchKillWeapon.None)
            {
                return;
            }

            _kill_events.Add(new MatchKillEvent
            {
                KillId = _next_kill_id++,
                Killer = CreatePlayerInfo(killer_stats),
                Victim = CreatePlayerInfo(victim_stats),
                Weapon = weapon,
                Tags = MatchKillEvent.GetTags(hitbox_type),
            });
        }

        public void RegisterCapturePresence(int player_id, float delta_time)
        {
            if (!_player_stats.TryGetValue(player_id, out PlayerRoundStats stats))
                return;

            stats.CapturePresenceTime += delta_time;
        }

        private static MatchKillPlayerInfo CreatePlayerInfo(PlayerRoundStats stats)
        {
            return new MatchKillPlayerInfo
            {
                PlayerId = stats.PlayerId,
                Nickname = string.IsNullOrWhiteSpace(stats.Nickname)
                    ? $"Player {stats.PlayerId}"
                    : stats.Nickname,
                TeamId = stats.TeamId,
            };
        }
    }
}
