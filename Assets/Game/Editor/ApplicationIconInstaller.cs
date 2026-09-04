#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ApplicationIconInstaller
{
    private const string LogoPath = "Assets/Image/logo.png";

    [MenuItem("Game Jam/Set Application Icon From Logo")]
    public static void SetApplicationIcon()
    {
        var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
        if (logo == null)
        {
            Debug.LogError($"Could not find application logo at {LogoPath}.");
            return;
        }

        // Standalone Windows expects eight icon slots. Unity will scale the
        // source texture for the required sizes when the same logo is used.
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,
            new[] { logo, logo, logo, logo, logo, logo, logo, logo });
        EditorUtility.SetDirty(logo);
        Debug.Log($"Application icon set from {LogoPath}.");
    }
}
#endif
