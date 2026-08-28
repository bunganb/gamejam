using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Gameplay.Tests
{
    public sealed class Issue10LevelFlowTests
    {
        private static readonly string[] LevelPaths =
        {
            "Assets/Game/Data/Levels/Level_01_Prototype.asset",
            "Assets/Game/Data/Levels/Level_02.asset",
            "Assets/Game/Data/Levels/Level_03.asset",
            "Assets/Game/Data/Levels/Level_04.asset",
            "Assets/Game/Data/Levels/Level_05.asset",
            "Assets/Game/Data/Levels/Level_06.asset"
        };

        private static readonly string[][] Masks =
        {
            new[] { "111", "111", "111" },
            new[] { "0110", "1111", "1111", "0110" },
            new[] { "11111", "11111", "01000" },
            new[] { "001001", "111111", "111111", "101001" },
            new[] { "01110", "01110", "01110", "11111", "11111" },
            new[] { "00100", "00100", "11111", "11111", "11111", "10001" }
        };

        private static readonly int[][] RowLengths =
        {
            new[] { 1, 1, 1, 1, 4 },
            new[] { 2, 2, 2 },
            new[] { 2, 1, 1, 1, 2, 1 },
            new[] { 1, 2, 3, 4 },
            new[] { 4, 2, 1, 3, 4 },
            new[] { 4, 2, 5 }
        };

        private static readonly Vector2Int[] PlayerStarts =
        {
            new(3, 3),
            new(2, 3),
            new(1, 3),
            new(3, 2),
            new(3, 5),
            new(3, 3)
        };

        private readonly List<Object> disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in disposables)
            {
                if (disposable != null)
                {
                    Object.DestroyImmediate(disposable);
                }
            }

            disposables.Clear();
        }

        [Test]
        public void SixAuthoredLevels_MatchReferenceMasksRowsAndSolutions()
        {
            for (var levelIndex = 0; levelIndex < LevelPaths.Length; levelIndex++)
            {
                var level = LoadLevel(levelIndex);
                Assert.That(level.LevelId, Is.EqualTo($"Level_{levelIndex + 1:00}"));
                Assert.That(level.PlayerStart, Is.EqualTo(PlayerStarts[levelIndex]));
                AssertMask(level, Masks[levelIndex]);
                Assert.That(level.ObjectiveRows.Count, Is.EqualTo(RowLengths[levelIndex].Length));
                for (var row = 0; row < RowLengths[levelIndex].Length; row++)
                {
                    Assert.That(level.ObjectiveRows[row].NoteCount, Is.EqualTo(RowLengths[levelIndex][row]));
                }

                Assert.That(level.ExpectedSolution.Count, Is.EqualTo(level.TotalNotes));
                Assert.That(LevelSolutionValidator.TryValidate(level, out var error), Is.True, error);
            }
        }

        [Test]
        public void Validator_RejectsSolutionThatEntersInactiveTile()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            disposables.Add(level);
            var cells = CreateEmptyCells();
            cells[LevelDefinition.ToIndex(new Vector2Int(3, 3))] = new BoardCellDefinition(true, BeatColor.Magenta);
            level.SetData(
                "Invalid_Level",
                cells,
                new Vector2Int(3, 3),
                new[] { new ObjectiveRowDefinition(BeatColor.Blue) },
                new[] { MoveDirection.Right });

            Assert.That(LevelSolutionValidator.TryValidate(level, out var error), Is.False);
            StringAssert.Contains("inactive/outside", error);
        }

        [Test]
        public void LevelLoader_SwitchesLevelsWithoutReloadingSceneAndEntersEnding()
        {
            var root = new GameObject("Issue10TestRoot");
            disposables.Add(root);
            var board = CreateBoard(root.transform);
            var player = new GameObject("Player").transform;
            player.SetParent(root.transform);
            var eventHub = root.AddComponent<PuzzleGameplayEvents>();
            var controller = root.AddComponent<PuzzleGameplayController>();
            var loader = root.AddComponent<LevelLoader>();
            var levels = new[] { LoadLevel(0), LoadLevel(1) };
            loader.ConfigureReferences(levels, board, player, controller, eventHub, false);
            var sceneHandle = SceneManager.GetActiveScene().handle;

            Assert.That(loader.LoadLevel(0), Is.True);
            Assert.That(board.ActiveTileCount, Is.EqualTo(9));
            Assert.That(loader.LoadNextLevel(), Is.True);
            Assert.That(loader.CurrentLevelIndex, Is.EqualTo(1));
            Assert.That(board.ActiveTileCount, Is.EqualTo(12));
            Assert.That(board.transform.Find("Content").localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(sceneHandle));

            var endingInvocations = 0;
            loader.GameCompleted += () => endingInvocations++;
            Assert.That(loader.LoadNextLevel(), Is.False);
            Assert.That(loader.IsEnding, Is.True);
            Assert.That(endingInvocations, Is.EqualTo(1));
        }

        private static LevelDefinition LoadLevel(int index)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPaths[index]);
            Assert.That(level, Is.Not.Null, $"Missing level asset: {LevelPaths[index]}");
            return level;
        }

        private static void AssertMask(LevelDefinition level, IReadOnlyList<string> mask)
        {
            var width = mask[0].Length;
            var height = mask.Count;
            var offsetX = (LevelDefinition.GridWidth - width) / 2;
            var offsetY = (LevelDefinition.GridHeight - height) / 2;
            for (var y = 0; y < LevelDefinition.GridHeight; y++)
            {
                for (var x = 0; x < LevelDefinition.GridWidth; x++)
                {
                    var localX = x - offsetX;
                    var localY = y - offsetY;
                    var expected = localX >= 0 && localX < width && localY >= 0 && localY < height &&
                                   mask[localY][localX] == '1';
                    Assert.That(level.GetCell(new Vector2Int(x, y)).IsActive, Is.EqualTo(expected),
                        $"{level.LevelId} mask mismatch at ({x},{y}).");
                }
            }
        }

        private static BoardCellDefinition[] CreateEmptyCells()
        {
            var cells = new BoardCellDefinition[LevelDefinition.CellCount];
            for (var index = 0; index < cells.Length; index++)
            {
                cells[index] = new BoardCellDefinition(false, BeatColor.Magenta);
            }

            return cells;
        }

        private static PuzzleBoard CreateBoard(Transform parent)
        {
            var boardObject = new GameObject("Board");
            boardObject.transform.SetParent(parent);
            var content = new GameObject("Content").transform;
            content.SetParent(boardObject.transform);
            var board = boardObject.AddComponent<PuzzleBoard>();
            var slots = new TileSlot[LevelDefinition.CellCount];
            for (var index = 0; index < slots.Length; index++)
            {
                var coordinate = LevelDefinition.ToCoordinate(index);
                var tile = new GameObject($"Tile_{coordinate.y:00}_{coordinate.x:00}");
                tile.transform.SetParent(content);
                var standPoint = new GameObject("PlayerStandPoint").transform;
                standPoint.SetParent(tile.transform);
                var slot = tile.AddComponent<TileSlot>();
                slot.ConfigureReferences(coordinate, null, null, null, standPoint, null);
                slots[index] = slot;
            }

            board.ConfigureReferences(content, slots, 1.1f);
            return board;
        }
    }
}
