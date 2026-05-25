using System;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public class PlayerThirdPersonPoseController : MonoBehaviour
    {
        private static readonly BonePose[] DefaultPose =
        {
            new("FpsChar_Spine_Bone", new Vector3(-6f, 0f, 0f)),
            new("FpsChar_Torso_Bone", new Vector3(4f, 0f, 0f)),
            new("FpsChar_Neck_Bone", new Vector3(6f, 0f, 0f)),
            new("FpsChar_RArm_Shoulder_Bone", new Vector3(0f, 0f, 20f)),
            new("FpsChar_RArm_Upper_Bone", new Vector3(38f, -28f, 108f)),
            new("FpsChar_RArm_Lower_Bone", new Vector3(0f, -8f, -78f)),
            new("FpsChar_RHand_Bone", new Vector3(0f, -8f, -12f)),
            new("FpsChar_LArm_Shoulder_Bone", new Vector3(0f, 0f, -20f)),
            new("FpsChar_LArm_Upper_Bone", new Vector3(38f, 28f, -108f)),
            new("FpsChar_LArm_Lower_Bone", new Vector3(0f, 8f, 78f)),
            new("FpsChar_LHand_Bone", new Vector3(0f, 8f, 12f)),
            new("FpsChar_RLeg_Upper_Bone", new Vector3(0f, 0f, 2f)),
            new("FpsChar_LLeg_Upper_Bone", new Vector3(0f, 0f, -2f)),
        };

        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerVisualController _visual;
        [SerializeField] private Transform _skeleton_root;
        [SerializeField] private bool _apply_pose;
        [SerializeField] private float _pose_blend_speed = 16f;
        [SerializeField] private float _crouch_torso_pitch = 8f;
        [SerializeField] private float _fall_torso_pitch = -6f;

        private readonly RuntimeBonePose[] _runtime_pose = new RuntimeBonePose[DefaultPose.Length];
        private bool _has_cached_bones;
        private int _cached_bone_count;

        private void Awake()
        {
            CacheReferences();
            CacheBones();
        }

        private void LateUpdate()
        {
            if (!_apply_pose)
                return;

            CacheReferences();
            CacheBones();
            ApplyPose();
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_visual == null)
                _visual = GetComponent<PlayerVisualController>();

            if (_skeleton_root == null && _visual != null)
                _skeleton_root = FindChildRecursive(_visual.ThirdPersonRoot, "ThirdPersonCharacter");

            if (_skeleton_root == null)
                _skeleton_root = FindChildRecursive(transform, "ThirdPersonCharacter");
        }

        private void CacheBones()
        {
            if (_has_cached_bones || _skeleton_root == null)
                return;

            DisableInactiveAnimator();
            _cached_bone_count = 0;
            for (int i = 0; i < DefaultPose.Length; i++)
            {
                Transform bone = FindChildRecursive(_skeleton_root, DefaultPose[i].BoneName);
                if (bone != null)
                    _cached_bone_count++;

                _runtime_pose[i] = new RuntimeBonePose(
                    bone,
                    bone == null ? Quaternion.identity : bone.localRotation,
                    Quaternion.Euler(DefaultPose[i].LocalEulerOffset));
            }

            _has_cached_bones = _cached_bone_count > 0;
        }

        private void ApplyPose()
        {
            PlayerState state = GetLatestState();
            float stance_pitch = state.Stance == MovementStance.Crouching ? _crouch_torso_pitch : 0f;
            float grounded_pitch = state.IsGrounded ? 0f : _fall_torso_pitch;
            Quaternion state_offset = Quaternion.Euler(stance_pitch + grounded_pitch, 0f, 0f);
            float blend = 1f - Mathf.Exp(-_pose_blend_speed * Time.deltaTime);

            for (int i = 0; i < _runtime_pose.Length; i++)
            {
                RuntimeBonePose pose = _runtime_pose[i];
                if (pose.Bone == null)
                    continue;

                Quaternion target_rotation = pose.BindRotation * pose.OffsetRotation;
                if (pose.Bone.name == "FpsChar_Spine_Bone" || pose.Bone.name == "FpsChar_Torso_Bone")
                    target_rotation *= state_offset;

                pose.Bone.localRotation = Quaternion.Slerp(pose.Bone.localRotation, target_rotation, blend);
            }
        }

        private PlayerState GetLatestState()
        {
            if (_character != null &&
                _character.TickManager != null &&
                _character.StateBuffer.TryGetLastAtOrBefore(_character.TickManager.CurrentTick, out PlayerState state))
            {
                return state;
            }

            return new PlayerState
            {
                IsGrounded = true,
                Stance = MovementStance.Standing,
            };
        }

        private void DisableInactiveAnimator()
        {
            Animator animator = _skeleton_root.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController == null)
                animator.enabled = false;
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

        [Serializable]
        private readonly struct BonePose
        {
            public readonly string BoneName;
            public readonly Vector3 LocalEulerOffset;

            public BonePose(string bone_name, Vector3 local_euler_offset)
            {
                BoneName = bone_name;
                LocalEulerOffset = local_euler_offset;
            }
        }

        private readonly struct RuntimeBonePose
        {
            public readonly Transform Bone;
            public readonly Quaternion BindRotation;
            public readonly Quaternion OffsetRotation;

            public RuntimeBonePose(Transform bone, Quaternion bind_rotation, Quaternion offset_rotation)
            {
                Bone = bone;
                BindRotation = bind_rotation;
                OffsetRotation = offset_rotation;
            }
        }
    }
}
