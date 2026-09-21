using System;
using System.Collections.Generic;
using GameJam.Gameplay;
using UnityEditor;
using UnityEngine;

namespace GameJam.Editor
{
    /// <summary>
    /// Rebuilds goal colors from each level's authored route using the global
    /// color-shift rule. Geometry, initial colors, row lengths, timing, and
    /// music note counts remain unchanged.
    /// </summary>
    public static class GlobalShiftLevelObjectiveInstaller
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

        // Earlier levels are intentionally more forgiving. Later levels narrow
        // the valid route space while still allowing player discovery.
        private static readonly int[] TargetSolutionCounts = { 5, 5, 4, 4, 3, 2 };
        private const int SearchAttemptsPerLevel = 250000;

        [MenuItem("Game Jam/Gameplay/Balance Global Shift Routes (2-5)")]
        public static void BalanceRoutes()
        {
            var report = new List<string>();
            for (var levelIndex = 0; levelIndex < LevelPaths.Length; levelIndex++)
            {
                var path = LevelPaths[levelIndex];
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level == null)
                    throw new InvalidOperationException($"Missing level asset: {path}");

                var rowLengths = CopyRowLengths(level);
                var solution = CopySolution(level);
                var targetCount = TargetSolutionCounts[levelIndex];
                var random = new System.Random(0xBCB00 + levelIndex * 7919);
                var cells = FindBalancedColors(level, solution, rowLengths, targetCount, random);
                var rows = BuildRows(cells, level.PlayerStart, solution, rowLengths, level.LevelId);

                level.SetData(
                    level.LevelId,
                    cells,
                    level.PlayerStart,
                    rows,
                    solution,
                    level.FailFeedbackDuration,
                    level.CompletionHoldDuration);

                if (!LevelSolutionValidator.TryValidate(level, out var error))
                    throw new InvalidOperationException($"{level.LevelId} is not solvable after balancing: {error}");

                var actualCount = LevelSolutionValidator.CountValidSolutions(level, 6);
                if (actualCount != targetCount)
                    throw new InvalidOperationException(
                        $"{level.LevelId} expected {targetCount} valid routes but has {actualCount}.");

                EditorUtility.SetDirty(level);
                report.Add($"{level.LevelId}: {actualCount} valid routes, {level.TotalNotes} notes");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GLOBAL_SHIFT_ROUTES_BALANCED\n" + string.Join("\n", report));
        }

        [MenuItem("Game Jam/Gameplay/Rebuild Goals For Global Color Shift")]
        public static void RebuildGoals()
        {
            var report = new List<string>();
            foreach (var path in LevelPaths)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level == null)
                    throw new InvalidOperationException($"Missing level asset: {path}");

                var rowLengths = CopyRowLengths(level);

                var rebuiltRows = BuildRows(level, rowLengths);
                var cells = new BoardCellDefinition[LevelDefinition.CellCount];
                for (var index = 0; index < cells.Length; index++)
                {
                    var cell = level.Cells[index];
                    cells[index] = new BoardCellDefinition(cell.IsActive, cell.InitialColor);
                }

                var solution = CopySolution(level);

                level.SetData(
                    level.LevelId,
                    cells,
                    level.PlayerStart,
                    rebuiltRows,
                    solution,
                    level.FailFeedbackDuration,
                    level.CompletionHoldDuration);

                if (!LevelSolutionValidator.TryValidate(level, out var error))
                    throw new InvalidOperationException($"{level.LevelId} is not solvable after rebuild: {error}");

