using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameJam.Gameplay.Tests
{
    public sealed class Issue9GameplayTests
    {
        [Test]
        public void ScreenInputMapping_WAndSMatchVisualBoardDirection()
        {
            Assert.That(
                PuzzleGameplayController.ScreenInputToBoardDirection(Vector2Int.up),
                Is.EqualTo(Vector2Int.down));
            Assert.That(
                PuzzleGameplayController.ScreenInputToBoardDirection(Vector2Int.down),
                Is.EqualTo(Vector2Int.up));
            Assert.That(
                PuzzleGameplayController.ScreenInputToBoardDirection(Vector2Int.left),
                Is.EqualTo(Vector2Int.left));
            Assert.That(
                PuzzleGameplayController.ScreenInputToBoardDirection(Vector2Int.right),
                Is.EqualTo(Vector2Int.right));
        }

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
                BeatColor.Magenta, BeatColor.Magenta, BeatColor.Magenta, BeatColor.Magenta,
                BeatColor.Yellow, BeatColor.Yellow, BeatColor.Yellow, BeatColor.Yellow,
                BeatColor.Blue, BeatColor.Blue, BeatColor.Blue
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
            Assert.That(tracker.MatchedTotal, Is.EqualTo(expected.Length));
            Assert.That(tracker.Resolve(BeatColor.Blue), Is.EqualTo(ObjectiveMatchResult.ChainCompleted));
            Assert.That(tracker.MatchedTotal, Is.EqualTo(expected.Length));
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
                [new Vector2Int(4, 3)] = BeatColor.Blue,
                [new Vector2Int(2, 4)] = BeatColor.Yellow,
                [new Vector2Int(3, 4)] = BeatColor.Blue,
                [new Vector2Int(4, 4)] = BeatColor.Blue
            };
            var directions = new[]
            {
                Vector2Int.down, Vector2Int.left, Vector2Int.up, Vector2Int.up,
                Vector2Int.right, Vector2Int.right, Vector2Int.down, Vector2Int.down,
                Vector2Int.left, Vector2Int.left, Vector2Int.up
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

        [Test]
        public void BeatSequencer_RapidInputsOccupyConsecutiveGridSlots()
        {
            const double timelineStart = 100d;
            const double currentTime = 100.1d;
            var stepDuration = 60d / 130d / 2d;
            var first = TileBeatSequencer.CalculateNextSequenceDspTime(
                timelineStart, currentTime, 130f, 2, 0.03f, 0d);
            var second = TileBeatSequencer.CalculateNextSequenceDspTime(
                timelineStart, currentTime, 130f, 2, 0.03f, first);
            var third = TileBeatSequencer.CalculateNextSequenceDspTime(
                timelineStart, currentTime, 130f, 2, 0.03f, second);

            Assert.That(second, Is.EqualTo(first + stepDuration).Within(0.000001d));
            Assert.That(third, Is.EqualTo(second + stepDuration).Within(0.000001d));
        }

        [Test]
        public void BeatSequencer_LoopRestartsOnEightStepBarBoundary()
        {
            const double timelineStart = 100d;
            var stepDuration = 60d / 130d / 2d;
            var loopDuration = stepDuration * 8d;
            var firstBoundary = TileBeatSequencer.CalculateNextLoopBoundaryDspTime(
                timelineStart, 100.1d, 130f, 2, 8, 0.03f);

            Assert.That(firstBoundary, Is.EqualTo(timelineStart + loopDuration).Within(0.000001d));

            var nearBoundary = timelineStart + loopDuration - 0.01d;
            var skippedBoundary = TileBeatSequencer.CalculateNextLoopBoundaryDspTime(
                timelineStart, nearBoundary, 130f, 2, 8, 0.03f);
            Assert.That(skippedBoundary, Is.EqualTo(timelineStart + loopDuration * 2d).Within(0.000001d));
        }

        [Test]
        public void BeatSequencer_TileFadeUsesSmoothAttackWithoutOvershoot()
        {
            Assert.That(TileBeatSequencer.CalculateFadeInGain(10d, 10d, 0.06f), Is.EqualTo(0f));
            Assert.That(TileBeatSequencer.CalculateFadeInGain(10.03d, 10d, 0.06f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(TileBeatSequencer.CalculateFadeInGain(10.06d, 10d, 0.06f), Is.EqualTo(1f));
            Assert.That(TileBeatSequencer.CalculateFadeInGain(11d, 10d, 0.06f), Is.EqualTo(1f));
        }

        [Test]
        public void BeatSequencer_NewLoopNoteWaitsThenEntersItsNextMusicalSlot()
        {
            const double timelineStart = 100d;
            const double currentTime = 100.2d;
            const double earliestTime = 100.2d + 60d / 130d / 2d;
            var target = TileBeatSequencer.CalculateNextSlotDspTime(
                timelineStart, currentTime, earliestTime, 130f, 4, 16, 6, 0.03f);

            var expectedSlotTime = timelineStart + 6d * (60d / 130d / 4d);
            Assert.That(target, Is.EqualTo(expectedSlotTime).Within(0.000001d));
            Assert.That(target, Is.GreaterThanOrEqualTo(earliestTime));
        }

        [Test]
        public void EarlyBass_UnlocksAtFirstLevelOneStepAboveEightyPercent()
        {
            Assert.That(
                PrototypeMusicDirector.ShouldUnlockBassEarly(8f / 11f, 0.80f, false, false),
                Is.False);
            Assert.That(
                PrototypeMusicDirector.ShouldUnlockBassEarly(9f / 11f, 0.80f, false, false),
                Is.True);
            Assert.That(
                PrototypeMusicDirector.ShouldUnlockBassEarly(9f / 11f, 0.80f, false, true),
                Is.False);
        }

        private static ObjectiveProgressTracker CreateTracker()
        {
            return new ObjectiveProgressTracker(new[]
            {
                new ObjectiveRowDefinition(BeatColor.Magenta, BeatColor.Magenta, BeatColor.Magenta, BeatColor.Magenta),
                new ObjectiveRowDefinition(BeatColor.Yellow, BeatColor.Yellow, BeatColor.Yellow, BeatColor.Yellow),
                new ObjectiveRowDefinition(BeatColor.Blue, BeatColor.Blue, BeatColor.Blue)
            });
        }
    }
}
