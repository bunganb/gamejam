using System.Collections;
using UnityEngine;

namespace GameJam.Gameplay
{
    public enum CameraMotionState
    {
        Idle,
        Groove,
        FailFeedback,
        FullGroove,
        WinPresented
    }

    [DisallowMultipleComponent]
    public sealed class PrototypeCameraDirector : MonoBehaviour
    {
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private CameraMotionProfile profile;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private float baseFieldOfView;
        private Vector3 reactionPositionOffset;
        private Vector3 reactionRotationOffset;
        private float reactionFovOffset;
        private float progress;
        private float grooveBlend;
        private float grooveBlendVelocity;
        private Vector3 ambientPositionOffset;
        private Vector3 ambientPositionVelocity;
        private Vector3 ambientRotationOffset;
        private Vector3 ambientRotationVelocity;
        private float ambientFovOffset;
        private float ambientFovVelocity;
        private float noiseSeed;
        private Coroutine reactionRoutine;
        private Coroutine notePulseRoutine;
        private bool baselineCaptured;

        public CameraMotionState State { get; private set; } = CameraMotionState.Idle;

        public void ConfigureReferences(PuzzleGameplayEvents eventHub, Camera camera, CameraMotionProfile motionProfile)
        {
            Unsubscribe();
            gameplayEvents = eventHub;
            targetCamera = camera;
            profile = motionProfile;
            CaptureBaseline();
            Subscribe();
        }

        private void Awake()
        {
            noiseSeed = Random.Range(10f, 1000f);
            CaptureBaseline();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreImmediately();
        }

        private void LateUpdate()
        {
            if (!baselineCaptured || profile == null || targetCamera == null)
            {
                return;
            }

            var targetGrooveBlend = progress <= 0f ? 0f : Mathf.Lerp(0.35f, 1f, progress);
            grooveBlend = Mathf.SmoothDamp(
                grooveBlend,
                targetGrooveBlend,
                ref grooveBlendVelocity,
                profile.GrooveBlendDuration,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            var phase = Time.unscaledTime * profile.IdleFrequency * Mathf.PI * 2f + noiseSeed;
            var swayStrength = Mathf.Lerp(1f, profile.GrooveSwayMultiplier, grooveBlend);

            var primaryArc = CameraAnimationPrinciples.EllipticalArc(phase, 1f, 0.72f);
            var secondaryArc = CameraAnimationPrinciples.EllipticalArc(phase * 0.47f + 1.1f, 0.22f, 0.5f);
            var positionWave = new Vector3(
                primaryArc.x + secondaryArc.x,
                0f,
                primaryArc.y + secondaryArc.y);
            var rotationWave = new Vector3(
                Mathf.Sin(phase * 0.61f + 0.8f),
                Mathf.Sin(phase * 0.83f + 2.1f),
                Mathf.Sin(phase * 0.52f + 3.4f));

            EvaluateGrooveDance(out var groovePositionOffset, out var grooveRotationOffset, out var grooveFovOffset);

            var desiredAmbientPosition =
                Vector3.Scale(positionWave, profile.IdlePositionAmplitude) * swayStrength + groovePositionOffset;
            var desiredAmbientRotation =
                Vector3.Scale(rotationWave, profile.IdleRotationAmplitude) * swayStrength + grooveRotationOffset;
            ambientPositionOffset = Vector3.SmoothDamp(
                ambientPositionOffset,
                desiredAmbientPosition,
                ref ambientPositionVelocity,
                profile.TransformDamping,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            ambientRotationOffset = Vector3.SmoothDamp(
                ambientRotationOffset,
                desiredAmbientRotation,
                ref ambientRotationVelocity,
                profile.TransformDamping,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            ambientFovOffset = Mathf.SmoothDamp(
                ambientFovOffset,
                grooveFovOffset,
                ref ambientFovVelocity,
                profile.TransformDamping,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            transform.localPosition = baseLocalPosition +
                                      ambientPositionOffset + reactionPositionOffset;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(
                ambientRotationOffset + reactionRotationOffset);
            targetCamera.fieldOfView = Mathf.Clamp(baseFieldOfView + ambientFovOffset + reactionFovOffset, 25f, 80f);
        }

        public void RestoreImmediately()
        {
            StopMotionCoroutines();
            progress = 0f;
            grooveBlend = 0f;
            grooveBlendVelocity = 0f;
            ambientPositionOffset = Vector3.zero;
            ambientPositionVelocity = Vector3.zero;
            ambientRotationOffset = Vector3.zero;
            ambientRotationVelocity = Vector3.zero;
            ambientFovOffset = 0f;
            ambientFovVelocity = 0f;
            State = CameraMotionState.Idle;
            reactionPositionOffset = Vector3.zero;
            reactionRotationOffset = Vector3.zero;
            reactionFovOffset = 0f;

            if (!baselineCaptured)
            {
                return;
            }

            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = baseFieldOfView;
            }
        }

        private void CaptureBaseline()
        {
            if (targetCamera == null || profile == null)
            {
                return;
            }

            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            baseFieldOfView = targetCamera.fieldOfView;
            baselineCaptured = true;
        }

        private void Subscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            Unsubscribe();
            gameplayEvents.ChainAdvanced += HandleChainAdvanced;
            gameplayEvents.ChainFailed += HandleChainFailed;
            gameplayEvents.ChainReset += HandleChainReset;
            gameplayEvents.ChainCompleted += HandleChainCompleted;
        }

        private void Unsubscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            gameplayEvents.ChainAdvanced -= HandleChainAdvanced;
            gameplayEvents.ChainFailed -= HandleChainFailed;
            gameplayEvents.ChainReset -= HandleChainReset;
            gameplayEvents.ChainCompleted -= HandleChainCompleted;
        }

        private void HandleChainAdvanced(GameplayProgressSnapshot snapshot)
        {
            progress = snapshot.NormalizedProgress;
            State = CameraMotionState.Groove;
            if (profile.NotePulseFov <= 0f)
            {
                return;
            }

            if (notePulseRoutine != null)
            {
                StopCoroutine(notePulseRoutine);
            }

            notePulseRoutine = StartCoroutine(NotePulse());
        }

        private void HandleChainFailed(GameplayProgressSnapshot snapshot)
        {
            StartReaction(FailShake());
        }

        private void HandleChainReset()
        {
            RestoreImmediately();
        }

        private void HandleChainCompleted(GameplayProgressSnapshot snapshot)
        {
            progress = 1f;
            StartReaction(CompleteSequence());
        }

        private IEnumerator NotePulse()
        {
            var elapsed = 0f;
            var duration = profile.NotePulseDuration;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                reactionFovOffset = -Mathf.Sin(normalized * Mathf.PI) * profile.NotePulseFov;
                yield return null;
            }

            reactionFovOffset = 0f;
            notePulseRoutine = null;
        }

