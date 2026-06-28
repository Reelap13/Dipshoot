using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class PlayerMovementAudioController : MonoBehaviour, IPlayerSimulationResettable
    {
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private FootstepAudioSet _footsteps;
        [SerializeField] private float _crouch_step_distance = 1.55f;
        [SerializeField] private float _walk_step_distance = 1.35f;
        [SerializeField] private float _run_step_distance = 1.05f;
        [SerializeField] private float _min_step_speed = 0.25f;
        [SerializeField] private float _run_speed_threshold = 5.8f;
        [SerializeField] private float _hard_land_velocity = 8f;

        private PlayerState _last_state;
        private bool _has_last_state;
        private float _step_distance;
        private Vector3 _last_transform_position;
        private bool _has_last_transform_position;

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            CacheReferences();

            if (_character == null ||
                !_character.isClient ||
                !_character.IsGameplayActive ||
                _character.Health != null && !_character.Health.IsAlive ||
                _footsteps == null)
            {
                return;
            }

            if (!_character.isOwned)
            {
                UpdateRemoteSteps();
                return;
            }

            if (!_character.IsClientSimulationInitialized ||
                !TryGetCurrentState(out PlayerState state))
            {
                return;
            }

            UpdateTransitions(state);
            UpdateSteps(state);

            _last_state = state;
            _has_last_state = true;
        }

        public void ResetSimulation()
        {
            _has_last_state = false;
            _has_last_transform_position = false;
            _step_distance = 0f;
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();
        }

        private bool TryGetCurrentState(out PlayerState state)
        {
            state = default;
            if (_character.TickManager == null)
                return false;

            return _character.StateBuffer.TryGetLastAtOrBefore(_character.TickManager.CurrentTick, out state);
        }

        private void UpdateTransitions(PlayerState state)
        {
            if (!_has_last_state)
                return;

            if (_last_state.IsGrounded && !state.IsGrounded && state.Velocity.y > 0.5f)
                Play(_footsteps.Jump);

            if (!_last_state.IsGrounded && state.IsGrounded)
            {
                AudioCue cue = _last_state.Velocity.y <= -_hard_land_velocity
                    ? _footsteps.LandHard
                    : _footsteps.LandSoft;
                Play(cue);
                _step_distance = 0f;
            }
        }

        private void UpdateSteps(PlayerState state)
        {
            if (!_has_last_state || !state.IsGrounded)
                return;

            Vector3 delta = state.Position - _last_state.Position;
            delta.y = 0f;

            float speed = new Vector2(state.Velocity.x, state.Velocity.z).magnitude;
            if (speed < _min_step_speed)
            {
                _step_distance = 0f;
                return;
            }

            _step_distance += delta.magnitude;
            float target_distance = GetStepDistance(state, speed);
            if (_step_distance < target_distance)
                return;

            _step_distance %= target_distance;
            Play(GetStepCue(state, speed));
        }

        private float GetStepDistance(PlayerState state, float speed)
        {
            if (state.Stance == MovementStance.Crouching)
                return _crouch_step_distance;

            return speed >= _run_speed_threshold
                ? _run_step_distance
                : _walk_step_distance;
        }

        private AudioCue GetStepCue(PlayerState state, float speed)
        {
            if (state.Stance == MovementStance.Crouching)
                return _footsteps.CrouchSteps;

            return speed >= _run_speed_threshold
                ? _footsteps.RunSteps
                : _footsteps.WalkSteps;
        }

        private void UpdateRemoteSteps()
        {
            Vector3 position = transform.position;
            if (!_has_last_transform_position)
            {
                _last_transform_position = position;
                _has_last_transform_position = true;
                return;
            }

            Vector3 delta = position - _last_transform_position;
            delta.y = 0f;
            _last_transform_position = position;

            float delta_time = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = delta.magnitude / delta_time;
            if (speed < _min_step_speed)
            {
                _step_distance = 0f;
                return;
            }

            _step_distance += delta.magnitude;
            float target_distance = speed >= _run_speed_threshold
                ? _run_step_distance
                : _walk_step_distance;
            if (_step_distance < target_distance)
                return;

            _step_distance %= target_distance;
            Play(speed >= _run_speed_threshold ? _footsteps.RunSteps : _footsteps.WalkSteps);
        }

        private void Play(AudioCue cue)
        {
            GameAudioService.Instance.Play(
                cue,
                transform.position,
                _character.isOwned,
                gameObject.GetInstanceID());
        }
    }
}
