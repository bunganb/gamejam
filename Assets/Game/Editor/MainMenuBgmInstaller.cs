#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuBgmInstaller
{
    private const string ScenePath = "Assets/Game/Scenes/MainMenu.unity";
    private const string AudioPath = "Assets/Game/Art/Beat/HIPDUT MAIN MENU.wav";

    [MenuItem("Game Jam/Install Main Menu BGM")]
    public static void Install()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var menuRoot = GameObject.Find("MainMenu");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
        if (menuRoot == null || clip == null)
        {
            Debug.LogError($"Could not find MainMenu or {AudioPath}.");
            return;
        }

        var bgmTransform = menuRoot.transform.Find("MainMenuBGM");
        if (bgmTransform == null)
        {
            var bgmObject = new GameObject("MainMenuBGM");
            Undo.RegisterCreatedObjectUndo(bgmObject, "Create Main Menu BGM");
            bgmObject.transform.SetParent(menuRoot.transform, false);
            bgmTransform = bgmObject.transform;
        }

        var source = bgmTransform.GetComponent<AudioSource>();
        if (source == null)
        {
            Debug.LogError("MainMenuBGM has no AudioSource. Add one with the Unity CLI, then run this installer again.");
            return;
        }

        source.clip = clip;
        source.volume = 0.15f;
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;
        EditorUtility.SetDirty(bgmTransform.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Installed low-volume looping Main Menu BGM.");
    }
}
#endif
