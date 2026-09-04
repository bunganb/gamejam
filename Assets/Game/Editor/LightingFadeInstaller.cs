using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class LightingFadeInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string EnvironmentPrefabPath = "Assets/Game/Prefabs/Environment/NightclubSet.prefab";

        [MenuItem("Game Jam/Install Lighting Fade In")]
        public static void Install()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            var added = 0;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light.gameObject.scene != scene || IsControllerOwned(light)) continue;
                if (light.GetComponent<LightStartupFade>() == null)
                {
                    light.gameObject.AddComponent<LightStartupFade>();
                    added++;
                }
            }

            foreach (var neon in Object.FindObjectsByType<NeonLightController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (neon.gameObject.scene == scene) EditorUtility.SetDirty(neon);
            foreach (var floor in Object.FindObjectsByType<DanceFloorController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (floor.gameObject.scene == scene) EditorUtility.SetDirty(floor);

            var prefabAdded = InstallIntoEnvironmentPrefab();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"LIGHTING_FADE_IN_INSTALL_COMPLETE: added {added + prefabAdded} light fades; neon and dance floor use startup fade.");
        }

        private static int InstallIntoEnvironmentPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(EnvironmentPrefabPath);
            if (root == null) return 0;
            var added = 0;
            try
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (IsControllerOwned(light) || light.GetComponent<LightStartupFade>() != null) continue;
                    light.gameObject.AddComponent<LightStartupFade>();
                    added++;
                }
                if (added > 0) PrefabUtility.SaveAsPrefabAsset(root, EnvironmentPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return added;
        }

        private static bool IsControllerOwned(Light light)
        {
            return light.GetComponentInParent<MovingSpotlightController>(true) != null
                || light.GetComponentInParent<StrobeLightController>(true) != null
                || light.GetComponentInParent<DiscoSpotlightRig>(true) != null;
        }
    }
}
