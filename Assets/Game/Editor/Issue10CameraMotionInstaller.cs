using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class Issue10CameraMotionInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string ProfilePath = "Assets/Game/Data/CameraMotionProfile_Prototype.asset";

        [MenuItem("Game Jam/Install Prototype Camera Motion")]
        public static void Install()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CameraMotionProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CameraMotionProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.ApplyPrototypeDefaults();

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            var rig = FindTransform(scene, "CameraRig");
            var mainCamera = FindComponent<Camera>(scene);
            var eventHub = FindComponent<PuzzleGameplayEvents>(scene);
            if (rig == null || mainCamera == null || eventHub == null)
            {
                throw new System.InvalidOperationException("GameplayPrototype requires CameraRig, Main Camera, and PuzzleGameplayEvents.");
            }

            var director = rig.GetComponent<PrototypeCameraDirector>();
            if (director == null)
            {
                director = rig.gameObject.AddComponent<PrototypeCameraDirector>();
            }

            director.ConfigureReferences(eventHub, mainCamera, profile);
            var hud = FindComponent<PrototypeObjectiveHud>(scene);
            var controller = FindComponent<PuzzleGameplayController>(scene);
            if (hud != null && controller != null)
            {
                hud.ConfigureReferences(controller, eventHub);
                EditorUtility.SetDirty(hud);
            }
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            if (openedAdditively)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("PROTOTYPE_CAMERA_MOTION_INSTALL_COMPLETE: idle sway and completion zoom sequence installed.");
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
