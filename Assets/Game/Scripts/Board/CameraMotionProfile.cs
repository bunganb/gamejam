using UnityEngine;

namespace GameJam.Gameplay
{
    [CreateAssetMenu(fileName = "CameraMotionProfile", menuName = "Game Jam/Camera Motion Profile")]
    public sealed class CameraMotionProfile : ScriptableObject
    {
        [Header("Idle Sway")]
        [SerializeField] private Vector3 idlePositionAmplitude = new(0.045f, 0f, 0.022f);
        [SerializeField] private Vector3 idleRotationAmplitude = Vector3.zero;
        [SerializeField, Min(0.01f)] private float idleFrequency = 0.28f;
        [SerializeField, Min(1f)] private float grooveSwayMultiplier = 1.8f;
        [SerializeField, Min(0.01f)] private float grooveBlendDuration = 0.7f;
        [SerializeField, Min(0.01f)] private float transformDamping = 0.14f;

        [Header("Groove")]
        [SerializeField, Min(0f)] private float notePulseFov;
        [SerializeField, Min(0.01f)] private float notePulseDuration = 0.14f;
        [SerializeField, Min(0.5f)] private float grooveDanceInterval = 2.4f;
        [SerializeField, Min(0.1f)] private float grooveDanceDuration = 0.85f;
        [SerializeField] private float grooveDancePosition = 0.045f;
        [SerializeField] private float grooveDanceRotation = 0.7f;
        [SerializeField] private float grooveDanceFov;
        [SerializeField, Range(0.05f, 0.4f)] private float grooveAnticipationRatio = 0.16f;
        [SerializeField, Range(0.1f, 1f)] private float grooveArcDepth = 0.55f;
        [SerializeField, Range(0f, 0.25f)] private float followThroughDelay = 0.08f;

        [Header("Failure")]
        [SerializeField] private Vector3 failRotationAmplitude = new(0.35f, 0.45f, 0.8f);
        [SerializeField, Min(0.01f)] private float failShakeDuration = 0.28f;
        [SerializeField, Min(0.1f)] private float failShakeFrequency = 18f;

        [Header("Complete Sequence")]
        [SerializeField, Min(0f)] private float bassFadeInDuration = 0.45f;
        [SerializeField, Min(0f)] private float bassHoldDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float fullSongCrossfadeDuration = 0.8f;
        [SerializeField] private float introPushInFovOffset = -2.5f;
        [SerializeField, Range(40f, 220f)] private float celebrationBpm = 130f;
        [SerializeField, Min(1f)] private float fullGrooveDuration = 4.5f;
        [SerializeField, Min(0.01f)] private float fullGrooveFadeInDuration = 0.6f;
        [SerializeField, Min(0.01f)] private float fullGrooveFadeOutDuration = 0.65f;
        [SerializeField] private float fullGrooveFovAmplitude = 3.2f;
        [SerializeField] private float fullGroovePositionAmplitude = 0.04f;
        [SerializeField] private float fullGrooveRotationAmplitude = 0.9f;
        [SerializeField, Min(0f)] private float winPanelDelay = 0.2f;
        [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public Vector3 IdlePositionAmplitude => idlePositionAmplitude;
        public Vector3 IdleRotationAmplitude => idleRotationAmplitude;
        public float IdleFrequency => idleFrequency;
        public float GrooveSwayMultiplier => grooveSwayMultiplier;
        public float GrooveBlendDuration => grooveBlendDuration;
        public float TransformDamping => transformDamping;
        public float NotePulseFov => notePulseFov;
        public float NotePulseDuration => notePulseDuration;
        public float GrooveDanceInterval => grooveDanceInterval;
        public float GrooveDanceDuration => grooveDanceDuration;
        public float GrooveDancePosition => grooveDancePosition;
        public float GrooveDanceRotation => grooveDanceRotation;
        public float GrooveDanceFov => grooveDanceFov;
        public float GrooveAnticipationRatio => grooveAnticipationRatio;
        public float GrooveArcDepth => grooveArcDepth;
        public float FollowThroughDelay => followThroughDelay;
        public Vector3 FailRotationAmplitude => failRotationAmplitude;
        public float FailShakeDuration => failShakeDuration;
        public float FailShakeFrequency => failShakeFrequency;
        public float BassFadeInDuration => bassFadeInDuration;
        public float BassHoldDuration => bassHoldDuration;
        public float FullSongCrossfadeDuration => fullSongCrossfadeDuration;
        public float IntroPushInFovOffset => introPushInFovOffset;
        public float CelebrationBpm => celebrationBpm;
        public float FullGrooveDuration => fullGrooveDuration;
        public float FullGrooveFadeInDuration => fullGrooveFadeInDuration;
        public float FullGrooveFadeOutDuration => fullGrooveFadeOutDuration;
        public float FullGrooveFovAmplitude => fullGrooveFovAmplitude;
        public float FullGroovePositionAmplitude => fullGroovePositionAmplitude;
        public float FullGrooveRotationAmplitude => fullGrooveRotationAmplitude;
        public float WinPanelDelay => winPanelDelay;

        public void ApplyPrototypeDefaults()
        {
            idlePositionAmplitude = new Vector3(0.045f, 0f, 0.022f);
            idleRotationAmplitude = Vector3.zero;
            idleFrequency = 0.28f;
            grooveSwayMultiplier = 1.8f;
            grooveBlendDuration = 0.7f;
            transformDamping = 0.14f;
            notePulseFov = 0f;
            notePulseDuration = 0.16f;
            grooveDanceInterval = 2.4f;
            grooveDanceDuration = 0.85f;
            grooveDancePosition = 0.045f;
            grooveDanceRotation = 0.7f;
            grooveDanceFov = 0f;
            grooveAnticipationRatio = 0.16f;
            grooveArcDepth = 0.55f;
            followThroughDelay = 0.08f;
            failRotationAmplitude = new Vector3(0.35f, 0.45f, 0.8f);
            failShakeDuration = 0.28f;
            failShakeFrequency = 18f;
            bassFadeInDuration = 0.45f;
            bassHoldDuration = 0.35f;
            fullSongCrossfadeDuration = 0.8f;
            introPushInFovOffset = -2.5f;
            celebrationBpm = 130f;
            fullGrooveDuration = 4.5f;
            fullGrooveFadeInDuration = 0.6f;
            fullGrooveFadeOutDuration = 0.65f;
            fullGrooveFovAmplitude = 3.2f;
            fullGroovePositionAmplitude = 0.04f;
            fullGrooveRotationAmplitude = 0.9f;
            winPanelDelay = 0.2f;
            zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        public float EvaluateZoom(float normalizedTime)
        {
            return zoomCurve == null ? Mathf.Clamp01(normalizedTime) : zoomCurve.Evaluate(Mathf.Clamp01(normalizedTime));
        }
    }
}
