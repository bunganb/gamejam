using System;
using System.Collections.Generic;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class Issue10LevelFlowInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string LevelFolder = "Assets/Game/Data/Levels";
        private const string MusicFolder = "Assets/Game/Data/Music";
        private const string GameLevelFolder = "Assets/Game/Data/GameLevels";
        private static readonly string[] LevelPaths =
        {
            LevelFolder + "/Level_01_Prototype.asset",
            LevelFolder + "/Level_02.asset",
            LevelFolder + "/Level_03.asset",
            LevelFolder + "/Level_04.asset",
            LevelFolder + "/Level_05.asset",
            LevelFolder + "/Level_06.asset"
        };

        [MenuItem("Game Jam/Install Issue 10 Level Flow")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before installing Issue 10.");
            }

            var levels = BuildLevels();
            var musicProfiles = BuildMusicProfiles(levels);
            var gameLevels = BuildGameLevels(levels, musicProfiles);
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            var systems = FindRoot(scene, "Systems");
            var board = FindComponent<PuzzleBoard>(scene);
            var player = FindTransform(scene, "PrototypePlayer");
            var controller = FindComponent<PuzzleGameplayController>(scene);
            var eventHub = FindComponent<PuzzleGameplayEvents>(scene);
            var bootstrap = FindComponent<GameplayPrototypeBootstrap>(scene);
            var cameraRig = FindTransform(scene, "CameraRig");
            var mainCamera = FindComponent<Camera>(scene);
            if (systems == null || board == null || player == null || controller == null || eventHub == null || bootstrap == null)
            {
                throw new InvalidOperationException("GameplayPrototype is missing required Issue 10 references.");
            }

            var cameraRigPosition = cameraRig != null ? cameraRig.localPosition : Vector3.zero;
            var cameraRigRotation = cameraRig != null ? cameraRig.localRotation : Quaternion.identity;
            var cameraPosition = mainCamera != null ? mainCamera.transform.localPosition : Vector3.zero;
            var cameraRotation = mainCamera != null ? mainCamera.transform.localRotation : Quaternion.identity;
            var cameraFov = mainCamera != null ? mainCamera.fieldOfView : 0f;

            var loader = systems.GetComponent<LevelLoader>();
            if (loader == null)
            {
                loader = systems.AddComponent<LevelLoader>();
            }

            loader.ConfigureReferences(gameLevels, board, player, controller, eventHub);
            var musicDirector = FindComponent<PrototypeMusicDirector>(scene);
            musicDirector?.ConfigureMusicCatalog(loader, musicProfiles);
            FindComponent<TileBeatSequencer>(scene)?.ConfigureMusicCatalog(loader, musicProfiles);
            bootstrap.enabled = false;
            controller.ConfigureReferences(levels[0], board, player, eventHub);
            board.BuildBoard(levels[0]);
            player.position = board.GetPlayerStandPosition(levels[0].PlayerStart);

            AssertCameraUnchanged(cameraRig, cameraRigPosition, cameraRigRotation, mainCamera, cameraPosition, cameraRotation, cameraFov);

            EditorUtility.SetDirty(loader);
            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            if (openedAdditively)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("ISSUE10_LEVEL_FLOW_INSTALL_COMPLETE: six validated levels, runtime loader, restart, next-level, and ending flow installed.");
        }

        private static LevelDefinition[] BuildLevels()
        {
            var specifications = new[]
            {
                new LevelSpecification(
                    "Level_01",
                    new[] { "111", "111", "111" },
                    new Vector2Int(3, 3),
                    new[] { 4, 4, 3 },
                    new[]
                    {
                        MoveDirection.Down, MoveDirection.Left, MoveDirection.Up, MoveDirection.Up,
                        MoveDirection.Right, MoveDirection.Right, MoveDirection.Down, MoveDirection.Down,
                        MoveDirection.Left, MoveDirection.Left, MoveDirection.Up
                    },
                    new[,]
                    {
                        { BeatColor.Yellow, BeatColor.Yellow, BeatColor.Blue },
                        { BeatColor.Yellow, BeatColor.Magenta, BeatColor.Blue },
                        { BeatColor.Yellow, BeatColor.Blue, BeatColor.Blue }
                    }),
                new LevelSpecification(
                    "Level_02",
                    new[] { "0110", "1111", "1111", "0110" },
                    new Vector2Int(2, 3),
                    new[] { 4, 3, 2 },
                    new[]
                    {
                        MoveDirection.Left, MoveDirection.Down, MoveDirection.Right,
                        MoveDirection.Down, MoveDirection.Right, MoveDirection.Up,
                        MoveDirection.Up, MoveDirection.Right, MoveDirection.Down
                    }),
                new LevelSpecification(
                    "Level_03",
                    new[] { "11111", "11111", "01000" },
                    new Vector2Int(1, 3),
                    new[] { 4, 4, 3 },
                    new[]
                    {
                        MoveDirection.Down, MoveDirection.Right, MoveDirection.Right, MoveDirection.Right,
                        MoveDirection.Right, MoveDirection.Up, MoveDirection.Left, MoveDirection.Left,
                        MoveDirection.Left, MoveDirection.Up, MoveDirection.Down
                    }),
                new LevelSpecification(
                    "Level_04",
                    new[] { "001001", "111111", "111111", "101001" },
                    new Vector2Int(3, 2),
                    new[] { 4, 3, 1, 2 },
                    new[]
                    {
                        MoveDirection.Right, MoveDirection.Right, MoveDirection.Up, MoveDirection.Up,
                        MoveDirection.Down, MoveDirection.Left, MoveDirection.Left, MoveDirection.Down,
                        MoveDirection.Left, MoveDirection.Left
                    }),
                new LevelSpecification(
                    "Level_05",
                    new[] { "01110", "01110", "01110", "11111", "11111" },
                    new Vector2Int(3, 5),
                    new[] { 4, 2, 2, 2 },
                    new[]
                    {
                        MoveDirection.Left, MoveDirection.Left, MoveDirection.Down, MoveDirection.Right,
                        MoveDirection.Down, MoveDirection.Down, MoveDirection.Down, MoveDirection.Right,
                        MoveDirection.Right, MoveDirection.Up
                    }),
                new LevelSpecification(
                    "Level_06",
                    new[] { "00100", "00100", "11111", "11111", "11111", "10001" },
                    new Vector2Int(3, 3),
                    new[] { 4, 4, 2, 1 },
                    new[]
                    {
                        MoveDirection.Down, MoveDirection.Down, MoveDirection.Down, MoveDirection.Up,
                        MoveDirection.Up, MoveDirection.Left, MoveDirection.Left, MoveDirection.Up,
                        MoveDirection.Up, MoveDirection.Up, MoveDirection.Down
                    })
            };

            var levels = new LevelDefinition[specifications.Length];
            for (var index = 0; index < specifications.Length; index++)
            {
                levels[index] = BuildLevel(index, specifications[index]);
            }

            return levels;
        }

        private static LevelMusicDefinition[] BuildMusicProfiles(IReadOnlyList<LevelDefinition> levels)
        {
            EnsureAssetFolder("Assets/Game/Data", "Music");
            var profiles = new[]
            {
                BuildMusicProfile(
                    1, 130f, 2, 8,
                    "Level_1/1_Harmony.wav", null, 1f,
                    "Level_1/5_BassGuitar.wav", null, -1,
                    "Level_1/6_FullSong.wav",
                    new[]
                    {
                        "Magenta_Kick.wav", "Magenta_Kick.wav", "Magenta_Kick.wav", "Magenta_Kick.wav",
                        "Yellow_Ketipung1.wav", "Yellow_Ketipung1.wav", "Yellow_Ketipung1.wav", "Yellow_Ketipung1_Accent.wav",
                        "Blue_Ketipung2.wav", "Blue_Ketipung2_Accent.wav", "Blue_Ketipung2_Accent.wav"
                    },
                    new[] { 0, 3, 5, 6, 1, 3, 5, 7, 2, 4, 6 },
                    new[] { .72f, .72f, .72f, .72f, .52f, .52f, .52f, .9f, .52f, .9f, .9f }),
                BuildMusicProfile(
                    2, 130f, 4, 16,
                    "Level_2/1_HARMONY.wav", null, 1f,
                    "Level_2/5_BASS GUITAR & KETIPUNG 3.wav", null, -1,
                    "Level_2/6_FULLSONG.wav",
                    GeneratedSamples(2, 9),
                    new[] { 0, 6, 10, 12, 2, 8, 14, 4, 11 },
                    new[] { .72f, .72f, .72f, .72f, .9f, .52f, .72f, .82f, .82f }),
                BuildMusicProfile(
                    3, 129.915f, 4, 16,
                    "Level_3/1_HARMONY.wav", null, 1f,
                    "Level_3/5_HARMONY 2.wav", null, -1,
                    "Level_3/6_FULLSONG.wav",
                    GeneratedSamples(3, 11),
                    new[] { 0, 6, 10, 12, 2, 6, 10, 14, 4, 9, 12 },
                    new[] { .72f, .72f, .72f, .72f, .88f, .55f, .88f, .55f, .75f, .9f, .75f }),
                BuildMusicProfile(
                    4, 129.915f, 4, 16,
                    "Level_4/1_HARMONY.wav", null, 1f,
                    "Level_4/6_HARMONY 2.wav", null, -1,
                    "Level_4/7_FULL SONG.wav",
                    GeneratedSamples(4, 10),
                    new[] { 0, 6, 10, 12, 2, 6, 14, 8, 4, 12 },
                    new[] { .72f, .72f, .72f, .72f, .82f, .82f, .82f, .9f, .78f, .78f }),
                BuildMusicProfile(
                    5, 130f, 4, 16,
                    "Level_5/1_HARMONY.wav", null, 1f,
                    "Level_5/6_HARMONY 2.wav", null, -1,
                    "Level_5/7_FULL SONG.wav",
                    GeneratedSamples(5, 10),
                    new[] { 0, 6, 10, 12, 2, 6, 4, 12, 8, 14 },
                    new[] { .72f, .72f, .72f, .72f, .82f, .82f, .88f, .55f, .9f, .72f }),
                BuildMusicProfile(
                    6, 130f, 4, 16,
                    "Level_6/1_HARMONY.wav", "Level_6/2_HARMONY 2.wav", .7f,
                    "Level_6/7_BASS GUITAR.wav", "Level_6/6_SNARE & HIHAT.wav", 3,
                    "Level_6/8_FULL SONG.wav",
                    GeneratedSamples(6, 11),
                    new[] { 0, 6, 10, 12, 2, 6, 8, 14, 4, 12, 8 },
                    new[] { .72f, .72f, .72f, .72f, .68f, .68f, .9f, .62f, .88f, .55f, .9f })
            };

            for (var index = 0; index < profiles.Length; index++)
            {
                if (!profiles[index].TryValidate(levels[index].TotalNotes, out var error))
                {
                    throw new InvalidOperationException(error);
                }
            }

            return profiles;
        }

        private static GameLevelDefinition[] BuildGameLevels(
            IReadOnlyList<LevelDefinition> levels,
            IReadOnlyList<LevelMusicDefinition> musicProfiles)
        {
            EnsureAssetFolder("Assets/Game/Data", "GameLevels");
            var gameLevels = new GameLevelDefinition[levels.Count];
            for (var index = 0; index < gameLevels.Length; index++)
            {
                var path = $"{GameLevelFolder}/GameLevel_{index + 1:00}.asset";
                var gameLevel = AssetDatabase.LoadAssetAtPath<GameLevelDefinition>(path);
                if (gameLevel == null)
                {
                    gameLevel = ScriptableObject.CreateInstance<GameLevelDefinition>();
                    AssetDatabase.CreateAsset(gameLevel, path);
                }

                gameLevels[index] = gameLevel;
            }

            for (var index = 0; index < gameLevels.Length; index++)
            {
                var nextLevel = index + 1 < gameLevels.Length ? gameLevels[index + 1] : null;
                gameLevels[index].SetData($"Level {index + 1:00}", levels[index], musicProfiles[index], nextLevel);
                if (!gameLevels[index].TryValidate(out var error))
                {
                    throw new InvalidOperationException(error);
                }

                EditorUtility.SetDirty(gameLevels[index]);
            }

            return gameLevels;
        }

        private static LevelMusicDefinition BuildMusicProfile(
            int levelNumber,
            float bpm,
            int subdivisions,
            int loopSteps,
            string harmonyPath,
            string secondaryPath,
            float secondaryThreshold,
            string buildPath,
            string topLoopPath,
            int topLoopRow,
            string fullSongPath,
            IReadOnlyList<string> samplePaths,
            IReadOnlyList<int> slots,
            IReadOnlyList<float> volumes)
        {
            var assetPath = $"{MusicFolder}/LevelMusic_{levelNumber:00}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<LevelMusicDefinition>(assetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<LevelMusicDefinition>();
                AssetDatabase.CreateAsset(profile, assetPath);
            }

            var samples = new AudioClip[samplePaths.Count];
            for (var index = 0; index < samples.Length; index++)
            {
                var sampleRoot = levelNumber == 1
                    ? "Assets/Game/Art/Beat/OneShots/"
                    : $"Assets/Game/Art/Beat/OneShots/Level_{levelNumber:00}/";
                samples[index] = RequireAudioClip(sampleRoot + samplePaths[index]);
            }

            profile.SetData(
                $"Level_{levelNumber:00}",
                bpm,
                subdivisions,
                loopSteps,
                RequireAudioClip("Assets/Game/Art/Beat/" + harmonyPath),
                OptionalAudioClip(secondaryPath),
                secondaryThreshold,
                OptionalAudioClip(buildPath),
                .8f,
                OptionalAudioClip(topLoopPath),
                topLoopRow,
                RequireAudioClip("Assets/Game/Art/Beat/" + fullSongPath),
                samples,
                slots,
                volumes);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static string[] GeneratedSamples(int levelNumber, int count)
        {
            var folder = $"Assets/Game/Art/Beat/OneShots/Level_{levelNumber:00}";
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            var paths = new List<string>(guids.Length);
            foreach (var guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            paths.Sort(StringComparer.Ordinal);
            if (paths.Count != count)
            {
                throw new InvalidOperationException($"Level {levelNumber} requires {count} generated one-shots, found {paths.Count}.");
            }

            var names = new string[count];
            for (var index = 0; index < count; index++)
            {
                names[index] = paths[index].Substring(paths[index].LastIndexOf('/') + 1);
            }

            return names;
        }

        private static AudioClip OptionalAudioClip(string relativePath)
        {
            return string.IsNullOrWhiteSpace(relativePath)
                ? null
                : RequireAudioClip("Assets/Game/Art/Beat/" + relativePath);
        }

        private static AudioClip RequireAudioClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                throw new InvalidOperationException($"Required audio clip was not found: {path}");
            }

            return clip;
        }

        private static void EnsureAssetFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static LevelDefinition BuildLevel(int index, LevelSpecification specification)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPaths[index]);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(level, LevelPaths[index]);
            }

            var cells = BuildCells(index, specification);
            var objectives = BuildObjectives(cells, specification.PlayerStart, specification.Solution, specification.RowLengths);
            level.SetData(
                specification.LevelId,
                cells,
                specification.PlayerStart,
                objectives,
                specification.Solution,
                0.75f,
                2.5f);

            if (!LevelSolutionValidator.TryValidate(level, out var error))
            {
                throw new InvalidOperationException(error);
            }

            EditorUtility.SetDirty(level);
            return level;
        }

        private static BoardCellDefinition[] BuildCells(int levelIndex, LevelSpecification specification)
        {
            var cells = new BoardCellDefinition[LevelDefinition.CellCount];
            for (var index = 0; index < cells.Length; index++)
            {
                cells[index] = new BoardCellDefinition(false, BeatColor.Magenta);
            }

            var height = specification.Mask.Length;
            var width = specification.Mask[0].Length;
            var offsetX = (LevelDefinition.GridWidth - width) / 2;
            var offsetY = (LevelDefinition.GridHeight - height) / 2;
            for (var row = 0; row < height; row++)
            {
                if (specification.Mask[row].Length != width)
                {
                    throw new InvalidOperationException($"{specification.LevelId} mask rows have inconsistent width.");
                }

                for (var column = 0; column < width; column++)
                {
                    if (specification.Mask[row][column] != '1')
                    {
                        continue;
                    }

                    var coordinate = new Vector2Int(offsetX + column, offsetY + row);
                    var color = specification.InitialColors != null
                        ? specification.InitialColors[row, column]
                        : (BeatColor)((coordinate.x + coordinate.y * 2 + levelIndex) % 3);
                    cells[LevelDefinition.ToIndex(coordinate)] = new BoardCellDefinition(true, color);
                }
            }

            return cells;
        }

        private static ObjectiveRowDefinition[] BuildObjectives(
            IReadOnlyList<BoardCellDefinition> cells,
            Vector2Int start,
            IReadOnlyList<MoveDirection> solution,
            IReadOnlyList<int> rowLengths)
        {
            var runtimeColors = new BeatColor[LevelDefinition.CellCount];
            for (var index = 0; index < runtimeColors.Length; index++)
            {
                runtimeColors[index] = cells[index].InitialColor;
            }

            var notes = new List<BeatColor>(solution.Count);
            var coordinate = start;
            foreach (var direction in solution)
            {
                coordinate += direction.ToVector();
                if (!LevelDefinition.IsInsideGrid(coordinate) || !cells[LevelDefinition.ToIndex(coordinate)].IsActive)
                {
                    throw new InvalidOperationException($"Authored solution enters inactive cell {coordinate}.");
                }

                var cellIndex = LevelDefinition.ToIndex(coordinate);
                runtimeColors[cellIndex] = runtimeColors[cellIndex].Next();
                notes.Add(runtimeColors[cellIndex]);
            }

            var rows = new ObjectiveRowDefinition[rowLengths.Count];
            var noteOffset = 0;
            for (var row = 0; row < rowLengths.Count; row++)
            {
                var length = rowLengths[row];
                if (length <= 0 || noteOffset + length > notes.Count)
                {
                    throw new InvalidOperationException("Objective row lengths do not match expected solution length.");
                }

                rows[row] = new ObjectiveRowDefinition(notes.GetRange(noteOffset, length).ToArray());
                noteOffset += length;
            }

            if (noteOffset != notes.Count)
            {
                throw new InvalidOperationException("Objective row lengths do not consume every authored solution note.");
            }

            return rows;
        }

        private static void AssertCameraUnchanged(
            Transform rig,
            Vector3 rigPosition,
            Quaternion rigRotation,
            Camera camera,
            Vector3 cameraPosition,
            Quaternion cameraRotation,
            float cameraFov)
        {
            var rigChanged = rig != null &&
                             (rig.localPosition != rigPosition || Quaternion.Angle(rig.localRotation, rigRotation) > 0.0001f);
            var cameraChanged = camera != null &&
                                (camera.transform.localPosition != cameraPosition ||
                                 Quaternion.Angle(camera.transform.localRotation, cameraRotation) > 0.0001f ||
                                 !Mathf.Approximately(camera.fieldOfView, cameraFov));
            if (rigChanged || cameraChanged)
            {
                throw new InvalidOperationException("Camera changed during Issue 10 install; scene was not saved.");
            }
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
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

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private sealed class LevelSpecification
        {
            public LevelSpecification(
                string levelId,
                string[] mask,
                Vector2Int playerStart,
                int[] rowLengths,
                MoveDirection[] solution,
                BeatColor[,] initialColors = null)
            {
                LevelId = levelId;
                Mask = mask;
                PlayerStart = playerStart;
                RowLengths = rowLengths;
                Solution = solution;
                InitialColors = initialColors;
            }

            public string LevelId { get; }
            public string[] Mask { get; }
            public Vector2Int PlayerStart { get; }
            public int[] RowLengths { get; }
            public MoveDirection[] Solution { get; }
            public BeatColor[,] InitialColors { get; }
        }
    }
}
