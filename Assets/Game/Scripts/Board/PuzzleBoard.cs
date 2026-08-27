using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PuzzleBoard : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private TileSlot[] slots = new TileSlot[LevelDefinition.CellCount];
        [SerializeField, Min(0.01f)] private float gridSpacing = 1.1f;

        private readonly TileSlot[] indexedSlots = new TileSlot[LevelDefinition.CellCount];
        private LevelDefinition activeLevel;
        private BoundsInt activeBounds;
        private Vector3 contentOffset;

        public LevelDefinition ActiveLevel => activeLevel;
        public int ActiveTileCount { get; private set; }

        public void ConfigureReferences(Transform newContentRoot, TileSlot[] newSlots, float newGridSpacing)
        {
            contentRoot = newContentRoot;
            slots = newSlots;
            gridSpacing = newGridSpacing;
            RebuildIndex();
        }

        public void BuildBoard(LevelDefinition level)
        {
            if (level == null)
            {
                throw new System.ArgumentNullException(nameof(level));
            }

            if (!level.TryValidate(out var error))
            {
                throw new System.InvalidOperationException(error);
            }

            RebuildIndex();
            activeLevel = level;
            ActiveTileCount = 0;

            for (var index = 0; index < LevelDefinition.CellCount; index++)
            {
                var definition = level.Cells[index];
                indexedSlots[index].ApplyCellDefinition(definition);
                if (definition.IsActive)
                {
                    ActiveTileCount++;
                }
            }

            activeBounds = CalculateActiveBounds(level.Cells);
            contentOffset = CalculateContentOffset(activeBounds);
            if (contentRoot != null)
            {
                contentRoot.localPosition = contentOffset;
            }
        }

        public void ResetBoard()
        {
            if (activeLevel == null)
            {
                return;
            }

            for (var index = 0; index < LevelDefinition.CellCount; index++)
            {
                indexedSlots[index].ApplyCellDefinition(activeLevel.Cells[index]);
            }

            if (contentRoot != null)
            {
                contentRoot.localPosition = contentOffset;
            }
        }

        public bool IsCellActive(Vector2Int coordinate)
        {
            return TryGetTile(coordinate, out _);
        }

        public bool TryGetTile(Vector2Int coordinate, out TileSlot tile)
        {
            tile = null;
            if (!LevelDefinition.IsInsideGrid(coordinate))
            {
                return false;
            }

            var candidate = indexedSlots[LevelDefinition.ToIndex(coordinate)];
            if (candidate == null || !candidate.IsActiveCell)
            {
                return false;
            }

            tile = candidate;
            return true;
        }

        public BoundsInt GetActiveBounds()
        {
            return activeBounds;
        }

        public Vector3 GetWorldPosition(Vector2Int coordinate)
        {
            if (!TryGetTile(coordinate, out var tile))
            {
                throw new System.ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is not an active tile.");
            }

            return tile.transform.position;
        }

        public Vector3 GetPlayerStandPosition(Vector2Int coordinate)
        {
            if (!TryGetTile(coordinate, out var tile))
            {
                throw new System.ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is not an active tile.");
            }

            return tile.PlayerStandPoint != null ? tile.PlayerStandPoint.position : tile.transform.position;
        }

        private void RebuildIndex()
        {
            if (slots == null || slots.Length != LevelDefinition.CellCount)
            {
                throw new System.InvalidOperationException($"PuzzleBoard requires exactly {LevelDefinition.CellCount} TileSlot references.");
            }

            System.Array.Clear(indexedSlots, 0, indexedSlots.Length);
            foreach (var slot in slots)
            {
                if (slot == null)
                {
                    throw new System.InvalidOperationException("PuzzleBoard contains a null TileSlot reference.");
                }

                var index = LevelDefinition.ToIndex(slot.Coordinate);
                if (indexedSlots[index] != null)
                {
                    throw new System.InvalidOperationException($"Duplicate TileSlot coordinate {slot.Coordinate}.");
                }

                indexedSlots[index] = slot;
            }
        }

        private static BoundsInt CalculateActiveBounds(IReadOnlyList<BoardCellDefinition> definitions)
        {
            var minX = LevelDefinition.GridWidth;
            var minY = LevelDefinition.GridHeight;
            var maxX = -1;
            var maxY = -1;

            for (var index = 0; index < definitions.Count; index++)
            {
                if (!definitions[index].IsActive)
                {
                    continue;
                }

                var coordinate = LevelDefinition.ToCoordinate(index);
                minX = Mathf.Min(minX, coordinate.x);
                minY = Mathf.Min(minY, coordinate.y);
                maxX = Mathf.Max(maxX, coordinate.x);
                maxY = Mathf.Max(maxY, coordinate.y);
            }

            if (maxX < minX || maxY < minY)
            {
                return new BoundsInt();
            }

            return new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
        }

        private Vector3 CalculateContentOffset(BoundsInt bounds)
        {
            if (bounds.size.x == 0 || bounds.size.y == 0)
            {
                return Vector3.zero;
            }

            var activeCenterX = bounds.xMin + (bounds.size.x - 1) * 0.5f;
            var activeCenterY = bounds.yMin + (bounds.size.y - 1) * 0.5f;
            var boardCenter = (LevelDefinition.GridWidth - 1) * 0.5f;
            var halfSpacing = gridSpacing * 0.5f;
            var offsetX = Mathf.Clamp((boardCenter - activeCenterX) * gridSpacing, -halfSpacing, halfSpacing);
            var offsetZ = Mathf.Clamp((activeCenterY - boardCenter) * gridSpacing, -halfSpacing, halfSpacing);
            return new Vector3(offsetX, 0f, offsetZ);
        }
    }
}
