using UnityEngine;

namespace GameJam.Gameplay
{
    public static class LevelSolutionValidator
    {
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

                var index = LevelDefinition.ToIndex(coordinate);
                colors[index] = colors[index].Next();
                if (tracker.Resolve(colors[index]) == ObjectiveMatchResult.Incorrect)
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
