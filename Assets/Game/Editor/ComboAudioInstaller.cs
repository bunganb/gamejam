using System;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class ComboAudioInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string SfxFolder = "Assets/Game/Art/sfx/";

        [MenuItem("Game Jam/Install Combo SFX")]
        public static void Install()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            var manager = FindComponent<ComboTierManager>(scene);
            if (manager == null) throw new InvalidOperationException("ComboTierManager missing from GameplayPrototype.");
            var source = manager.GetComponent<AudioSource>();
            if (source == null) source = manager.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            manager.ConfigureAudio(
                source,
                Require<AudioClip>(SfxFolder + "1_combo.wav"),
                Require<AudioClip>(SfxFolder + "2_combo.wav"),
                Require<AudioClip>(SfxFolder + "3_combo.wav"),
                Require<AudioClip>(SfxFolder + "crowd_boo V2.wav"));
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(manager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("COMBO_SFX_INSTALL_COMPLETE: combo 1/2/3 and crowd boo assigned.");
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Missing combo audio: " + path);
            return asset;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }
    }
}
