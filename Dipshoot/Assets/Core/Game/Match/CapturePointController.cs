using Game.Players;
using Game.TickSystem;
using UnityEngine;

namespace Game.MatchMode
{
    public class CapturePointController : MonoBehaviour, ITickSystem
    {
        [SerializeField] private float _radius = 4f;
        [SerializeField] private float _capture_time = 6f;
        [SerializeField] private float _score_per_second = 1f;

        private TeamControlModeController _mode_controller;
        private PlayerSpawnController _spawn_controller;
        private TickManager _tick_manager;
        private TickManager _registered_tick_manager;
        private Transform _point;
        private bool _is_active;

        public TeamId OwnerTeam { get; private set; }
        public TeamId CapturingTeam { get; private set; }
        public float CaptureProgress { get; private set; }
        public bool IsContested { get; private set; }
        public int RedPlayersInside { get; private set; }
        public int BluePlayersInside { get; private set; }

        public TickLayer TickLayer => TickLayer.Objective;
        public int TickOrder => 0;

        public void Initialize(
            TeamControlModeController mode_controller,
            PlayerSpawnController spawn_controller,
            TickManager tick_manager,
            Transform point)
        {
            _mode_controller = mode_controller;
            _spawn_controller = spawn_controller;
            _tick_manager = tick_manager;
            _point = point == null ? transform : point;
            TryRegisterTickSystem();
        }

        private void OnDisable()
        {
            TryUnregisterTickSystem();
        }

        public void ResetRound()
        {
            OwnerTeam = TeamId.None;
            CapturingTeam = TeamId.None;
            CaptureProgress = 0f;
            IsContested = false;
            RedPlayersInside = 0;
            BluePlayersInside = 0;
            _mode_controller?.UpdateCaptureState(this);
        }

        public void StartObjective()
        {
            _is_active = true;
        }

        public void StopObjective()
        {
            _is_active = false;
        }

        public bool ShouldTick(GameTickContext context)
        {
            return _is_active && _tick_manager == context.TickManager && _spawn_controller != null;
        }

        public void Tick(GameTickContext context)
        {
            CountPlayersInside(context.DeltaTime);
            UpdateCapture(context.DeltaTime);
            AwardOwnerScore(context.DeltaTime);
            _mode_controller?.UpdateCaptureState(this);
        }

        private void CountPlayersInside(float delta_time)
        {
            RedPlayersInside = 0;
            BluePlayersInside = 0;

            foreach (PlayerCharacter character in _spawn_controller.ActiveCharacters)
            {
                if (character == null || character.Health == null || !character.Health.IsAlive)
                    continue;

                if ((character.transform.position - _point.position).sqrMagnitude > _radius * _radius)
                    continue;

                TeamId team_id = _spawn_controller.GetTeam(character);
                if (team_id == TeamId.Red)
                    RedPlayersInside++;
                else if (team_id == TeamId.Blue)
                    BluePlayersInside++;

                int player_id = _spawn_controller.GetPlayerId(character);
                _mode_controller?.RegisterCapturePresence(player_id, delta_time);
            }
        }

        private void UpdateCapture(float delta_time)
        {
            TeamId active_team = GetActiveCaptureTeam();
            IsContested = RedPlayersInside > 0 && BluePlayersInside > 0;

            if (active_team == TeamId.None)
                return;

            if (OwnerTeam == active_team)
            {
                CapturingTeam = TeamId.None;
                CaptureProgress = 1f;
                return;
            }

            if (CapturingTeam != active_team)
            {
                CapturingTeam = active_team;
                CaptureProgress = 0f;
            }

            CaptureProgress = Mathf.Clamp01(CaptureProgress + delta_time / _capture_time);
            if (CaptureProgress < 1f)
                return;

            OwnerTeam = active_team;
            CapturingTeam = TeamId.None;
        }

        private TeamId GetActiveCaptureTeam()
        {
            if (RedPlayersInside > 0 && BluePlayersInside == 0)
                return TeamId.Red;

            if (BluePlayersInside > 0 && RedPlayersInside == 0)
                return TeamId.Blue;

            return TeamId.None;
        }

        private void AwardOwnerScore(float delta_time)
        {
            if (OwnerTeam == TeamId.None)
                return;

            _mode_controller?.AddTeamScore(OwnerTeam, _score_per_second * delta_time);
        }

        private void TryRegisterTickSystem()
        {
            if (_registered_tick_manager != null || _tick_manager == null)
                return;

            _registered_tick_manager = _tick_manager;
            _registered_tick_manager.RegisterSystem(this);
        }

        private void TryUnregisterTickSystem()
        {
            if (_registered_tick_manager == null)
                return;

            _registered_tick_manager.UnregisterSystem(this);
            _registered_tick_manager = null;
        }
    }
}
