using System;
using System.Collections.Generic;

namespace GameJam.Gameplay
{
    public sealed class ObjectiveProgressTracker
    {
        private readonly IReadOnlyList<ObjectiveRowDefinition> rows;

        public int CurrentRowIndex { get; private set; }
        public int CurrentNoteIndex { get; private set; }
        public int MatchedTotal { get; private set; }
        public int TotalNotes { get; }
        public bool IsComplete { get; private set; }

        public BeatColor ExpectedColor
        {
            get
            {
                if (IsComplete || rows.Count == 0)
                {
                    throw new InvalidOperationException("There is no active objective note.");
                }

                return rows[CurrentRowIndex].Notes[CurrentNoteIndex];
            }
        }

        public ObjectiveProgressTracker(IReadOnlyList<ObjectiveRowDefinition> objectiveRows)
        {
            rows = objectiveRows ?? throw new ArgumentNullException(nameof(objectiveRows));
            var total = 0;
            foreach (var row in rows)
            {
                if (row == null || row.NoteCount == 0)
                {
                    throw new ArgumentException("Every objective row must contain at least one note.", nameof(objectiveRows));
                }

                total += row.NoteCount;
            }

            if (total == 0)
            {
                throw new ArgumentException("At least one objective note is required.", nameof(objectiveRows));
            }

            TotalNotes = total;
            Reset();
        }

        public ObjectiveMatchResult Resolve(BeatColor actualColor)
        {
            if (IsComplete)
            {
                return ObjectiveMatchResult.ChainCompleted;
            }

            if (actualColor != ExpectedColor)
            {
                return ObjectiveMatchResult.Incorrect;
            }

            MatchedTotal++;
            var row = rows[CurrentRowIndex];
            var completedRow = CurrentNoteIndex == row.NoteCount - 1;
            if (!completedRow)
            {
                CurrentNoteIndex++;
                return ObjectiveMatchResult.NoteMatched;
            }

            if (CurrentRowIndex == rows.Count - 1)
            {
                IsComplete = true;
                return ObjectiveMatchResult.ChainCompleted;
            }

            CurrentRowIndex++;
            CurrentNoteIndex = 0;
            return ObjectiveMatchResult.RowCompleted;
        }

        public void Reset()
        {
            CurrentRowIndex = 0;
            CurrentNoteIndex = 0;
            MatchedTotal = 0;
            IsComplete = false;
        }
    }
}
