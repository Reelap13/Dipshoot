using Core.ClientPresentation;
using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Game.MatchMode
{
    public class MatchHudController : NetworkBehaviour
    {
        [SerializeField] private TeamControlModeController _mode_controller;
        [SerializeField] private MatchStatsController _stats_controller;
        [SerializeField] private float _scoreboard_sync_interval = 0.1f;
        [SerializeField] private int _max_synced_kill_events = 32;

        private readonly SyncDictionary<int, ScoreboardPlayerState> _scoreboard_players = new();
        private readonly SyncList<MatchKillEvent> _kill_feed_events = new();
        private readonly HashSet<int> _server_seen_player_ids = new();
        private readonly List<int> _server_removed_player_ids = new();
        private double _next_scoreboard_sync_at;
        private int _last_published_kill_id;

        public IReadOnlyDictionary<int, ScoreboardPlayerState> ScoreboardPlayers =>
            _scoreboard_players;
        public int ScoreboardRevision { get; private set; }
        public IReadOnlyList<MatchKillEvent> KillFeedEvents => _kill_feed_events;

        public event Action OnScoreboardChanged;
        public event Action<MatchKillEvent> OnKillFeedEvent;
        public event Action OnKillFeedCleared;

        public override void OnStartServer()
        {
            base.OnStartServer();
            CacheReferences();
            PublishScoreboard();
            PublishKillEvents();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            CacheReferences();
            _scoreboard_players.OnChange += HandleScoreboardChanged;
            _kill_feed_events.OnAdd += HandleKillFeedAdded;
            _kill_feed_events.OnClear += HandleKillFeedCleared;
            ScoreboardRevision++;
            ClientAppRoot.Instance.MatchStore.SetMatchControllers(_mode_controller, this);
        }

        public override void OnStopClient()
        {
            _scoreboard_players.OnChange -= HandleScoreboardChanged;
            _kill_feed_events.OnAdd -= HandleKillFeedAdded;
            _kill_feed_events.OnClear -= HandleKillFeedCleared;
            if (ClientAppRoot.HasInstance)
                ClientAppRoot.Instance.MatchStore.ClearMatchControllers(this);

            base.OnStopClient();
        }

        [ServerCallback]
        private void Update()
        {
            if (NetworkTime.time < _next_scoreboard_sync_at)
                return;

            _next_scoreboard_sync_at =
                NetworkTime.time + Mathf.Max(0.05f, _scoreboard_sync_interval);
            PublishScoreboard();
            PublishKillEvents();
        }

        private void CacheReferences()
        {
            if (_mode_controller == null)
                _mode_controller = GetComponent<TeamControlModeController>();

            if (_stats_controller == null)
                _stats_controller = GetComponent<MatchStatsController>();
        }

        [Server]
        private void PublishScoreboard()
        {
            if (_stats_controller == null)
                return;

            _server_seen_player_ids.Clear();
            foreach (PlayerRoundStats stats in _stats_controller.PlayerStats)
            {
                if (stats == null || stats.PlayerId < 0)
                    continue;

                ScoreboardPlayerState state = new()
                {
                    PlayerId = stats.PlayerId,
                    Nickname = string.IsNullOrWhiteSpace(stats.Nickname)
                        ? $"Player {stats.PlayerId}"
                        : stats.Nickname,
                    TeamId = stats.TeamId,
                    Kills = stats.Kills,
                    Deaths = stats.Deaths,
                    CapturePresenceSeconds = Mathf.Max(
                        0,
                        Mathf.FloorToInt(stats.CapturePresenceTime)),
                };

                _server_seen_player_ids.Add(stats.PlayerId);
                if (!_scoreboard_players.TryGetValue(stats.PlayerId, out ScoreboardPlayerState current) ||
                    !current.Equals(state))
                {
                    _scoreboard_players[stats.PlayerId] = state;
                }
            }

            _server_removed_player_ids.Clear();
            foreach (int player_id in _scoreboard_players.Keys)
            {
                if (!_server_seen_player_ids.Contains(player_id))
                    _server_removed_player_ids.Add(player_id);
            }

            for (int i = 0; i < _server_removed_player_ids.Count; i++)
                _scoreboard_players.Remove(_server_removed_player_ids[i]);
        }

        [Client]
        private void HandleScoreboardChanged(
            SyncDictionary<int, ScoreboardPlayerState>.Operation operation,
            int player_id,
            ScoreboardPlayerState state)
        {
            ScoreboardRevision++;
            OnScoreboardChanged?.Invoke();
        }

        [Server]
        private void PublishKillEvents()
        {
            if (_stats_controller == null)
                return;

            IReadOnlyList<MatchKillEvent> events = _stats_controller.KillEvents;
            if (_last_published_kill_id > 0 &&
                (events.Count == 0 || events[^1].KillId < _last_published_kill_id))
            {
                _kill_feed_events.Clear();
                _last_published_kill_id = 0;
            }

            for (int i = 0; i < events.Count; i++)
            {
                MatchKillEvent kill_event = events[i];
                if (kill_event.KillId <= _last_published_kill_id)
                    continue;

                _kill_feed_events.Add(kill_event);
                _last_published_kill_id = kill_event.KillId;
            }

            int max_events = Mathf.Max(1, _max_synced_kill_events);
            while (_kill_feed_events.Count > max_events)
                _kill_feed_events.RemoveAt(0);
        }

        [Client]
        private void HandleKillFeedAdded(int index)
        {
            if (index < 0 || index >= _kill_feed_events.Count)
                return;

            OnKillFeedEvent?.Invoke(_kill_feed_events[index]);
        }

        [Client]
        private void HandleKillFeedCleared()
        {
            OnKillFeedCleared?.Invoke();
        }
    }
}
