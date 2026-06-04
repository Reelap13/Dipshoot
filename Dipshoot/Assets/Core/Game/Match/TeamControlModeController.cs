using System.Collections;
using Mirror;
using UnityEngine;

namespace Game.MatchMode
{
    public class TeamControlModeController : NetworkBehaviour
    {
        private const string LogPrefix = "[MatchMode]";

        [SerializeField] private DipshootGameController _game_controller;
        [SerializeField] private PlayerSpawnController _spawn_controller;
        [SerializeField] private CapturePointController _capture_point_controller;
        [SerializeField] private MatchStatsController _stats_controller;
        [SerializeField] private int _rounds_count = 3;
        [SerializeField] private float _map_intro_duration = 1.5f;
        [SerializeField] private float _round_results_duration = 4f;
        [SerializeField] private float _round_duration = 180f;
        [SerializeField] private float _score_limit = 100f;

        [SyncVar] private RoundPhase _phase = RoundPhase.None;
        [SyncVar] private int _current_round;
        [SyncVar] private float _phase_time_remaining;
        [SyncVar] private int _red_score;
        [SyncVar] private int _blue_score;
        [SyncVar] private int _red_round_wins;
        [SyncVar] private int _blue_round_wins;
        [SyncVar] private TeamId _capture_owner;
        [SyncVar] private TeamId _capturing_team;
        [SyncVar] private TeamId _last_round_winner;
        [SyncVar] private float _capture_progress;
        [SyncVar] private bool _is_capture_contested;
        [SyncVar] private int _red_players_inside;
        [SyncVar] private int _blue_players_inside;

        private readonly TeamRoster _team_roster = new();
        private Coroutine _match_routine;

        public RoundPhase Phase => _phase;
        public int CurrentRound => _current_round;
        public int RoundsCount => _rounds_count;
        public float PhaseTimeRemaining => _phase_time_remaining;
        public int RedScore => _red_score;
        public int BlueScore => _blue_score;
        public int RedRoundWins => _red_round_wins;
        public int BlueRoundWins => _blue_round_wins;
        public TeamId CaptureOwner => _capture_owner;
        public TeamId CapturingTeam => _capturing_team;
        public TeamId LastRoundWinner => _last_round_winner;
        public float CaptureProgress => _capture_progress;
        public bool IsCaptureContested => _is_capture_contested;
        public int RedPlayersInside => _red_players_inside;
        public int BluePlayersInside => _blue_players_inside;
        public bool IsRoundPlaying => _phase == RoundPhase.Playing;
        public bool IsGameplayActive => _phase == RoundPhase.Playing;
        public TeamId MatchWinner => GetMatchWinner();

        public void Initialize(DipshootGameController game_controller)
        {
            _game_controller = game_controller;
            CacheReferences();

            if (!isServer)
                return;

            _rounds_count = Mathf.Max(1, _game_controller.MatchController.MatchData.LobbyData.SelectedRoundsCount);
            _stats_controller.ResetMatch();
            _team_roster.AssignLobbyTeams(_game_controller.MatchController.MatchData.LobbyData.Players);
            _spawn_controller.Initialize(_game_controller, _team_roster, _stats_controller);
            _capture_point_controller.Initialize(
                this,
                _spawn_controller,
                _game_controller.TickManager,
                _game_controller.LevelCreator.Level.CapturePoint);
        }

        public void BeginMatch()
        {
            if (!isServer || _match_routine != null)
                return;

            _match_routine = StartCoroutine(RunMatch());
        }

        public void AddTeamScore(TeamId team_id, float amount)
        {
            if (!isServer || _phase != RoundPhase.Playing)
                return;

            _stats_controller.AddTeamScore(team_id, amount);
            SyncScoreState();
        }

        public void RegisterCapturePresence(int player_id, float delta_time)
        {
            if (!isServer || player_id < 0)
                return;

            _stats_controller.RegisterCapturePresence(player_id, delta_time);
        }

        public void UpdateCaptureState(CapturePointController capture_point)
        {
            if (!isServer || capture_point == null)
                return;

            _capture_owner = capture_point.OwnerTeam;
            _capturing_team = capture_point.CapturingTeam;
            _capture_progress = capture_point.CaptureProgress;
            _is_capture_contested = capture_point.IsContested;
            _red_players_inside = capture_point.RedPlayersInside;
            _blue_players_inside = capture_point.BluePlayersInside;
        }

