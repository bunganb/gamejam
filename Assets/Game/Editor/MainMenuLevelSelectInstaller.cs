using System;
using System.Collections.Generic;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class MainMenuLevelSelectInstaller
    {
        private const string MainMenuScenePath = "Assets/Game/Scenes/MainMenu.unity";
        private const string LoadingScenePath = "Assets/Game/Scenes/Loading.unity";
        private const string GameplayScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string GameLevelRoot = "Assets/Game/Data/GameLevels/GameLevel_";

        [MenuItem("Game Jam/Install Main Menu Level Select")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before installing Main Menu Level Select.");
            }

            var scene = SceneManager.GetSceneByPath(MainMenuScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
            {
                scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);
            }

            var levelSelect = FindTransform(scene, "LevelSelect");
            if (levelSelect == null)
            {
                throw new InvalidOperationException("MainMenu is missing the LevelSelect object.");
            }

            for (var levelNumber = 1; levelNumber <= 6; levelNumber++)
            {
                var buttonTransform = FindDirectChild(levelSelect, $"lvl{levelNumber}");
                var gameLevel = AssetDatabase.LoadAssetAtPath<GameLevelDefinition>(
                    $"{GameLevelRoot}{levelNumber:00}.asset");
                if (buttonTransform == null || gameLevel == null)
                {
                    throw new InvalidOperationException($"Missing lvl{levelNumber} button or GameLevel_{levelNumber:00} asset.");
                }

                var levelButton = buttonTransform.GetComponent<LevelButton>();
                if (levelButton == null)
                {
                    throw new InvalidOperationException($"{buttonTransform.name} is missing LevelButton.");
                }

                levelButton.Configure(gameLevel, levelNumber - 1);
                EditorUtility.SetDirty(levelButton);
            }

            EnsureBuildScene(MainMenuScenePath);
            EnsureBuildScene(LoadingScenePath);
            EnsureBuildScene(GameplayScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            AssetDatabase.SaveAssets();

            if (openedAdditively)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("MAIN_MENU_LEVEL_SELECT_INSTALL_COMPLETE: lvl1-lvl6 use Loading before GameplayPrototype.");
        }

        private static void EnsureBuildScene(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var existingIndex = scenes.FindIndex(scene => scene.path == scenePath);
            if (existingIndex >= 0)
            {
                scenes[existingIndex] = new EditorBuildSettingsScene(scenePath, true);
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }
    }
}
