#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoadingTransitionInstaller
{
    private const string ScenePath = "Assets/Game/Scenes/Loading.unity";
    private const string MaterialPath = "Assets/Game/Materials/RadialLevelTransition.mat";

    [MenuItem("Game Jam/Install Loading Radial Transition")]
    public static void Install()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var loadingManager = Object.FindFirstObjectByType<LoadingScreenManager>();
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (loadingManager == null || material == null)
        {
            Debug.LogError("Loading radial transition requires LoadingScreenManager and RadialLevelTransition.mat.");
            return;
        }

        loadingManager.ConfigureTransitionMaterial(material);
        EditorUtility.SetDirty(loadingManager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Installed loading-to-gameplay radial transition.");
    }
}
#endif
