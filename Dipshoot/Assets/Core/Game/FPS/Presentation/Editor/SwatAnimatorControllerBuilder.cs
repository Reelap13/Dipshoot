using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Players.Editor
{
    public static class SwatAnimatorControllerBuilder
    {
        private const string RequestPath = "Temp/RebuildSwatAnimator.request";
        private const string ControllerPath = "Assets/Core/Resources/Presentation/Kinemation/Animations/Locomotion/DipshootSwatThirdPerson.controller";
        private const string VisualDefinitionPath = "Assets/Core/Resources/Presentation/Characters/Humanoid/PlayerVisualDefinition.asset";
        private const string Root = "Assets/Core/Resources/Presentation/Kinemation/Animations/Locomotion/Humanoid";

        [InitializeOnLoadMethod]
        private static void RunRequestedBuild()
        {
            if (!File.Exists(RequestPath))
                return;

            File.Delete(RequestPath);
            Build();
        }

        [MenuItem("Dipshoot/Resources/Rebuild SWAT Third Person Animator")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
            AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Velocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Crouching", AnimatorControllerParameterType.Bool);
            controller.AddParameter("InAir", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Sprinting", AnimatorControllerParameterType.Float);
            controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine state_machine = controller.layers[0].stateMachine;
            state_machine.states = System.Array.Empty<ChildAnimatorState>();
            state_machine.anyStateTransitions = System.Array.Empty<AnimatorStateTransition>();

            AnimatorState standing = state_machine.AddState("Standing");
            standing.motion = CreateStandingTree(controller);
            state_machine.defaultState = standing;

            AnimatorState crouching = state_machine.AddState("Crouching");
            crouching.motion = CreateCrouchingTree(controller);

            AnimatorState jump_start = AddClipState(state_machine, "JumpStart", $"{Root}/InAir/C_JumpStart_Humanoid.fbx");
            AnimatorState jump_loop = AddClipState(state_machine, "JumpLoop", $"{Root}/InAir/C_JumpLoop_Humanoid.fbx");
            AnimatorState jump_end = AddClipState(state_machine, "JumpEnd", $"{Root}/InAir/C_JumpEnd_Humanoid.fbx");

            AddBoolTransition(standing, crouching, "Crouching", true, false);
            AddBoolTransition(crouching, standing, "Crouching", false, false);
            AddBoolTransition(standing, jump_start, "InAir", true, false);
            AddBoolTransition(crouching, jump_start, "InAir", true, false);
            AddExitTransition(jump_start, jump_loop, 0.8f, 0.04f);
            AddBoolTransition(jump_loop, jump_end, "InAir", false, false);
            AddBoolTransition(jump_end, standing, "Crouching", false, true);
            AddBoolTransition(jump_end, crouching, "Crouching", true, true);

            AssignToVisualDefinition(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static BlendTree CreateStandingTree(AnimatorController controller)
        {
            BlendTree tree = CreateTree(controller, "StandingBlendTree");
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Idle_Humanoid.fbx", 0f, 0f);
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Run_Fwd_Humanoid.fbx", 0f, 1f);
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Sprint_Fwd_Humanoid.fbx", 0f, 1.35f);
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Run_Bwd_Humanoid.fbx", 0f, -1f);
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Strafe_Right_Humanoid.fbx", 1f, 0f);
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Run_Fwd_Left_Humanoid.fbx", -1f, 0.55f);
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Run_Bwd_Left_Humanoid.fbx", -1f, -0.55f);
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Run_Fwd_Right_Humanoid.fbx", 1f, 0.55f);
            AddChild(tree, "WeaponSet/Standing/C_Rifle_Run_Bwd_Right_Humanoid.fbx", 1f, -0.55f);
            return tree;
        }

        private static BlendTree CreateCrouchingTree(AnimatorController controller)
        {
            BlendTree tree = CreateTree(controller, "CrouchingBlendTree");
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Idle_Humanoid.fbx", 0f, 0f);
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Walk_Fwd_Humanoid.fbx", 0f, 1f);
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Walk_Bwd_Humanoid.fbx", 0f, -1f);
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Strafe_Left_Humanoid.fbx", -1f, 0f);
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Strafe_Right_Humanoid.fbx", 1f, 0f);
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Walk_Fwd_Left_Humanoid.fbx", -1f, 0.55f);
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Walk_Fwd_Right_Humanoid.fbx", 1f, 0.55f);
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Walk_Bwd_Left_Humanoid.fbx", -1f, -0.55f);
            AddChild(tree, "WeaponSet/Crouching/C_Rifle_Crouch_Walk_Bwd_Right_Humanoid.fbx", 1f, -0.55f);
            return tree;
        }

        private static BlendTree CreateTree(AnimatorController controller, string name)
        {
            BlendTree tree = new()
            {
                name = name,
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false,
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            return tree;
        }

        private static AnimatorState AddClipState(AnimatorStateMachine state_machine, string name, string path)
        {
            AnimatorState state = state_machine.AddState(name);
            state.motion = LoadClip(path);
            return state;
        }

        private static void AddChild(BlendTree tree, string relative_path, float x, float y)
        {
            AnimationClip clip = LoadClip($"{Root}/{relative_path}");
            if (clip != null)
                tree.AddChild(clip, new Vector2(x, y));
        }

        private static AnimationClip LoadClip(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview", System.StringComparison.Ordinal))
                    return clip;
            }

            Debug.LogWarning($"Missing animation clip: {path}");
            return null;
        }

        private static void AddBoolTransition(
            AnimatorState from,
            AnimatorState to,
            string parameter,
            bool value,
            bool has_exit_time)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = has_exit_time;
            transition.exitTime = has_exit_time ? 0.85f : 0f;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState to, float exit_time, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exit_time;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
        }

        private static void AssignToVisualDefinition(AnimatorController controller)
        {
            Object visual_definition = AssetDatabase.LoadAssetAtPath<Object>(VisualDefinitionPath);
            if (visual_definition == null)
                return;

            SerializedObject serialized_object = new(visual_definition);
            serialized_object.FindProperty("_third_person_animator_controller").objectReferenceValue = controller;
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visual_definition);
        }
    }
}
