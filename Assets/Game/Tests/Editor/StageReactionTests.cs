using NUnit.Framework;
using UnityEngine;

namespace GameJam.Gameplay.Tests
{
    public sealed class StageReactionTests
    {
        [TestCase(0f, StageReactionState.Hening)]
        [TestCase(0.01f, StageReactionState.Groove)]
        [TestCase(0.99f, StageReactionState.Groove)]
        [TestCase(1f, StageReactionState.FullGroove)]
        public void Progress_ResolvesExpectedState(float progress, StageReactionState expected)
        {
            Assert.That(StageReactionMath.ResolveState(progress, false), Is.EqualTo(expected));
        }

        [Test]
        public void RingFill_AdvancesClockwiseAcrossFourSegments()
        {
            Assert.That(StageReactionMath.SegmentFill(0f, 0), Is.Zero);
            Assert.That(StageReactionMath.SegmentFill(0.125f, 0), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(StageReactionMath.SegmentFill(0.25f, 0), Is.EqualTo(1f));
            Assert.That(StageReactionMath.SegmentFill(0.25f, 1), Is.Zero);
            Assert.That(StageReactionMath.SegmentFill(0.625f, 2), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(StageReactionMath.SegmentFill(1f, 3), Is.EqualTo(1f));
        }

        [Test]
        public void GameplayEvents_DriveDirectorAndResetImmediately()
        {
            var eventObject = new GameObject("Events");
            var directorObject = new GameObject("Director");
            var profile = ScriptableObject.CreateInstance<StageReactionProfile>();
            try
            {
                profile.ApplyPrototypeDefaults();
                var eventHub = eventObject.AddComponent<PuzzleGameplayEvents>();
                var director = directorObject.AddComponent<StageReactionDirector>();
                director.ConfigureReferences(eventHub, profile, null, null, null, null);

                var snapshot = new GameplayProgressSnapshot(0, 0, 1, 4, BeatColor.Blue, BeatColor.Blue, Vector2Int.zero);
                eventHub.PublishChainAdvanced(snapshot);
                Assert.That(director.State, Is.EqualTo(StageReactionState.Groove));
                Assert.That(director.TargetProgress, Is.EqualTo(0.25f).Within(0.001f));

                eventHub.PublishChainCompleted(new GameplayProgressSnapshot(0, 3, 4, 4, BeatColor.Magenta, BeatColor.Magenta, Vector2Int.one));
                Assert.That(director.State, Is.EqualTo(StageReactionState.FullGroove));
                Assert.That(director.TargetProgress, Is.EqualTo(1f));

                eventHub.PublishChainReset();
                Assert.That(director.State, Is.EqualTo(StageReactionState.Hening));
                Assert.That(director.TargetProgress, Is.Zero);
                Assert.That(director.VisualProgress, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(eventObject);
                UnityEngine.Object.DestroyImmediate(directorObject);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SpotlightProfile_UsesFourLightFriendlyDefaults()
        {
            var profile = ScriptableObject.CreateInstance<StageReactionProfile>();
            try
            {
                profile.ApplyPrototypeDefaults();
                Assert.That(profile.SpotlightRange, Is.EqualTo(14f));
                Assert.That(profile.SpotlightInnerAngle, Is.LessThan(profile.SpotlightOuterAngle));
                Assert.That(profile.SpotlightFadeDuration, Is.GreaterThan(0f));
                Assert.That(profile.SpotlightColors, Has.Length.EqualTo(3));
                Assert.That(profile.RgbFilterIntensity, Is.InRange(0f, 0.5f));
                Assert.That(profile.RgbFilterFrequency, Is.InRange(0.5f, 6f));
                Assert.That(profile.RgbFilterColors, Has.Length.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
