using UnityEngine;

namespace GameJam.Gameplay
{
    [CreateAssetMenu(fileName = "StageReactionProfile", menuName = "Game Jam/Stage Reaction Profile")]
    public sealed class StageReactionProfile : ScriptableObject
    {
        [Header("Reaction")]
        [SerializeField, Min(0.01f)] private float transitionSmoothTime = 0.16f;
        [SerializeField, Min(0.01f)] private float beatPulseDuration = 0.20f;
        [SerializeField, Min(0.01f)] private float rowPulseDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float failurePulseDuration = 0.45f;

        [Header("Audience")]
        [SerializeField, Min(0f)] private float audienceBobHeight = 0.08f;
        [SerializeField, Min(0f)] private float audienceSwayAngle = 10f;
        [SerializeField, Min(0f)] private float audienceDanceSpeed = 4.5f;

        [Header("Base Stage Lighting")]
        [SerializeField, Min(0f)] private float heningLightIntensity = 2f;
        [SerializeField, Min(0f)] private float grooveLightIntensity = 3.5f;
        [SerializeField, Min(0f)] private float fullGrooveLightIntensity = 5f;
        [SerializeField, Min(0f)] private float lightBeatPulse = 0.2f;
        [SerializeField, Min(0f)] private float lightRowPulse = 0.45f;
        [SerializeField, Min(0f)] private float lightFailurePulse = 0.35f;

        [Header("Spotlights")]
        [SerializeField, Min(0.01f)] private float spotlightFadeDuration = 0.35f;
        [SerializeField, Min(0f)] private float spotlightIntensity = 8f;
        [SerializeField, Min(0f)] private float spotlightRange = 14f;
        [SerializeField, Range(1f, 179f)] private float spotlightOuterAngle = 32f;
        [SerializeField, Range(0f, 179f)] private float spotlightInnerAngle = 18f;
        [SerializeField, Min(0f)] private float spotlightPanAmplitude = 24f;
        [SerializeField, Min(0f)] private float spotlightTiltAmplitude = 12f;
        [SerializeField, Min(0f)] private float spotlightMovementSpeed = 0.85f;
        [SerializeField, Min(0.1f)] private float spotlightColorCycleDuration = 1.2f;
        [SerializeField] private Color[] spotlightColors =
        {
            new Color(1f, 0.05f, 0.35f),
            new Color(1f, 0.65f, 0.05f),
            new Color(0.05f, 0.25f, 1f)
        };

        [Header("Bloom")]
        [SerializeField, Min(0f)] private float baselineBloom = 0.3f;
        [SerializeField, Min(0f)] private float fullGrooveBloom = 0.75f;

        [Header("Full Groove RGB Filter")]
        [SerializeField, Range(0f, 1f)] private float rgbFilterIntensity = 0.38f;
        [SerializeField, Range(0.5f, 6f)] private float rgbFilterFrequency = 3.2f;
        [SerializeField, Min(0.01f)] private float rgbFilterFadeDuration = 0.15f;
        [SerializeField] private Color[] rgbFilterColors =
        {
            new Color(1f, 0.52f, 0.52f),
            new Color(0.52f, 1f, 0.58f),
            new Color(0.52f, 0.62f, 1f)
        };

        public float TransitionSmoothTime => transitionSmoothTime;
        public float BeatPulseDuration => beatPulseDuration;
        public float RowPulseDuration => rowPulseDuration;
        public float FailurePulseDuration => failurePulseDuration;
        public float AudienceBobHeight => audienceBobHeight;
        public float AudienceSwayAngle => audienceSwayAngle;
        public float AudienceDanceSpeed => audienceDanceSpeed;
        public float HeningLightIntensity => heningLightIntensity;
        public float GrooveLightIntensity => grooveLightIntensity;
        public float FullGrooveLightIntensity => fullGrooveLightIntensity;
        public float LightBeatPulse => lightBeatPulse;
        public float LightRowPulse => lightRowPulse;
        public float LightFailurePulse => lightFailurePulse;
        public float SpotlightFadeDuration => spotlightFadeDuration;
        public float SpotlightIntensity => spotlightIntensity;
        public float SpotlightRange => spotlightRange;
        public float SpotlightOuterAngle => spotlightOuterAngle;
        public float SpotlightInnerAngle => Mathf.Min(spotlightInnerAngle, spotlightOuterAngle);
        public float SpotlightPanAmplitude => spotlightPanAmplitude;
        public float SpotlightTiltAmplitude => spotlightTiltAmplitude;
        public float SpotlightMovementSpeed => spotlightMovementSpeed;
        public float SpotlightColorCycleDuration => spotlightColorCycleDuration;
        public Color[] SpotlightColors => spotlightColors;
        public float BaselineBloom => baselineBloom;
        public float FullGrooveBloom => fullGrooveBloom;
        public float RgbFilterIntensity => rgbFilterIntensity;
        public float RgbFilterFrequency => rgbFilterFrequency;
        public float RgbFilterFadeDuration => rgbFilterFadeDuration;
        public Color[] RgbFilterColors => rgbFilterColors;

        public void ApplyPrototypeDefaults()
        {
            transitionSmoothTime = 0.16f;
            beatPulseDuration = 0.20f;
            rowPulseDuration = 0.35f;
            failurePulseDuration = 0.45f;
            audienceBobHeight = 0.08f;
            audienceSwayAngle = 10f;
            audienceDanceSpeed = 4.5f;
            heningLightIntensity = 2f;
            grooveLightIntensity = 3.5f;
            fullGrooveLightIntensity = 5f;
            lightBeatPulse = 0.2f;
            lightRowPulse = 0.45f;
            lightFailurePulse = 0.35f;
            spotlightFadeDuration = 0.35f;
            spotlightIntensity = 8f;
            spotlightRange = 14f;
            spotlightOuterAngle = 32f;
            spotlightInnerAngle = 18f;
            spotlightPanAmplitude = 24f;
            spotlightTiltAmplitude = 12f;
            spotlightMovementSpeed = 0.85f;
            spotlightColorCycleDuration = 1.2f;
            spotlightColors = new[]
            {
                new Color(1f, 0.05f, 0.35f),
                new Color(1f, 0.65f, 0.05f),
                new Color(0.05f, 0.25f, 1f)
            };
            baselineBloom = 0.3f;
            fullGrooveBloom = 0.75f;
            rgbFilterIntensity = 0.38f;
            rgbFilterFrequency = 3.2f;
            rgbFilterFadeDuration = 0.15f;
            rgbFilterColors = new[]
            {
                new Color(1f, 0.52f, 0.52f),
                new Color(0.52f, 1f, 0.58f),
                new Color(0.52f, 0.62f, 1f)
            };
        }
    }
}
