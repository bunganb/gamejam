using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameJam.Gameplay.Tests
{
    public sealed class PuzzleBoardTests
    {
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
        public void CoordinateMapping_RoundTripsAllCells()
        {
            for (var index = 0; index < LevelDefinition.CellCount; index++)
            {
                Assert.That(LevelDefinition.ToIndex(LevelDefinition.ToCoordinate(index)), Is.EqualTo(index));
            }
        }

        [Test]
        public void LevelDefinition_RejectsInactivePlayerStart()
        {
            var level = CreatePrototypeLevel(false);

            Assert.That(level.TryValidate(out var error), Is.False);
            StringAssert.Contains("must be an active cell", error);
        }

        [Test]
        public void BuildBoard_ActivatesNineTilesAndCalculatesThreeByThreeBounds()
        {
            var level = CreatePrototypeLevel(true);
            var board = CreateBoard();

            board.BuildBoard(level);

            Assert.That(board.ActiveTileCount, Is.EqualTo(9));
            Assert.That(board.GetActiveBounds().size, Is.EqualTo(new Vector3Int(3, 3, 1)));
            Assert.That(board.IsCellActive(new Vector2Int(2, 2)), Is.True);
            Assert.That(board.IsCellActive(new Vector2Int(0, 0)), Is.False);
            Assert.That(board.TryGetTile(new Vector2Int(6, 6), out _), Is.False);
        }

        [Test]
        public void ResetBoard_RestoresInitialColor()
        {
            var level = CreatePrototypeLevel(true);
            var board = CreateBoard();
            board.BuildBoard(level);
            Assert.That(board.TryGetTile(new Vector2Int(2, 2), out var center), Is.True);
            center.SetColor(BeatColor.Blue);

            board.ResetBoard();

            Assert.That(center.CurrentColor, Is.EqualTo(BeatColor.Magenta));
        }

        [Test]
        public void BuildBoard_KeepsDormantTileVisibleAndDisablesItsCollider()
        {
            var level = CreatePrototypeLevel(true);
            var board = CreateBoard();
            board.BuildBoard(level);
            var dormantTile = FindTile(board, Vector2Int.zero);

            Assert.That(dormantTile.IsActiveCell, Is.False);
            Assert.That(dormantTile.transform.Find("VisualRoot").gameObject.activeSelf, Is.True);
            Assert.That(dormantTile.GetComponent<BoxCollider>().enabled, Is.False);
        }

        private LevelDefinition CreatePrototypeLevel(bool activeStart)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            disposables.Add(level);
            var cells = new BoardCellDefinition[LevelDefinition.CellCount];
            for (var index = 0; index < cells.Length; index++)
            {
                cells[index] = new BoardCellDefinition(false, BeatColor.Magenta);
            }

            for (var row = 1; row <= 3; row++)
            {
                for (var column = 1; column <= 3; column++)
                {
                    var coordinate = new Vector2Int(column, row);
                    var isStart = coordinate == new Vector2Int(2, 2);
                    cells[LevelDefinition.ToIndex(coordinate)] = new BoardCellDefinition(!isStart || activeStart, BeatColor.Magenta);
                }
            }

            level.SetData(
                cells,
                new Vector2Int(2, 2),
                new[] { new ObjectiveRowDefinition(BeatColor.Magenta) });
            return level;
        }

        private PuzzleBoard CreateBoard()
        {
            var root = new GameObject("TestBoard");
            disposables.Add(root);
            var content = new GameObject("Content").transform;
            content.SetParent(root.transform);
            var board = root.AddComponent<PuzzleBoard>();
            var slots = new TileSlot[LevelDefinition.CellCount];

            for (var index = 0; index < slots.Length; index++)
            {
                var coordinate = LevelDefinition.ToCoordinate(index);
                var slotObject = new GameObject($"Tile_{coordinate.y:00}_{coordinate.x:00}");
                slotObject.transform.SetParent(content);
                var visual = new GameObject("VisualRoot").transform;
                visual.SetParent(slotObject.transform);
                var standPoint = new GameObject("PlayerStandPoint").transform;
                standPoint.SetParent(slotObject.transform);
                var vfxPoint = new GameObject("VFXPoint").transform;
                vfxPoint.SetParent(slotObject.transform);
                var slot = slotObject.AddComponent<TileSlot>();
                var tileCollider = slotObject.AddComponent<BoxCollider>();
                slot.ConfigureReferences(coordinate, visual, null, tileCollider, standPoint, vfxPoint);
                slots[index] = slot;
            }

            board.ConfigureReferences(content, slots, 1.1f);
            return board;
        }

        private static TileSlot FindTile(PuzzleBoard board, Vector2Int coordinate)
        {
            foreach (var tile in board.GetComponentsInChildren<TileSlot>(true))
            {
                if (tile.Coordinate == coordinate)
                {
                    return tile;
                }
            }

            Assert.Fail($"Tile {coordinate} was not found.");
            return null;
        }
    }
}
