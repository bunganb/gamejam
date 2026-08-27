using UnityEngine;

namespace GameJam.Gameplay
{
    public readonly struct GameplayProgressSnapshot
    {
        public int ObjectiveRowIndex { get; }
        public int NoteIndex { get; }
        public int MatchedTotal { get; }
        public int TotalNotes { get; }
        public float NormalizedProgress { get; }
        public BeatColor ActualColor { get; }
        public BeatColor ExpectedColor { get; }
        public Vector2Int TileCoordinate { get; }

        public GameplayProgressSnapshot(
            int objectiveRowIndex,
            int noteIndex,
            int matchedTotal,
            int totalNotes,
            BeatColor actualColor,
            BeatColor expectedColor,
            Vector2Int tileCoordinate)
        {
            ObjectiveRowIndex = objectiveRowIndex;
            NoteIndex = noteIndex;
            MatchedTotal = matchedTotal;
            TotalNotes = totalNotes;
            NormalizedProgress = totalNotes > 0 ? (float)matchedTotal / totalNotes : 0f;
            ActualColor = actualColor;
            ExpectedColor = expectedColor;
            TileCoordinate = tileCoordinate;
        }
    }
}
