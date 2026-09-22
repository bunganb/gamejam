using System;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class WinCrowdChantInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string ClipPath = "Assets/Game/Art/sfx/crowd_chant v3.wav";
        private const string MixerPath = "Assets/Game/MainMixer.mixer";

        [MenuItem("Game Jam/Audio/Install Win Crowd Chant")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before installing crowd chant.");

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            var sfxGroups = mixer != null ? mixer.FindMatchingGroups("SFX") : null;
            if (clip == null || sfxGroups == null || sfxGroups.Length == 0)
                throw new InvalidOperationException("Crowd chant or SFX mixer group is missing.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                PrototypeMusicDirector director = null;
                LevelLoader loader = null;
                PuzzleGameplayEvents eventHub = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    director ??= root.GetComponentInChildren<PrototypeMusicDirector>(true);
                    loader ??= root.GetComponentInChildren<LevelLoader>(true);
                    eventHub ??= root.GetComponentInChildren<PuzzleGameplayEvents>(true);
                }

                if (director == null || loader == null || eventHub == null)
                    throw new InvalidOperationException("Gameplay scene is missing music, loader, or events.");

                var child = director.transform.Find("WinCrowdChant");
                if (child == null)
                {
                    child = new GameObject("WinCrowdChant").transform;
                    child.SetParent(director.transform, false);
                }

                var source = child.TryGetComponent<AudioSource>(out var existingSource)
                    ? existingSource
                    : child.gameObject.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = sfxGroups[0];
                if (source.clip == null)
                    source.clip = clip;
                var chant = director.TryGetComponent<WinCrowdChant>(out var existingChant)
                    ? existingChant
                    : director.gameObject.AddComponent<WinCrowdChant>();
                chant.ConfigureReferences(director, loader, eventHub, source);

                EditorUtility.SetDirty(chant);
                EditorUtility.SetDirty(source);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("WIN_CROWD_CHANT_READY: 0.14 volume, SFX mixer, full-song transition trigger.");
            }
            finally
            {
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
