using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    public static class LevelSolutionValidator
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        public static bool TryValidate(LevelDefinition level, out string error)
        {
            if (level == null)
            {
                error = "Level is null.";
                return false;
            }

            if (!level.TryValidate(out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(level.LevelId))
            {
                error = "Level ID cannot be empty.";
                return false;
            }

            if (!TryValidateBounds(level, out error))
            {
                return false;
            }

            if (level.ExpectedSolution == null || level.ExpectedSolution.Count != level.TotalNotes)
            {
                error = $"{level.LevelId} expected solution must contain exactly {level.TotalNotes} moves.";
                return false;
            }

            var colors = new BeatColor[LevelDefinition.CellCount];
            for (var index = 0; index < colors.Length; index++)
            {
                colors[index] = level.Cells[index].InitialColor;
            }

            var tracker = new ObjectiveProgressTracker(level.ObjectiveRows);
            var coordinate = level.PlayerStart;
            for (var step = 0; step < level.ExpectedSolution.Count; step++)
            {
                coordinate += level.ExpectedSolution[step].ToVector();
                if (!LevelDefinition.IsInsideGrid(coordinate) || !level.GetCell(coordinate).IsActive)
                {
                    error = $"{level.LevelId} solution step {step + 1} enters inactive/outside cell {coordinate}.";
                    return false;
                }

                for (var colorIndex = 0; colorIndex < colors.Length; colorIndex++)
                {
                    if (level.Cells[colorIndex].IsActive)
                    {
                        colors[colorIndex] = colors[colorIndex].Next();
                    }
                }

                var destinationIndex = LevelDefinition.ToIndex(coordinate);
                if (tracker.Resolve(colors[destinationIndex]) == ObjectiveMatchResult.Incorrect)
                {
                    error = $"{level.LevelId} solution step {step + 1} produces wrong color at {coordinate}.";
                    return false;
                }
            }

            if (!tracker.IsComplete)
            {
                error = $"{level.LevelId} expected solution does not complete every objective row.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Counts distinct movement-input sequences that complete the level.
        /// The count is capped so editor validation cannot overflow or waste time
        /// after it has already proven that a level is too permissive.
        /// </summary>
        public static long CountValidSolutions(LevelDefinition level, long countCap = long.MaxValue)
        {
            if (level == null || countCap < 1 || !level.TryValidate(out _))
            {
                return 0;
            }

            var states = new Dictionary<int, long>
            {
                [LevelDefinition.ToIndex(level.PlayerStart)] = 1
            };
            var step = 0;

            foreach (var row in level.ObjectiveRows)
            {
                foreach (var expectedColor in row.Notes)
                {
                    step++;
                    var nextStates = new Dictionary<int, long>();
                    foreach (var state in states)
                    {
                        var coordinate = LevelDefinition.ToCoordinate(state.Key);
                        foreach (var direction in CardinalDirections)
                        {
                            var destination = coordinate + direction;
                            if (!LevelDefinition.IsInsideGrid(destination))
                            {
                                continue;
                            }

                            var destinationIndex = LevelDefinition.ToIndex(destination);
                            var cell = level.Cells[destinationIndex];
                            if (!cell.IsActive || Advance(cell.InitialColor, step) != expectedColor)
                            {
                                continue;
                            }

                            nextStates.TryGetValue(destinationIndex, out var existingCount);
                            nextStates[destinationIndex] = AddCapped(existingCount, state.Value, countCap);
                        }
                    }

                    states = nextStates;
                    if (states.Count == 0)
                    {
                        return 0;
                    }
                }
            }

            long total = 0;
            foreach (var count in states.Values)
            {
                total = AddCapped(total, count, countCap);
            }

            return total;
        }

        private static BeatColor Advance(BeatColor color, int steps)
        {
            for (var step = 0; step < steps; step++)
            {
                color = color.Next();
            }

            return color;
        }

        private static long AddCapped(long left, long right, long cap)
        {
            return left >= cap - right ? cap : left + right;
        }

        private static bool TryValidateBounds(LevelDefinition level, out string error)
        {
            var minX = LevelDefinition.GridWidth;
            var minY = LevelDefinition.GridHeight;
            var maxX = -1;
            var maxY = -1;

            for (var index = 0; index < level.Cells.Count; index++)
            {
                if (!level.Cells[index].IsActive)
                {
                    continue;
                }

                var coordinate = LevelDefinition.ToCoordinate(index);
                minX = Mathf.Min(minX, coordinate.x);
                minY = Mathf.Min(minY, coordinate.y);
                maxX = Mathf.Max(maxX, coordinate.x);
                maxY = Mathf.Max(maxY, coordinate.y);
            }

            var width = maxX - minX + 1;
            var height = maxY - minY + 1;
            if (width <= 0 || height <= 0)
            {
                error = $"{level.LevelId} must contain active tiles.";
                return false;
            }

            if (width > 6 || height > 6)
            {
                error = $"{level.LevelId} active bounds {width}x{height} exceed reference maximum 6x6.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
