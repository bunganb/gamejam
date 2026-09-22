#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
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

            var target = NamedBuildTarget.Standalone;
            var iconSizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
            if (iconSizes == null || iconSizes.Length == 0)
            {
                Debug.LogError("Standalone does not expose application icon slots.");
                return;
            }

            var icons = new Texture2D[iconSizes.Length];
            for (var index = 0; index < icons.Length; index++)
            {
                icons[index] = icon;
            }

            PlayerSettings.SetIcons(target, icons, IconKind.Application);
            AssetDatabase.SaveAssets();

            var assignedIcons = PlayerSettings.GetIcons(target, IconKind.Application);
            if (assignedIcons == null || assignedIcons.Length != icons.Length)
            {
                Debug.LogError("Standalone application icon assignment could not be verified.");
                return;
            }

            Debug.Log(
                $"Application icon set to {IconPath} for {assignedIcons.Length} Standalone icon sizes: " +
                string.Join(", ", iconSizes));
        }
    }
}
#endif
