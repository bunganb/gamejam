using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameJam.Gameplay.Tests
{
    public sealed class Issue9GameplayTests
    {
        [Test]
        public void BeatColor_CyclesMagentaBlueYellowMagenta()
        {
            Assert.That(BeatColor.Magenta.Next(), Is.EqualTo(BeatColor.Blue));
            Assert.That(BeatColor.Blue.Next(), Is.EqualTo(BeatColor.Yellow));
            Assert.That(BeatColor.Yellow.Next(), Is.EqualTo(BeatColor.Magenta));
        }

        [Test]
        public void TileSlot_AdvanceColorUsesRuntimeColorOnRevisit()
        {
            var tileObject = new GameObject("Tile");
            try
            {
                var tile = tileObject.AddComponent<TileSlot>();
                tile.ConfigureReferences(Vector2Int.zero, null, null, null, null, null);
                tile.ApplyCellDefinition(new BoardCellDefinition(true, BeatColor.Magenta));

                Assert.That(tile.AdvanceColor(), Is.EqualTo(BeatColor.Blue));
                Assert.That(tile.AdvanceColor(), Is.EqualTo(BeatColor.Yellow));
                Assert.That(tile.CurrentColor, Is.EqualTo(BeatColor.Yellow));
            }
            finally
            {
                Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void ObjectiveTracker_WrongColorDoesNotAdvanceAndResetClearsProgress()
        {
            var tracker = CreateTracker();

            Assert.That(tracker.Resolve(BeatColor.Blue), Is.EqualTo(ObjectiveMatchResult.Incorrect));
            Assert.That(tracker.MatchedTotal, Is.Zero);
            Assert.That(tracker.CurrentRowIndex, Is.Zero);
            tracker.Resolve(BeatColor.Magenta);
            tracker.Reset();

            Assert.That(tracker.MatchedTotal, Is.Zero);
            Assert.That(tracker.CurrentRowIndex, Is.Zero);
            Assert.That(tracker.CurrentNoteIndex, Is.Zero);
            Assert.That(tracker.IsComplete, Is.False);
        }

        [Test]
        public void ObjectiveTracker_MovesAcrossRowsAndCompletesOnce()
        {
            var tracker = CreateTracker();
            var expected = new[]
            {
                BeatColor.Magenta, BeatColor.Yellow, BeatColor.Magenta, BeatColor.Blue,
                BeatColor.Blue, BeatColor.Yellow, BeatColor.Magenta, BeatColor.Blue
            };

            for (var index = 0; index < expected.Length; index++)
            {
                var result = tracker.Resolve(expected[index]);
                if (index == expected.Length - 1)
                {
                    Assert.That(result, Is.EqualTo(ObjectiveMatchResult.ChainCompleted));
                }
            }

            Assert.That(tracker.IsComplete, Is.True);
            Assert.That(tracker.MatchedTotal, Is.EqualTo(8));
            Assert.That(tracker.Resolve(BeatColor.Blue), Is.EqualTo(ObjectiveMatchResult.ChainCompleted));
            Assert.That(tracker.MatchedTotal, Is.EqualTo(8));
        }

        [Test]
        public void PrototypeSolution_UsesNewTileColorAndCompletesAllObjectives()
        {
            var tracker = CreateTracker();
            var colors = new Dictionary<Vector2Int, BeatColor>
            {
                [new Vector2Int(2, 2)] = BeatColor.Yellow,
                [new Vector2Int(3, 2)] = BeatColor.Yellow,
                [new Vector2Int(4, 2)] = BeatColor.Blue,
                [new Vector2Int(2, 3)] = BeatColor.Yellow,
                [new Vector2Int(3, 3)] = BeatColor.Magenta,
                [new Vector2Int(4, 3)] = BeatColor.Yellow,
                [new Vector2Int(2, 4)] = BeatColor.Blue,
                [new Vector2Int(3, 4)] = BeatColor.Magenta,
                [new Vector2Int(4, 4)] = BeatColor.Magenta
            };
            var directions = new[]
            {
                Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.up,
                Vector2Int.left, Vector2Int.left, Vector2Int.down, Vector2Int.right
            };
            var coordinate = new Vector2Int(3, 3);

            foreach (var direction in directions)
            {
                coordinate += direction;
                colors[coordinate] = colors[coordinate].Next();
                Assert.That(tracker.Resolve(colors[coordinate]), Is.Not.EqualTo(ObjectiveMatchResult.Incorrect));
            }

            Assert.That(tracker.IsComplete, Is.True);
        }

        [Test]
        public void MusicGrid_QuantizesToNextEighthNoteAt130Bpm()
        {
            const double timelineStart = 100d;
            var firstGrid = PrototypeMusicDirector.CalculateNextGridDspTime(
                timelineStart, 100.1d, 130f, 2, 0.01f);
            var stepDuration = 60d / 130d / 2d;
            Assert.That(firstGrid, Is.EqualTo(timelineStart + stepDuration).Within(0.000001d));

            var nearGrid = timelineStart + stepDuration - 0.005d;
            var skippedGrid = PrototypeMusicDirector.CalculateNextGridDspTime(
                timelineStart, nearGrid, 130f, 2, 0.01f);
            Assert.That(skippedGrid, Is.EqualTo(timelineStart + stepDuration * 2d).Within(0.000001d));
        }

        private static ObjectiveProgressTracker CreateTracker()
        {
            return new ObjectiveProgressTracker(new[]
            {
                new ObjectiveRowDefinition(BeatColor.Magenta),
                new ObjectiveRowDefinition(BeatColor.Yellow),
                new ObjectiveRowDefinition(BeatColor.Magenta),
                new ObjectiveRowDefinition(BeatColor.Blue),
                new ObjectiveRowDefinition(BeatColor.Blue, BeatColor.Yellow, BeatColor.Magenta, BeatColor.Blue)
            });
        }
    }
}
