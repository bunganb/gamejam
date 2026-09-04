#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public static class AudioMixerRoutingInstaller
{
    private const string MixerPath = "Assets/Game/MainMixer.mixer";
    private static readonly string[] ScenePaths =
    {
        "Assets/Game/Scenes/MainMenu.unity",
        "Assets/Game/Scenes/GameplayPrototype.unity"
    };

    [MenuItem("Game Jam/Route All Audio Through Main Mixer")]
    public static void Install()
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        var musicGroup = mixer?.FindMatchingGroups("BGM");
        var sfxGroup = mixer?.FindMatchingGroups("SFX");
        if (musicGroup == null || musicGroup.Length == 0 || sfxGroup == null || sfxGroup.Length == 0)
        {
            Debug.LogError("MainMixer must contain BGM and SFX groups.");
            return;
        }

        foreach (var scenePath in ScenePaths)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            var routedCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                {
                    source.outputAudioMixerGroup = IsMusicSource(source) ? musicGroup[0] : sfxGroup[0];
                    EditorUtility.SetDirty(source);
                    routedCount++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Routed {routedCount} audio sources in {scenePath}.");
        }
    }

    private static bool IsMusicSource(AudioSource source)
    {
        var objectName = source != null ? source.name : string.Empty;
        return objectName.IndexOf("BGM", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Music", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Harmony", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Drum", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Ketipung", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Bass", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("FullSong", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
#endif
