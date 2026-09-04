#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuAudioInstaller
{
    private const string ScenePath = "Assets/Game/Scenes/MainMenu.unity";
    private const string HoverPath = "Assets/Game/Art/sfx/button_hover.wav";
    private const string ClickPath = "Assets/Game/Art/sfx/button_click.wav";

    [MenuItem("Game Jam/Install Main Menu Button SFX")]
    public static void Install()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(HoverPath);
        var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickPath);
        if (hoverClip == null || clickClip == null)
        {
            Debug.LogError($"Missing menu SFX. Expected {HoverPath} and {ClickPath}.");
            return;
        }

        var mainMenu = GameObject.Find("MainMenu");
        if (mainMenu == null)
        {
            Debug.LogError("Could not find MainMenu root.");
            return;
        }

        var audioObject = mainMenu.transform.Find("UIAudioSource");
        if (audioObject == null)
        {
            var createdAudioObject = new GameObject("UIAudioSource");
            Undo.RegisterCreatedObjectUndo(createdAudioObject, "Create Main Menu UI Audio Source");
            createdAudioObject.transform.SetParent(mainMenu.transform, false);
            audioObject = createdAudioObject.transform;
        }

        var sharedSource = audioObject.GetComponent<AudioSource>() ?? audioObject.gameObject.AddComponent<AudioSource>();
        if (sharedSource == null)
        {
            Debug.LogError("Could not create the shared Main Menu AudioSource.");
            return;
        }

        sharedSource.playOnAwake = false;
        sharedSource.loop = false;
        sharedSource.spatialBlend = 0f;

        var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var installedCount = 0;
        foreach (var button in buttons)
        {
            if (!button.transform.IsChildOf(mainMenu.transform))
            {
                continue;
            }

            var sfx = button.GetComponent<UIButtonSfx>() ?? button.gameObject.AddComponent<UIButtonSfx>();
            if (sfx == null)
            {
                Debug.LogWarning($"Could not add button SFX to {button.name}.");
                continue;
            }

            sfx.Configure(sharedSource, hoverClip, clickClip);
            EditorUtility.SetDirty(button.gameObject);
            installedCount++;
        }

        EditorUtility.SetDirty(mainMenu);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Installed hover/click SFX on {installedCount} Main Menu buttons.");
    }
}
#endif
