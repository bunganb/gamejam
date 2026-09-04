#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuExitInstaller
{
    private const string ScenePath = "Assets/Game/Scenes/MainMenu.unity";

    [MenuItem("Game Jam/Connect Main Menu Exit Button")]
    public static void Install()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var exitObject = GameObject.Find("MainMenu/Exit");
        var director = Object.FindFirstObjectByType<MenuDirector>();
        if (exitObject == null || director == null)
        {
            Debug.LogError("Could not find MainMenu/Exit or MenuDirector.");
            return;
        }

        var button = exitObject.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("MainMenu/Exit has no Button component.");
            return;
        }

        for (var i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (button.onClick.GetPersistentTarget(i) == director &&
                button.onClick.GetPersistentMethodName(i) == nameof(MenuDirector.QuitGame))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Main Menu Exit button is already connected.");
                return;
            }
        }

        UnityEventTools.AddPersistentListener(button.onClick, director.QuitGame);
        EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Connected Main Menu Exit button to MenuDirector.QuitGame().");
    }
}
#endif