        private IEnumerator FailShake()
        {
            State = CameraMotionState.FailFeedback;
            var elapsed = 0f;
            var duration = profile.FailShakeDuration;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var strength = 1f - profile.EvaluateZoom(normalized);
                var phase = elapsed * profile.FailShakeFrequency;
                var noise = new Vector3(
                    SignedNoise(phase, noiseSeed + 131f),
                    SignedNoise(phase, noiseSeed + 151f),
                    SignedNoise(phase, noiseSeed + 173f));
                reactionRotationOffset = Vector3.Scale(noise, profile.FailRotationAmplitude) * strength;
                yield return null;
            }

            reactionRotationOffset = Vector3.zero;
            State = progress > 0f ? CameraMotionState.Groove : CameraMotionState.Idle;
            reactionRoutine = null;
        }

        private IEnumerator CompleteSequence()
        {
            State = CameraMotionState.FullGroove;
            yield return AnimateFov(0f, profile.IntroPushInFovOffset, profile.BassFadeInDuration);

            if (profile.BassHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(profile.BassHoldDuration);
            }

            yield return AnimateFov(profile.IntroPushInFovOffset, 0f, profile.FullSongCrossfadeDuration);

            var elapsed = 0f;
            var beatDuration = 60f / profile.CelebrationBpm;
            while (elapsed < profile.FullGrooveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var envelope = CameraAnimationPrinciples.FadeEnvelope(
                    elapsed,
                    profile.FullGrooveDuration,
                    profile.FullGrooveFadeInDuration,
                    profile.FullGrooveFadeOutDuration);
                var phase = elapsed / beatDuration * Mathf.PI * 2f;
                var danceArc = CameraAnimationPrinciples.EllipticalArc(
                    phase * 0.5f,
                    profile.FullGroovePositionAmplitude,
                    profile.GrooveArcDepth);
                var followThroughPhase = phase - profile.FollowThroughDelay * Mathf.PI * 2f;

                reactionFovOffset = Mathf.Sin(phase) * profile.FullGrooveFovAmplitude * envelope;
                reactionPositionOffset.x = danceArc.x * envelope;
                reactionPositionOffset.y = 0f;
                reactionPositionOffset.z = danceArc.y * envelope;
                reactionRotationOffset.y = Mathf.Sin(phase * 0.5f) * profile.FullGrooveRotationAmplitude * 0.45f * envelope;
                reactionRotationOffset.z = Mathf.Sin(followThroughPhase) * profile.FullGrooveRotationAmplitude * envelope;
                yield return null;
            }

            reactionPositionOffset = Vector3.zero;
            reactionRotationOffset = Vector3.zero;
            reactionFovOffset = 0f;
            if (profile.WinPanelDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(profile.WinPanelDelay);
            }

            State = CameraMotionState.WinPresented;
            gameplayEvents.PublishWinPresentationReady();
            reactionRoutine = null;
        }

