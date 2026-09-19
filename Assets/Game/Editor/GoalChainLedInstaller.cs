using System;
using System.Linq;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class GoalChainLedInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";

        [MenuItem("Game Jam/Gameplay/Install Goal Chain On LED DJ")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before installing the LED goal chain.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var objects = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var led = objects.FirstOrDefault(t => t.name == "LED DJ");
                var gameplay = objects.Select(t => t.GetComponent<PuzzleGameplayController>())
                    .FirstOrDefault(c => c != null);
                if (led == null || gameplay == null)
                    throw new InvalidOperationException("GameplayPrototype needs LED DJ and PuzzleGameplayController.");

                var display = led.GetComponent<LedGoalChainDisplay>();
                if (display == null)
                    display = Undo.AddComponent<LedGoalChainDisplay>(led.gameObject);

                var serialized = new SerializedObject(display);
                serialized.FindProperty("gameplayController").objectReferenceValue = gameplay;
                serialized.FindProperty("textureWidth").intValue = 256;
                serialized.FindProperty("textureHeight").intValue = 64;
                serialized.FindProperty("pulseTickInterval").floatValue = 0.05f;
                serialized.FindProperty("ledDotSize").floatValue = 0.67f;
                serialized.FindProperty("completionFont").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Creepy Spider Demo SDF.asset");
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(display);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("GOAL_CHAIN_LED_INSTALL_COMPLETE: LED DJ now displays the three-slot goal chain.");
            }
            finally
            {
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
