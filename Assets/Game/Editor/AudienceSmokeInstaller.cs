#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AudienceSmokeInstaller
{
    private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
    private const string ParentPath = "Environment/AudienceAnchors";
    private const string MaterialPath = "Assets/Samples/Timeline/1.8.13/Gameplay Sequence Demo/Particles/Misc Effects/Materials/DustMote.mat";

    [MenuItem("Game Jam/Install Audience Smoke")]
    public static void Install()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var parent = GameObject.Find(ParentPath);
        if (parent == null)
        {
            Debug.LogError($"Could not find {ParentPath}.");
            return;
        }

        var controller = parent.GetComponent<GameJam.Gameplay.AudienceSmokeEffect>();
        if (controller == null)
        {
            Debug.LogError("AudienceAnchors has no AudienceSmokeEffect. Add it with the Unity CLI, then run this installer again.");
            return;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Debug.LogWarning($"Smoke material not found at {MaterialPath}; runtime fallback will be used.");
        }

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("smokeMaterial").objectReferenceValue = material;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(parent);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Installed lightweight audience smoke with lighting-following color.");
    }
}
#endif