        private void EvaluateGrooveDance(
            out Vector3 positionOffset,
            out Vector3 rotationOffset,
            out float fovOffset)
        {
            positionOffset = Vector3.zero;
            rotationOffset = Vector3.zero;
            fovOffset = 0f;
            if (State != CameraMotionState.Groove || grooveBlend <= 0.01f)
            {
                return;
            }

            var cycle = Mathf.Repeat(Time.unscaledTime + noiseSeed, profile.GrooveDanceInterval);
            if (cycle >= profile.GrooveDanceDuration)
            {
                return;
            }

            var normalized = cycle / profile.GrooveDanceDuration;
            var action = CameraAnimationPrinciples.AnticipationAction(normalized, profile.GrooveAnticipationRatio);
            var actionTime = Mathf.Clamp01(
                (normalized - profile.GrooveAnticipationRatio) / (1f - profile.GrooveAnticipationRatio));
            var actionEnvelope = Mathf.Sin(actionTime * Mathf.PI) * grooveBlend;
            var arc = CameraAnimationPrinciples.EllipticalArc(
                actionTime * Mathf.PI * 2f,
                profile.GrooveDancePosition,
                profile.GrooveArcDepth);

            positionOffset.x = action * profile.GrooveDancePosition * grooveBlend;
            positionOffset.y = 0f;
            positionOffset.z = arc.y * actionEnvelope;

            var delayedNormalized = Mathf.Clamp01(
                (normalized - profile.FollowThroughDelay) / (1f - profile.FollowThroughDelay));
            var delayedAction = CameraAnimationPrinciples.AnticipationAction(
                delayedNormalized,
                profile.GrooveAnticipationRatio);
            rotationOffset.y = action * profile.GrooveDanceRotation * 0.3f * grooveBlend;
            rotationOffset.z = delayedAction * profile.GrooveDanceRotation * grooveBlend;
            fovOffset = -Mathf.Sin(normalized * Mathf.PI) * profile.GrooveDanceFov * actionEnvelope;
        }

        private IEnumerator AnimateFov(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                reactionFovOffset = to;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = profile.EvaluateZoom(elapsed / duration);
                reactionFovOffset = Mathf.LerpUnclamped(from, to, normalized);
                yield return null;
            }

            reactionFovOffset = to;
        }

        private void StartReaction(IEnumerator routine)
        {
            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
            }

            if (notePulseRoutine != null)
            {
                StopCoroutine(notePulseRoutine);
                notePulseRoutine = null;
            }

            reactionFovOffset = 0f;
            reactionPositionOffset = Vector3.zero;
            reactionRotationOffset = Vector3.zero;
            reactionRoutine = StartCoroutine(routine);
        }

        private void StopMotionCoroutines()
        {
            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
                reactionRoutine = null;
            }

            if (notePulseRoutine != null)
            {
                StopCoroutine(notePulseRoutine);
                notePulseRoutine = null;
            }
        }

        private static float SignedNoise(float time, float seed)
        {
            return Mathf.PerlinNoise(time, seed) * 2f - 1f;
        }
    }
}
