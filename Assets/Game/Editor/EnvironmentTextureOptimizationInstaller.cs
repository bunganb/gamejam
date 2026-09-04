#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameJam.Editor
{
    public static class EnvironmentTextureOptimizationInstaller
    {
        private const string TexturePath = "Assets/Game/3d/colorr (1).png";

        [MenuItem("Game Jam/Optimize Environment Texture")]
        public static void Install()
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not find texture importer at {TexturePath}.");
                return;
            }

            var settings = importer.GetPlatformTextureSettings("Standalone");
            settings.overridden = true;
            settings.maxTextureSize = 1024;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = 40;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            Debug.Log($"Optimized distant environment texture: {TexturePath} (1024px, compressed quality 40).");
        }
    }
}
#endif
