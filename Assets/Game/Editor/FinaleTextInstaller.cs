#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameJam.Gameplay;

namespace GameJam.Editor
{
    public static class FinaleTextInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string FontPath = "Assets/Fonts/Creepy-Story SDF.asset";

        [MenuItem("Game Jam/Install Final Level Ending Text")]
        public static void Install()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            var systems = FindSystems(scene);
            var fader = systems != null ? systems.GetComponent<LevelTransitionFader>() : null;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (fader == null || font == null)
            {
                throw new System.InvalidOperationException("GameplayPrototype or Creepy-Story SDF font is missing.");
            }

            fader.ConfigureFinaleFont(font);
            EditorUtility.SetDirty(fader);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("FINAL_LEVEL_ENDING_TEXT_INSTALLED: TMP finale text uses Creepy-Story SDF.");
        }

        private static GameObject FindSystems(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Systems") return root;
            }
            return null;
        }
    }
}
#endif