                EditorUtility.SetDirty(level);
                report.Add($"{level.LevelId}: {level.TotalNotes} notes, authored solution validated");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GLOBAL_SHIFT_LEVELS_READY\n" + string.Join("\n", report));
        }

        private static BoardCellDefinition[] FindBalancedColors(
            LevelDefinition level,
            IReadOnlyList<MoveDirection> solution,
            IReadOnlyList<int> rowLengths,
            int targetCount,
            System.Random random)
        {
            var activeIndices = new List<int>();
            for (var index = 0; index < level.Cells.Count; index++)
            {
                if (level.Cells[index].IsActive)
                    activeIndices.Add(index);
            }

            var palette = new BeatColor[activeIndices.Count];
            for (var index = 0; index < palette.Length; index++)
                palette[index] = (BeatColor)(index % 3);

            for (var attempt = 0; attempt < SearchAttemptsPerLevel; attempt++)
            {
                Shuffle(palette, random);
                var cells = CopyCells(level);
                for (var index = 0; index < activeIndices.Count; index++)
                    cells[activeIndices[index]] = new BoardCellDefinition(true, palette[index]);

                var rows = BuildRows(cells, level.PlayerStart, solution, rowLengths, level.LevelId);
                var candidate = ScriptableObject.CreateInstance<LevelDefinition>();
                try
                {
                    candidate.SetData(level.LevelId, cells, level.PlayerStart, rows, solution);
                    if (LevelSolutionValidator.CountValidSolutions(candidate, targetCount + 1) == targetCount)
                        return cells;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(candidate);
                }
            }

            throw new InvalidOperationException(
                $"Could not find a balanced {targetCount}-route color layout for {level.LevelId} " +
                $"after {SearchAttemptsPerLevel} deterministic attempts.");
        }

        private static ObjectiveRowDefinition[] BuildRows(
            IReadOnlyList<BoardCellDefinition> cells,
            Vector2Int playerStart,
            IReadOnlyList<MoveDirection> solution,
            IReadOnlyList<int> rowLengths,
            string levelId)
        {
            var notes = new List<BeatColor>(solution.Count);
            var coordinate = playerStart;
            for (var step = 0; step < solution.Count; step++)
            {
                coordinate += solution[step].ToVector();
                if (!LevelDefinition.IsInsideGrid(coordinate) ||
                    !cells[LevelDefinition.ToIndex(coordinate)].IsActive)
                {
                    throw new InvalidOperationException($"{levelId} route enters inactive tile {coordinate}.");
                }

                var color = cells[LevelDefinition.ToIndex(coordinate)].InitialColor;
                for (var shift = 0; shift <= step; shift++)
                    color = color.Next();
                notes.Add(color);
            }

            var rows = new ObjectiveRowDefinition[rowLengths.Count];
            var offset = 0;
            for (var row = 0; row < rows.Length; row++)
            {
                var length = rowLengths[row];
                if (length <= 0 || offset + length > notes.Count)
                    throw new InvalidOperationException($"{levelId} row lengths do not match its solution.");

                rows[row] = new ObjectiveRowDefinition(notes.GetRange(offset, length).ToArray());
                offset += length;
            }

            if (offset != notes.Count)
                throw new InvalidOperationException($"{levelId} row lengths leave unused solution steps.");

            return rows;
        }

        private static BoardCellDefinition[] CopyCells(LevelDefinition level)
        {
            var cells = new BoardCellDefinition[LevelDefinition.CellCount];
            for (var index = 0; index < cells.Length; index++)
            {
                var cell = level.Cells[index];
                cells[index] = new BoardCellDefinition(cell.IsActive, cell.InitialColor);
            }

            return cells;
        }

        private static int[] CopyRowLengths(LevelDefinition level)
        {
            var lengths = new int[level.ObjectiveRows.Count];
            for (var row = 0; row < lengths.Length; row++)
                lengths[row] = level.ObjectiveRows[row].NoteCount;
            return lengths;
        }

        private static MoveDirection[] CopySolution(LevelDefinition level)
        {
            var solution = new MoveDirection[level.ExpectedSolution.Count];
            for (var index = 0; index < solution.Length; index++)
                solution[index] = level.ExpectedSolution[index];
            return solution;
        }

        private static void Shuffle<T>(T[] values, System.Random random)
        {
            for (var index = values.Length - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }

        private static ObjectiveRowDefinition[] BuildRows(LevelDefinition level, IReadOnlyList<int> rowLengths)
        {
            var colors = new BeatColor[LevelDefinition.CellCount];
            for (var index = 0; index < colors.Length; index++)
                colors[index] = level.Cells[index].InitialColor;

            var notes = new List<BeatColor>(level.ExpectedSolution.Count);
            var coordinate = level.PlayerStart;
            foreach (var direction in level.ExpectedSolution)
            {
                coordinate += direction.ToVector();
                if (!LevelDefinition.IsInsideGrid(coordinate) || !level.GetCell(coordinate).IsActive)
                    throw new InvalidOperationException($"{level.LevelId} route enters inactive tile {coordinate}.");

                for (var index = 0; index < colors.Length; index++)
                {
                    if (level.Cells[index].IsActive)
                        colors[index] = colors[index].Next();
                }

                notes.Add(colors[LevelDefinition.ToIndex(coordinate)]);
            }

            var rows = new ObjectiveRowDefinition[rowLengths.Count];
            var offset = 0;
            for (var row = 0; row < rows.Length; row++)
            {
                var length = rowLengths[row];
                if (length <= 0 || offset + length > notes.Count)
                    throw new InvalidOperationException($"{level.LevelId} row lengths do not match its solution.");

                rows[row] = new ObjectiveRowDefinition(notes.GetRange(offset, length).ToArray());
                offset += length;
            }

            if (offset != notes.Count)
                throw new InvalidOperationException($"{level.LevelId} row lengths leave unused solution steps.");

            return rows;
        }
    }
}