        private void CacheReferences()
        {
            if (_game_controller == null)
                _game_controller = GetComponent<DipshootGameController>();

            if (_spawn_controller == null)
                _spawn_controller = GetComponent<PlayerSpawnController>();

            if (_capture_point_controller == null)
                _capture_point_controller = GetComponent<CapturePointController>();

            if (_stats_controller == null)
                _stats_controller = GetComponent<MatchStatsController>();
        }

        private IEnumerator RunMatch()
        {
            for (int round = 1; round <= _rounds_count; round++)
                yield return RunRound(round);

            SetPhase(RoundPhase.Finished, 0f);
            _spawn_controller.StopRespawns();
            _capture_point_controller.StopObjective();
            _game_controller.FinishGame();
            Debug.Log($"{LogPrefix} Match finished. redWins={_red_round_wins} blueWins={_blue_round_wins}");
        }

        private IEnumerator RunRound(int round)
        {
            _current_round = round;
            _last_round_winner = TeamId.None;

            SetPhase(RoundPhase.GeneratingMap, 0f);
            yield return null;

            SetPhase(RoundPhase.SpawningPlayers, 0f);
            _stats_controller.ResetRoundScores();
            SyncScoreState();
            _spawn_controller.StopRespawns();
            _spawn_controller.SpawnRoundPlayers();
            _capture_point_controller.ResetRound();

            yield return RunTimedPhase(RoundPhase.Intro, _map_intro_duration);

            SetPhase(RoundPhase.Playing, _round_duration);
            _spawn_controller.IsRespawnEnabled = true;
            _capture_point_controller.StartObjective();
            _game_controller.StartGame();

            while (_phase_time_remaining > 0f && !IsScoreLimitReached())
            {
                _phase_time_remaining = Mathf.Max(0f, _phase_time_remaining - Time.deltaTime);
                yield return null;
            }

            _spawn_controller.StopRespawns();
            _capture_point_controller.StopObjective();
            RegisterRoundWinner();

            yield return RunTimedPhase(RoundPhase.Ending, _round_results_duration);
        }

        private IEnumerator RunTimedPhase(RoundPhase phase, float duration)
        {
            SetPhase(phase, duration);

            while (_phase_time_remaining > 0f)
            {
                _phase_time_remaining = Mathf.Max(0f, _phase_time_remaining - Time.deltaTime);
                yield return null;
            }
        }

        private void RegisterRoundWinner()
        {
            TeamId winner = GetCurrentRoundWinner();
            _last_round_winner = winner;
            _stats_controller.RegisterRoundWin(winner);
            _red_round_wins = _stats_controller.RedRoundWins;
            _blue_round_wins = _stats_controller.BlueRoundWins;

            Debug.Log(
                $"{LogPrefix} Round finished. round={_current_round} winner={winner} " +
                $"redScore={_red_score} blueScore={_blue_score}");
        }

        private TeamId GetCurrentRoundWinner()
        {
            if (_red_score > _blue_score)
                return TeamId.Red;

            if (_blue_score > _red_score)
                return TeamId.Blue;

            return TeamId.None;
        }

        private TeamId GetMatchWinner()
        {
            if (_red_round_wins > _blue_round_wins)
                return TeamId.Red;

            if (_blue_round_wins > _red_round_wins)
                return TeamId.Blue;

            return TeamId.None;
        }

        private bool IsScoreLimitReached()
        {
            return _red_score >= _score_limit || _blue_score >= _score_limit;
        }

        private void SyncScoreState()
        {
            _red_score = Mathf.FloorToInt(_stats_controller.RedScore);
            _blue_score = Mathf.FloorToInt(_stats_controller.BlueScore);
            _red_round_wins = _stats_controller.RedRoundWins;
            _blue_round_wins = _stats_controller.BlueRoundWins;
        }

        private void SetPhase(RoundPhase phase, float duration)
        {
            _phase = phase;
            _phase_time_remaining = duration;
            Debug.Log($"{LogPrefix} Phase={phase} round={_current_round} time={duration:0.0}");
        }
    }
}
