using System;
using System.Linq;
using GameJam.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam.Editor
{
    public static class GameplayTutorialInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";

        [MenuItem("Game Jam/Gameplay/Install Level 1 Tutorial")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before installing the gameplay tutorial.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var transforms = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var bubble = Find(transforms, "bubble");
                var carlImage = Find(transforms, "carl-img")?.GetComponent<Image>();
                var dialogue = Find(transforms, "carl-dialogue")?.GetComponent<TMP_Text>();
                var rules = Find(transforms, "rules");
                var movement = Find(transforms, "movement");
                var restart = Find(transforms, "Restart");
                var led = Find(transforms, "LED DJ");
                var gameplay = transforms.Select(t => t.GetComponent<PuzzleGameplayController>())
                    .FirstOrDefault(c => c != null);
                var eventsHub = transforms.Select(t => t.GetComponent<PuzzleGameplayEvents>())
                    .FirstOrDefault(e => e != null);

                if (bubble == null || carlImage == null || dialogue == null || rules == null ||
                    movement == null || restart == null || led == null || gameplay == null || eventsHub == null)
                    throw new InvalidOperationException("GameplayPrototype is missing tutorial UI or gameplay references.");

                var tutorial = bubble.GetComponent<GameplayTutorialController>();
                if (tutorial == null)
                    tutorial = Undo.AddComponent<GameplayTutorialController>(bubble.gameObject);
                var bubbleAnimator = bubble.GetComponent<Animator>();
                if (bubbleAnimator == null)
                    bubbleAnimator = Undo.AddComponent<Animator>(bubble.gameObject);
                EnsureBubbleAnimator(bubbleAnimator);

                var serialized = new SerializedObject(tutorial);
                serialized.FindProperty("gameplayController").objectReferenceValue = gameplay;
                serialized.FindProperty("gameplayEvents").objectReferenceValue = eventsHub;
                serialized.FindProperty("bubble").objectReferenceValue = bubble.gameObject;
                serialized.FindProperty("bubbleRoot").objectReferenceValue = bubble;
                serialized.FindProperty("bubbleAnimator").objectReferenceValue = bubbleAnimator;
                serialized.FindProperty("carlImage").objectReferenceValue = carlImage;
                serialized.FindProperty("dialogueText").objectReferenceValue = dialogue;
                serialized.FindProperty("movementUi").objectReferenceValue = movement.gameObject;
                serialized.FindProperty("restartUi").objectReferenceValue = restart.gameObject;
                serialized.FindProperty("rulesTarget").objectReferenceValue = rules;
                serialized.FindProperty("ledGoalTarget").objectReferenceValue = led.GetComponent<Renderer>();
                serialized.FindProperty("radialHighlightMaterial").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/RadialLevelTransition.mat");
                serialized.FindProperty("typingInterval").floatValue = 0.025f;

                var lines = serialized.FindProperty("lines");
                lines.arraySize = 3;
                ConfigureLine(lines.GetArrayElementAtIndex(0), "GoalChain", 
                    "Hey! I'm Carl. See those colors up there? That’s your Goal Chain!",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/up_there.png"), led, null);
                ConfigureLine(lines.GetArrayElementAtIndex(1), "Rules", 
                    "Every move shifts ALL active tiles to their next color! Plan where you land, follow the Goal Chain, and make this graveyard PARTY!",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/aha.png"), null, rules.GetComponent<RectTransform>());
                ConfigureLine(lines.GetArrayElementAtIndex(2), "Break", 
                    "Oops! One wrong color breaks the chain! Complete it! Good Luck!",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/dizzy.png"), null, null);

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tutorial);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("GAMEPLAY_TUTORIAL_INSTALL_COMPLETE: Level 1 tutorial configured.");
            }
            finally
            {
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ConfigureLine(
            SerializedProperty line,
            string id,
            string message,
            Sprite sprite,
            Transform worldTarget,
            RectTransform uiTarget)
        {
            line.FindPropertyRelative("id").stringValue = id;
            line.FindPropertyRelative("message").stringValue = message;
            line.FindPropertyRelative("carlSprite").objectReferenceValue = sprite;
            line.FindPropertyRelative("worldHighlightTarget").objectReferenceValue = worldTarget;
            line.FindPropertyRelative("uiHighlightTarget").objectReferenceValue = uiTarget;
        }

        private static Transform Find(Transform[] transforms, string objectName)
        {
            return transforms.FirstOrDefault(t => string.Equals(t.name, objectName, StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureBubbleAnimator(Animator animator)
        {
            const string folder = "Assets/Game/Animations";
            const string controllerPath = folder + "/TutorialBubble.controller";
            const string hiddenPath = folder + "/TutorialBubbleHidden.anim";
            const string showPath = folder + "/TutorialBubbleShow.anim";
            const string hidePath = folder + "/TutorialBubbleHide.anim";

            EnsureFolder("Assets/Game", "Animations");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            var hidden = AssetDatabase.LoadAssetAtPath<AnimationClip>(hiddenPath);
            if (hidden == null)
            {
                hidden = CreateScaleClip("TutorialBubbleHidden", 0.001f, 0.001f);
                AssetDatabase.CreateAsset(hidden, hiddenPath);
            }
            var show = AssetDatabase.LoadAssetAtPath<AnimationClip>(showPath);
            if (show == null)
            {
                show = CreateBounceClip("TutorialBubbleShow");
                AssetDatabase.CreateAsset(show, showPath);
            }
            var hide = AssetDatabase.LoadAssetAtPath<AnimationClip>(hidePath);
            if (hide == null)
            {
                hide = CreateHideClip("TutorialBubbleHide");
                AssetDatabase.CreateAsset(hide, hidePath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var hiddenState = FindState(stateMachine, "Hidden") ?? stateMachine.AddState("Hidden");
            var showState = FindState(stateMachine, "Show") ?? stateMachine.AddState("Show");
            var hideState = FindState(stateMachine, "Hide") ?? stateMachine.AddState("Hide");
            hiddenState.motion = hidden;
            showState.motion = show;
            hideState.motion = hide;
            stateMachine.defaultState = hiddenState;
            EnsureTrigger(controller, "Show");
            EnsureTrigger(controller, "Hide");
            EnsureTransition(hiddenState, showState, "Show", 0.02f);
            EnsureTransition(showState, hideState, "Hide", 0.06f);
            EnsureTransition(hideState, hiddenState, null, 0f);
            AssetDatabase.SaveAssets();

            animator.runtimeAnimatorController = controller;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            return machine.states.FirstOrDefault(entry => entry.state.name == name).state;
        }

        private static void EnsureTrigger(AnimatorController controller, string name)
        {
            if (!controller.parameters.Any(parameter => parameter.name == name))
                controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }

        private static void EnsureTransition(AnimatorState from, AnimatorState to, string trigger, float duration)
        {
            if (from.transitions.Any(transition => transition.destinationState == to))
                return;
            var transition = from.AddTransition(to);
            transition.hasExitTime = trigger == null;
            transition.exitTime = 1f;
            transition.duration = duration;
            if (trigger != null)
                transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static AnimationClip CreateScaleClip(string name, float from, float to)
        {
            var clip = new AnimationClip { name = name, wrapMode = WrapMode.Once };
            var curve = AnimationCurve.Linear(0f, from, 0.05f, to);
            AddScaleCurve(clip, "m_LocalScale.x", curve);
            AddScaleCurve(clip, "m_LocalScale.y", curve);
            AddScaleCurve(clip, "m_LocalScale.z", curve);
            return clip;
        }

        private static AnimationClip CreateBounceClip(string name)
        {
            var clip = new AnimationClip { name = name, wrapMode = WrapMode.Once };
            var curve = new AnimationCurve(
                new Keyframe(0f, 0.001f),
                new Keyframe(0.12f, 1.12f),
                new Keyframe(0.22f, 0.94f),
                new Keyframe(0.34f, 1f));
            AddScaleCurve(clip, "m_LocalScale.x", curve);
            AddScaleCurve(clip, "m_LocalScale.y", curve);
            AddScaleCurve(clip, "m_LocalScale.z", curve);
            return clip;
        }

        private static AnimationClip CreateHideClip(string name)
        {
            var clip = new AnimationClip { name = name, wrapMode = WrapMode.Once };
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.10f, 1.05f),
                new Keyframe(0.24f, 0.001f));
            AddScaleCurve(clip, "m_LocalScale.x", curve);
            AddScaleCurve(clip, "m_LocalScale.y", curve);
            AddScaleCurve(clip, "m_LocalScale.z", curve);
            return clip;
        }

        private static void AddScaleCurve(AnimationClip clip, string property, AnimationCurve curve)
        {
            var binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(RectTransform), property);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }
}
