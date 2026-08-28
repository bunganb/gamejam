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

            loader.ConfigureReferences(levels, board, player, controller, eventHub);
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
                    new[] { 1, 1, 1, 1, 4 },
                    new[]
                    {
                        MoveDirection.Down, MoveDirection.Right, MoveDirection.Up, MoveDirection.Up,
                        MoveDirection.Left, MoveDirection.Left, MoveDirection.Down, MoveDirection.Right
                    },
                    new[,]
                    {
                        { BeatColor.Yellow, BeatColor.Yellow, BeatColor.Blue },
                        { BeatColor.Yellow, BeatColor.Magenta, BeatColor.Yellow },
                        { BeatColor.Blue, BeatColor.Magenta, BeatColor.Magenta }
                    }),
                new LevelSpecification(
                    "Level_02",
                    new[] { "0110", "1111", "1111", "0110" },
                    new Vector2Int(2, 3),
                    new[] { 2, 2, 2 },
                    new[]
                    {
                        MoveDirection.Left, MoveDirection.Down, MoveDirection.Right,
                        MoveDirection.Down, MoveDirection.Right, MoveDirection.Up
                    }),
                new LevelSpecification(
                    "Level_03",
                    new[] { "11111", "11111", "01000" },
                    new Vector2Int(1, 3),
                    new[] { 2, 1, 1, 1, 2, 1 },
                    new[]
                    {
                        MoveDirection.Down, MoveDirection.Right, MoveDirection.Right, MoveDirection.Up,
                        MoveDirection.Left, MoveDirection.Up, MoveDirection.Down, MoveDirection.Left
                    }),
                new LevelSpecification(
                    "Level_04",
                    new[] { "001001", "111111", "111111", "101001" },
                    new Vector2Int(3, 2),
                    new[] { 1, 2, 3, 4 },
                    new[]
                    {
                        MoveDirection.Left, MoveDirection.Down, MoveDirection.Up, MoveDirection.Up,
                        MoveDirection.Left, MoveDirection.Left, MoveDirection.Up, MoveDirection.Down,
                        MoveDirection.Right, MoveDirection.Right
                    }),
                new LevelSpecification(
                    "Level_05",
                    new[] { "01110", "01110", "01110", "11111", "11111" },
                    new Vector2Int(3, 5),
                    new[] { 4, 2, 1, 3, 4 },
                    new[]
                    {
                        MoveDirection.Left, MoveDirection.Left, MoveDirection.Down, MoveDirection.Right,
                        MoveDirection.Down, MoveDirection.Down, MoveDirection.Down, MoveDirection.Right,
                        MoveDirection.Right, MoveDirection.Up, MoveDirection.Up, MoveDirection.Up,
                        MoveDirection.Right, MoveDirection.Up
                    }),
                new LevelSpecification(
                    "Level_06",
                    new[] { "00100", "00100", "11111", "11111", "11111", "10001" },
                    new Vector2Int(3, 3),
                    new[] { 4, 2, 5 },
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
