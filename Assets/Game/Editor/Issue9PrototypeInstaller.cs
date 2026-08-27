using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class Issue9PrototypeInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string LevelPath = "Assets/Game/Data/Levels/Level_01_Prototype.asset";
        private const string BeatRoot = "Assets/Game/Art/Beat/Prototype/";

        [MenuItem("Game Jam/Install Issue 9 Gameplay")]
        public static void Install()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            if (level == null)
            {
                throw new System.InvalidOperationException($"Level asset was not found at {LevelPath}.");
            }

            var objectives = new[]
            {
                new ObjectiveRowDefinition(BeatColor.Magenta),
                new ObjectiveRowDefinition(BeatColor.Yellow),
                new ObjectiveRowDefinition(BeatColor.Magenta),
                new ObjectiveRowDefinition(BeatColor.Blue),
                new ObjectiveRowDefinition(BeatColor.Blue, BeatColor.Yellow, BeatColor.Magenta, BeatColor.Blue)
            };
            level.SetData(level.Cells, level.PlayerStart, objectives);
            EditorUtility.SetDirty(level);

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            var systems = FindRoot(scene, "Systems");
            var board = FindComponent<PuzzleBoard>(scene);
            var player = FindTransform(scene, "PrototypePlayer");
            if (systems == null || board == null || player == null)
            {
                throw new System.InvalidOperationException("GameplayPrototype is missing Systems, PuzzleBoard, or PrototypePlayer.");
            }

            var eventHub = GetOrAdd<PuzzleGameplayEvents>(systems);
            var controller = GetOrAdd<PuzzleGameplayController>(systems);
            controller.ConfigureReferences(level, board, player, eventHub);
            var hud = GetOrAdd<PrototypeObjectiveHud>(systems);
            hud.ConfigureReferences(controller, eventHub);

            var musicRoot = systems.transform.Find("PrototypeMusic");
            if (musicRoot == null)
            {
                musicRoot = new GameObject("PrototypeMusic").transform;
                musicRoot.SetParent(systems.transform, false);
            }

            var harmony = EnsureAudioLayer(musicRoot, "Harmony_01", BeatRoot + "1_Harmony.wav");
            var drumKick = EnsureAudioLayer(musicRoot, "DrumKick_02", BeatRoot + "2_DrumKick.wav");
            var ketipungOne = EnsureAudioLayer(musicRoot, "Ketipung1_03", BeatRoot + "3_Ketipung1.wav");
            var ketipungTwo = EnsureAudioLayer(musicRoot, "Ketipung2_04", BeatRoot + "4_Ketipung2.wav");
            var bass = EnsureAudioLayer(musicRoot, "BassGuitar_05", BeatRoot + "5_BassGuitar.wav");
            var fullSong = EnsureAudioLayer(musicRoot, "FullSong_06", BeatRoot + "6_FullSong.wav");
            var musicDirector = GetOrAdd<PrototypeMusicDirector>(musicRoot.gameObject);
            musicDirector.ConfigureReferences(eventHub, harmony, drumKick, ketipungOne, ketipungTwo, bass, fullSong);

            var bootstrap = GetOrAdd<GameplayPrototypeBootstrap>(systems);
            bootstrap.ConfigureReferences(level, board, player, controller);
            board.BuildBoard(level);
            player.position = board.GetPlayerStandPosition(level.PlayerStart);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (openedAdditively)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("ISSUE9_PROTOTYPE_INSTALL_COMPLETE: movement, objectives, full reset, and synchronized music layers installed.");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name)
                    {
                        return transform;
                    }
                }
            }

            return null;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();
        }

        private static AudioSource EnsureAudioLayer(Transform parent, string name, string clipPath)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }

            var source = GetOrAdd<AudioSource>(child.gameObject);
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
