using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Game Jam/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public const int GridWidth = 6;
        public const int GridHeight = 6;
        public const int CellCount = GridWidth * GridHeight;

        [SerializeField] private BoardCellDefinition[] cells = CreateEmptyCells();
        [SerializeField] private Vector2Int playerStart = new(2, 2);
        [SerializeField] private ObjectiveRowDefinition[] objectiveRows = System.Array.Empty<ObjectiveRowDefinition>();

        public IReadOnlyList<BoardCellDefinition> Cells => cells;
        public Vector2Int PlayerStart => playerStart;
        public IReadOnlyList<ObjectiveRowDefinition> ObjectiveRows => objectiveRows;
        public int TotalNotes
        {
            get
            {
                var total = 0;
                if (objectiveRows == null)
                {
                    return total;
                }

                foreach (var row in objectiveRows)
                {
                    total += row?.NoteCount ?? 0;
                }

                return total;
            }
        }

        public BoardCellDefinition GetCell(Vector2Int coordinate)
        {
            return cells[ToIndex(coordinate)];
        }

        public void SetData(IReadOnlyList<BoardCellDefinition> sourceCells, Vector2Int start)
        {
            SetData(sourceCells, start, objectiveRows);
        }

        public void SetData(
            IReadOnlyList<BoardCellDefinition> sourceCells,
            Vector2Int start,
            IReadOnlyList<ObjectiveRowDefinition> sourceObjectives)
        {
            if (sourceCells == null || sourceCells.Count != CellCount)
            {
                throw new System.ArgumentException($"Level data must contain exactly {CellCount} cells.", nameof(sourceCells));
            }

            cells = new BoardCellDefinition[CellCount];
            for (var index = 0; index < CellCount; index++)
            {
                var source = sourceCells[index] ?? new BoardCellDefinition(false, BeatColor.Magenta);
                cells[index] = new BoardCellDefinition(source.IsActive, source.InitialColor);
            }

            playerStart = start;
            if (sourceObjectives == null)
            {
                objectiveRows = System.Array.Empty<ObjectiveRowDefinition>();
            }
            else
            {
                objectiveRows = new ObjectiveRowDefinition[sourceObjectives.Count];
                for (var index = 0; index < sourceObjectives.Count; index++)
                {
                    objectiveRows[index] = sourceObjectives[index]?.Copy();
                }
            }
        }

        public bool TryValidate(out string error)
        {
            if (cells == null || cells.Length != CellCount)
            {
                error = $"Level data must contain exactly {CellCount} cells.";
                return false;
            }

            for (var index = 0; index < cells.Length; index++)
            {
                if (cells[index] == null)
                {
                    error = $"Cell {index} is null.";
                    return false;
                }
            }

            if (!IsInsideGrid(playerStart))
            {
                error = $"Player start {playerStart} is outside the {GridWidth}x{GridHeight} board.";
                return false;
            }

            if (!GetCell(playerStart).IsActive)
            {
                error = $"Player start {playerStart} must be an active cell.";
                return false;
            }

            if (objectiveRows == null || objectiveRows.Length == 0)
            {
                error = "Level must contain at least one objective row.";
                return false;
            }

            for (var index = 0; index < objectiveRows.Length; index++)
            {
                if (objectiveRows[index] == null || objectiveRows[index].NoteCount == 0)
                {
                    error = $"Objective row {index} must contain at least one note.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public static bool IsInsideGrid(Vector2Int coordinate)
        {
            return coordinate.x >= 0 && coordinate.x < GridWidth &&
                   coordinate.y >= 0 && coordinate.y < GridHeight;
        }

        public static int ToIndex(Vector2Int coordinate)
        {
            if (!IsInsideGrid(coordinate))
            {
                throw new System.ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is outside the board.");
            }

            return coordinate.y * GridWidth + coordinate.x;
        }

        public static Vector2Int ToCoordinate(int index)
        {
            if (index < 0 || index >= CellCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), index, "Index is outside the board.");
            }

            return new Vector2Int(index % GridWidth, index / GridWidth);
        }

        private static BoardCellDefinition[] CreateEmptyCells()
        {
            var result = new BoardCellDefinition[CellCount];
            for (var index = 0; index < CellCount; index++)
            {
                result[index] = new BoardCellDefinition(false, BeatColor.Magenta);
            }

            return result;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cells == null || cells.Length != CellCount)
            {
                var previous = cells;
                cells = CreateEmptyCells();
                if (previous != null)
                {
                    var copyCount = Mathf.Min(previous.Length, cells.Length);
                    for (var index = 0; index < copyCount; index++)
                    {
                        if (previous[index] != null)
                        {
                            cells[index] = previous[index];
                        }
                    }
                }
            }

            objectiveRows ??= System.Array.Empty<ObjectiveRowDefinition>();
        }
#endif
    }
}
