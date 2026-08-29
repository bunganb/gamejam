using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Game Jam/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public const int GridWidth = 7;
        public const int GridHeight = 7;
        public const int CellCount = GridWidth * GridHeight;

        [SerializeField] private string levelId = "Level_01";
        [SerializeField] private BoardCellDefinition[] cells = CreateEmptyCells();
        [SerializeField] private Vector2Int playerStart = new(3, 3);
        [SerializeField] private ObjectiveRowDefinition[] objectiveRows = System.Array.Empty<ObjectiveRowDefinition>();
        [SerializeField] private MoveDirection[] expectedSolution = System.Array.Empty<MoveDirection>();
        [SerializeField, Min(0f)] private float failFeedbackDuration = 0.75f;
        [SerializeField, Min(0f)] private float completionHoldDuration = 2.5f;

        public string LevelId => levelId;
        public IReadOnlyList<BoardCellDefinition> Cells => cells;
        public Vector2Int PlayerStart => playerStart;
        public IReadOnlyList<ObjectiveRowDefinition> ObjectiveRows => objectiveRows;
        public IReadOnlyList<MoveDirection> ExpectedSolution => expectedSolution;
        public float FailFeedbackDuration => failFeedbackDuration;
        public float CompletionHoldDuration => completionHoldDuration;
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
            SetData(
                levelId,
                sourceCells,
                start,
                sourceObjectives,
                expectedSolution,
                failFeedbackDuration,
                completionHoldDuration);
        }

        public void SetData(
            string id,
            IReadOnlyList<BoardCellDefinition> sourceCells,
            Vector2Int start,
            IReadOnlyList<ObjectiveRowDefinition> sourceObjectives,
            IReadOnlyList<MoveDirection> sourceSolution,
            float failureDuration = 0.75f,
            float completionDuration = 2.5f)
        {
            if (sourceCells == null || sourceCells.Count != CellCount)
            {
                throw new System.ArgumentException($"Level data must contain exactly {CellCount} cells.", nameof(sourceCells));
            }

            levelId = id ?? string.Empty;
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

            if (sourceSolution == null)
            {
                expectedSolution = System.Array.Empty<MoveDirection>();
            }
            else
            {
                expectedSolution = new MoveDirection[sourceSolution.Count];
                for (var index = 0; index < sourceSolution.Count; index++)
                {
                    expectedSolution[index] = sourceSolution[index];
                }
            }

            failFeedbackDuration = Mathf.Max(0f, failureDuration);
            completionHoldDuration = Mathf.Max(0f, completionDuration);
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
            expectedSolution ??= System.Array.Empty<MoveDirection>();
            failFeedbackDuration = Mathf.Max(0f, failFeedbackDuration);
            completionHoldDuration = Mathf.Max(0f, completionHoldDuration);
        }
#endif
    }
}
