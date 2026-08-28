using NUnit.Framework;
using UnityEngine;

namespace GameJam.Gameplay.Tests
{
    public sealed class CameraMotionTests
    {
        [Test]
        public void CameraMotionProfile_DefaultZoomCurveHasValidEndpoints()
        {
            var profile = ScriptableObject.CreateInstance<CameraMotionProfile>();
            try
            {
                Assert.That(profile.EvaluateZoom(0f), Is.EqualTo(0f).Within(0.001f));
                Assert.That(profile.EvaluateZoom(1f), Is.EqualTo(1f).Within(0.001f));
                Assert.That(profile.CelebrationBpm, Is.GreaterThan(0f));
                Assert.That(profile.FullGrooveDuration, Is.GreaterThan(1f));
                Assert.That(profile.TileMoveZoomFov, Is.InRange(0.1f, 2f));
                Assert.That(profile.TileMoveZoomDuration, Is.InRange(0.05f, 0.3f));
                Assert.That(profile.TileMoveZoomInRatio, Is.InRange(0.2f, 0.7f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void WinPresentationEvent_IsPublishedOnlyWhenRequested()
        {
            var eventObject = new GameObject("Events");
            try
            {
                var eventHub = eventObject.AddComponent<PuzzleGameplayEvents>();
                var invocationCount = 0;
                eventHub.WinPresentationReady += () => invocationCount++;

                Assert.That(invocationCount, Is.Zero);
                eventHub.PublishWinPresentationReady();
                Assert.That(invocationCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(eventObject);
            }
        }

        [Test]
        public void PlayerMoveStartedEvent_PublishesOrthogonalDirection()
        {
            var eventObject = new GameObject("Events");
            try
            {
                var eventHub = eventObject.AddComponent<PuzzleGameplayEvents>();
                var received = Vector2Int.zero;
                eventHub.PlayerMoveStarted += direction => received = direction;

                eventHub.PublishPlayerMoveStarted(Vector2Int.left);

                Assert.That(received, Is.EqualTo(Vector2Int.left));
            }
            finally
            {
                Object.DestroyImmediate(eventObject);
            }
        }

        [Test]
        public void AnimationPrinciples_ReturnToNeutralWithoutLoopJump()
        {
            Assert.That(CameraAnimationPrinciples.SmootherStep(0f), Is.Zero);
            Assert.That(CameraAnimationPrinciples.SmootherStep(1f), Is.EqualTo(1f));
            Assert.That(CameraAnimationPrinciples.AnticipationAction(0f, 0.16f), Is.Zero.Within(0.001f));
            Assert.That(CameraAnimationPrinciples.AnticipationAction(0.1f, 0.16f), Is.LessThan(0f));
            Assert.That(CameraAnimationPrinciples.AnticipationAction(1f, 0.16f), Is.Zero.Within(0.001f));
        }

        [Test]
        public void AnimationPrinciples_CreateArcAndSoftEnvelope()
        {
            var arc = CameraAnimationPrinciples.EllipticalArc(0f, 1f, 0.5f);
            Assert.That(arc.x, Is.Zero.Within(0.001f));
            Assert.That(arc.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(CameraAnimationPrinciples.FadeEnvelope(0f, 4f, 0.5f, 0.5f), Is.Zero);
            Assert.That(CameraAnimationPrinciples.FadeEnvelope(2f, 4f, 0.5f, 0.5f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(CameraAnimationPrinciples.FadeEnvelope(4f, 4f, 0.5f, 0.5f), Is.Zero);
        }

        [Test]
        public void ChainReset_RestoresCameraRigAndFieldOfView()
        {
            var rig = new GameObject("CameraRig");
            var cameraObject = new GameObject("Main Camera");
            var eventsObject = new GameObject("Events");
            var profile = ScriptableObject.CreateInstance<CameraMotionProfile>();
            try
            {
                cameraObject.transform.SetParent(rig.transform, false);
                var camera = cameraObject.AddComponent<Camera>();
                camera.fieldOfView = 48f;
                rig.transform.localPosition = new Vector3(-0.1f, -3.33f, 0.38f);
                rig.transform.localRotation = Quaternion.Euler(31f, 0f, 0f);
                var baselinePosition = rig.transform.localPosition;
                var baselineRotation = rig.transform.localRotation;
                var eventHub = eventsObject.AddComponent<PuzzleGameplayEvents>();
                var director = rig.AddComponent<PrototypeCameraDirector>();
                director.ConfigureReferences(eventHub, camera, profile);

                rig.transform.localPosition = Vector3.one * 20f;
                rig.transform.localRotation = Quaternion.identity;
                camera.fieldOfView = 70f;
                eventHub.PublishChainReset();

                Assert.That(rig.transform.localPosition, Is.EqualTo(baselinePosition));
                Assert.That(Quaternion.Angle(rig.transform.localRotation, baselineRotation), Is.LessThan(0.001f));
                Assert.That(camera.fieldOfView, Is.EqualTo(48f).Within(0.001f));
                Assert.That(director.State, Is.EqualTo(CameraMotionState.Idle));
            }
            finally
            {
                Object.DestroyImmediate(rig);
                Object.DestroyImmediate(eventsObject);
                Object.DestroyImmediate(profile);
            }
        }
    }
}
