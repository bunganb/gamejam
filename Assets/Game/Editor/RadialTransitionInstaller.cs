#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RadialTransitionInstaller
{
    private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
    private const string ShaderPath = "Assets/Game/Shaders/RadialLevelTransition.shader";
    private const string MaterialPath = "Assets/Game/Materials/RadialLevelTransition.mat";

    [MenuItem("Game Jam/Install Radial Level Transition")]
    public static void Install()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var systems = GameObject.Find("Systems");
        var fader = systems != null
            ? systems.GetComponent<GameJam.Gameplay.LevelTransitionFader>()
            : null;
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (fader == null || shader == null)
        {
            Debug.LogError("Radial transition requires Systems/LevelTransitionFader and the radial shader.");
            return;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "RadialLevelTransition" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        fader.ConfigureRadialMaterial(material);
        EditorUtility.SetDirty(fader);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Installed radial MC-focused level transition material.");
    }
}
#endif
