using System;
using Game.TickSystem;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class PlayerAnimationController : MonoBehaviour, ITickSystem, IPlayerSimulationResettable
    {
        private const float SprintSpeed01Threshold = 0.72f;
        private const float ReferenceRunSpeed = 7f;

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerVisualController _visual;
        [SerializeField] private WeaponController _weapon_controller;
        [SerializeField] private StateSynchronizer _state_synchronizer;
        [SerializeField] private Transform _skeleton_root;
        [SerializeField] private Animator _third_person_animator;
        [SerializeField] private bool _server_updates_animator = true;
        [SerializeField] private bool _client_updates_parameters = true;
        [SerializeField] private string _move_x_parameter = "MoveX";
        [SerializeField] private string _move_y_parameter = "MoveY";
        [SerializeField] private string _speed_parameter = "Speed";
        [SerializeField] private string _grounded_parameter = "Grounded";
        [SerializeField] private string _crouch_parameter = "Crouch";
        [SerializeField] private string _sprint_parameter = "Sprint";
        [SerializeField] private string _falling_parameter = "Falling";
        [SerializeField] private string _weapon_slot_parameter = "WeaponSlot";
        [SerializeField] private string _fire_parameter = "Fire";
        [SerializeField] private string _aim_pitch_parameter = "AimPitch";
        [SerializeField] private string _neo_forward_parameter = "Forward";
        [SerializeField] private string _neo_turn_parameter = "Turn";
        [SerializeField] private string _neo_grounded_parameter = "OnGround";
        [SerializeField] private string _neo_jump_parameter = "Jump";
        [SerializeField] private string _neo_jump_leg_parameter = "JumpLeg";
        [SerializeField] private string _template_forward_parameter = "forward";
        [SerializeField] private string _template_strafe_parameter = "strafe";
        [SerializeField] private string _template_locomotion_multiplier_parameter = "locomotionMultiplier";
        [SerializeField] private string _template_character_height_parameter = "characterHeight";
        [SerializeField] private string _template_jump_parameter = "jump";
        [SerializeField] private string _template_airborne_parameter = "airborne";
        [SerializeField] private string _template_sprinting_parameter = "sprinting";
        [SerializeField] private string _template_dead_parameter = "dead";
        [SerializeField] private string _kinemation_velocity_parameter = "Velocity";
        [SerializeField] private string _kinemation_moving_parameter = "Moving";
        [SerializeField] private string _kinemation_crouching_parameter = "Crouching";
        [SerializeField] private string _kinemation_in_air_parameter = "InAir";
        [SerializeField] private string _kinemation_sprinting_parameter = "Sprinting";
        [SerializeField] private string _kinemation_crouch_weight_parameter = "CrouchWeight";
        [SerializeField] private string _kinemation_sprint_pose_weight_parameter = "SprintPoseWeight";
        [SerializeField] private string _kinemation_full_body_weight_parameter = "FullBodyWeight";
        [SerializeField] private string _kinemation_proning_parameter = "Proning";
        [SerializeField] private string _kinemation_prone_weight_parameter = "ProneWeight";

        private TickManager _registered_tick_manager;
        private AnimatorParameterCache _parameter_cache;
        private int _last_fire_sequence;
        private bool _has_fire_sequence;
        private bool _was_grounded = true;

        public TickLayer TickLayer => TickLayer.AnimationPose;
        public int TickOrder => 0;

        private void Awake()
        {
            CacheReferences();
            ConfigureAnimator();
        }

        private void OnEnable()
        {
            CacheReferences();
            TryRegisterTickSystem();
        }

        private void OnDisable()
        {
            TryUnregisterTickSystem();
        }

        private void Update()
        {
            CacheReferences();
            ConfigureAnimator();
            TryRegisterTickSystem();

            if (!_client_updates_parameters || _character == null || !_character.isClient)
                return;

            ApplyCurrentState(false, Time.deltaTime);
        }

        public bool ShouldTick(GameTickContext context)
        {
            return _server_updates_animator &&
                _character != null &&
                _character.isServer &&
                (_character.Health == null || _character.Health.IsAlive) &&
                _character.TickManager == context.TickManager;
        }

        public void Tick(GameTickContext context)
        {
            ApplyCurrentState(true, context.DeltaTime);
        }

        public void ResetSimulation()
        {
            _last_fire_sequence = 0;
            _has_fire_sequence = false;
            _was_grounded = true;
        }

        public void PlayShot(ShotResult result)
        {
            CacheReferences();
            ConfigureAnimator();
            if (_third_person_animator == null)
                return;

            _last_fire_sequence = Mathf.Max(_last_fire_sequence + 1, result.ServerTick);
            _has_fire_sequence = true;
            TriggerIfExists(_fire_parameter);
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_visual == null)
                _visual = GetComponent<PlayerVisualController>();

            if (_weapon_controller == null)
                _weapon_controller = GetComponent<WeaponController>();

            if (_state_synchronizer == null)
                _state_synchronizer = GetComponent<StateSynchronizer>();

            Transform current_skeleton_root = _visual == null
                ? null
                : FindChildRecursive(_visual.ThirdPersonRoot, "ThirdPersonCharacter");
            if (current_skeleton_root != null && current_skeleton_root != _skeleton_root)
            {
                _skeleton_root = current_skeleton_root;
                _third_person_animator = null;
                _parameter_cache = default;
            }

            if (_skeleton_root == null)
                _skeleton_root = FindChildRecursive(transform, "ThirdPersonCharacter");

            if (_third_person_animator == null && _skeleton_root != null)
                _third_person_animator = _skeleton_root.GetComponentInChildren<Animator>(true);
        }

        private void ConfigureAnimator()
        {
            if (_third_person_animator == null)
                return;

            _third_person_animator.applyRootMotion = false;
            _third_person_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (_third_person_animator.runtimeAnimatorController == null)
            {
                _parameter_cache = default;
                return;
            }

            for (int i = 0; i < _third_person_animator.layerCount; i++)
                _third_person_animator.SetLayerWeight(i, 1f);

            if (_parameter_cache.Animator != _third_person_animator)
                _parameter_cache = new AnimatorParameterCache(_third_person_animator);
        }

        private void TryRegisterTickSystem()
        {
            if (_registered_tick_manager != null || _character == null || _character.TickManager == null)
                return;

            _registered_tick_manager = _character.TickManager;
            _registered_tick_manager.RegisterSystem(this);
        }

        private void TryUnregisterTickSystem()
        {
            if (_registered_tick_manager == null)
                return;

            _registered_tick_manager.UnregisterSystem(this);
            _registered_tick_manager = null;
        }

        private void ApplyCurrentState(bool update_animator, float delta_time)
        {
            if (_third_person_animator == null)
                return;

            PlayerAnimationState animation_state = BuildAnimationState();
            ApplyParameters(animation_state);

            if (update_animator && delta_time > 0f)
                _third_person_animator.Update(delta_time);
        }

        private PlayerAnimationState BuildAnimationState()
        {
            PlayerState state = GetLatestState();
            Vector3 local_velocity = Quaternion.Inverse(state.Rotation) * state.Velocity;
            Vector2 move = new(local_velocity.x, local_velocity.z);
            float speed = move.magnitude;
            float speed01 = Mathf.Clamp01(speed / ReferenceRunSpeed);

            return new PlayerAnimationState
            {
                Move = speed <= 0.0001f ? Vector2.zero : Vector2.ClampMagnitude(move / ReferenceRunSpeed, 1f),
                Speed01 = speed01,
                IsGrounded = state.IsGrounded,
                IsCrouching = state.Stance == MovementStance.Crouching,
                IsSprinting = state.Stance != MovementStance.Crouching && speed01 >= SprintSpeed01Threshold,
                IsFalling = !state.IsGrounded && state.Velocity.y < -0.1f,
                WeaponSlot = _weapon_controller == null ? (int)WeaponSlot.None : (int)_weapon_controller.ActiveSlot,
                FireSequence = _has_fire_sequence ? _last_fire_sequence : 0,
                AimPitch = PlayerAimUtility.GetEffectiveCameraPitch(state),
            };
        }

        private PlayerState GetLatestState()
        {
            if (_character != null &&
                _character.isClient &&
                !_character.isOwned &&
                _state_synchronizer != null &&
                _state_synchronizer.TryGetRenderState(out PlayerState render_state))
            {
                return render_state;
            }

            if (_character != null &&
                _character.TickManager != null &&
                _character.StateBuffer.TryGetLastAtOrBefore(_character.TickManager.CurrentTick, out PlayerState state))
            {
                return state;
            }

            return new PlayerState
            {
                Rotation = transform.rotation,
                IsGrounded = true,
                Stance = MovementStance.Standing,
            };
        }

        private void ApplyParameters(PlayerAnimationState state)
        {
            bool did_leave_ground = _was_grounded && !state.IsGrounded;

            SetFloatIfExists(_move_x_parameter, state.Move.x);
            SetFloatIfExists(_move_y_parameter, state.Move.y);
            SetFloatIfExists(_speed_parameter, state.Speed01);
            SetBoolIfExists(_grounded_parameter, state.IsGrounded);
            SetBoolIfExists(_crouch_parameter, state.IsCrouching);
            SetBoolIfExists(_sprint_parameter, state.IsSprinting);
            SetBoolIfExists(_falling_parameter, state.IsFalling);
            SetIntegerIfExists(_weapon_slot_parameter, state.WeaponSlot);
            SetFloatIfExists(_aim_pitch_parameter, state.AimPitch);

            SetFloatIfExists(_neo_forward_parameter, state.Move.y);
            SetFloatIfExists(_neo_turn_parameter, state.Move.x);
            SetBoolIfExists(_neo_grounded_parameter, state.IsGrounded);
            SetFloatIfExists(_neo_jump_parameter, state.IsGrounded ? 0f : 1f);
            SetFloatIfExists(_neo_jump_leg_parameter, state.IsGrounded ? 0f : 1f);

            SetFloatIfExists(_template_forward_parameter, state.Move.y);
            SetFloatIfExists(_template_strafe_parameter, state.Move.x);
            SetFloatIfExists(_template_locomotion_multiplier_parameter, state.IsSprinting ? 1.15f : 1f);
            SetFloatIfExists(_template_character_height_parameter, state.IsCrouching ? 0f : 1f);
            SetBoolIfExists(_template_airborne_parameter, !state.IsGrounded);
            SetBoolIfExists(_template_sprinting_parameter, state.IsSprinting);
            SetBoolIfExists(_template_dead_parameter, false);

            SetFloatIfExists(_kinemation_velocity_parameter, state.Speed01);
            SetBoolIfExists(_kinemation_moving_parameter, state.Speed01 > 0.01f);
            SetBoolIfExists(_kinemation_crouching_parameter, state.IsCrouching);
            SetBoolIfExists(_kinemation_in_air_parameter, !state.IsGrounded);
            SetFloatIfExists(_kinemation_sprinting_parameter, state.IsSprinting ? 1f : 0f);
            SetFloatIfExists(_kinemation_crouch_weight_parameter, state.IsCrouching ? 1f : 0f);
            SetFloatIfExists(_kinemation_sprint_pose_weight_parameter, state.IsSprinting ? 1f : 0f);
            SetFloatIfExists(_kinemation_full_body_weight_parameter, 1f);
            SetBoolIfExists(_kinemation_proning_parameter, false);
            SetFloatIfExists(_kinemation_prone_weight_parameter, 0f);

            if (did_leave_ground)
                TriggerIfExists(_template_jump_parameter);

            _was_grounded = state.IsGrounded;
        }

        private void SetFloatIfExists(string parameter_name, float value)
        {
            if (string.IsNullOrEmpty(parameter_name) ||
                !_parameter_cache.HasParameter(parameter_name, AnimatorControllerParameterType.Float))
            {
                return;
            }

            _third_person_animator.SetFloat(parameter_name, value);
        }

        private void SetBoolIfExists(string parameter_name, bool value)
        {
            if (string.IsNullOrEmpty(parameter_name) ||
                !_parameter_cache.HasParameter(parameter_name, AnimatorControllerParameterType.Bool))
            {
                return;
            }

            _third_person_animator.SetBool(parameter_name, value);
        }

        private void SetIntegerIfExists(string parameter_name, int value)
        {
            if (string.IsNullOrEmpty(parameter_name) ||
                !_parameter_cache.HasParameter(parameter_name, AnimatorControllerParameterType.Int))
            {
                return;
            }

            _third_person_animator.SetInteger(parameter_name, value);
        }

        private void TriggerIfExists(string parameter_name)
        {
            if (string.IsNullOrEmpty(parameter_name) ||
                !_parameter_cache.HasParameter(parameter_name, AnimatorControllerParameterType.Trigger))
            {
                return;
            }

            _third_person_animator.ResetTrigger(parameter_name);
            _third_person_animator.SetTrigger(parameter_name);
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = FindChildRecursive(root.GetChild(i), name);
                if (child != null)
                    return child;
            }

            return null;
        }

        private readonly struct AnimatorParameterCache
        {
            private readonly AnimatorControllerParameter[] _parameters;

            public readonly Animator Animator;

            public AnimatorParameterCache(Animator animator)
            {
                Animator = animator;
                _parameters = animator == null
                    ? Array.Empty<AnimatorControllerParameter>()
                    : animator.parameters;
            }

            public bool HasParameter(string name, AnimatorControllerParameterType type)
            {
                if (_parameters == null)
                    return false;

                for (int i = 0; i < _parameters.Length; i++)
                {
                    if (_parameters[i].type == type && _parameters[i].name == name)
                        return true;
                }

                return false;
            }
        }
    }
}
