#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameJam.Editor
{
    public static class ApplicationIconInstaller
    {
        private const string IconPath = "Assets/Image/logo.png";

        [MenuItem("Game Jam/Set Application Icon")]
        public static void Install()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogError($"Could not find application icon at {IconPath}.");
                return;
            }

            // Standalone expects eight icon slots for the Windows icon mip sizes.
            var icons = new Texture2D[8];
            for (var index = 0; index < icons.Length; index++)
            {
                icons[index] = icon;
            }

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, icons);
            AssetDatabase.SaveAssets();
            Debug.Log($"Application icon set to {IconPath} for Standalone.");
        }
    }
}
#endif
