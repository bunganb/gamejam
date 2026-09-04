using UnityEngine;

namespace GameJam.Gameplay
{
    [CreateAssetMenu(fileName = "NightclubLightingProfile", menuName = "Game Jam/Nightclub Lighting Profile")]
    public sealed class NightclubLightingProfile : ScriptableObject
    {
        [Header("Palette")]
        [SerializeField] private Color cyan = new(0.02f, 0.9f, 1f, 1f);
        [SerializeField] private Color magenta = new(1f, 0.03f, 0.75f, 1f);
        [SerializeField] private Color blue = new(0.05f, 0.2f, 1f, 1f);
        [SerializeField] private Color green = new(0.05f, 1f, 0.45f, 1f);
        [SerializeField] private Color[] palette =
        {
            new(0.02f, 0.9f, 1f, 1f), new(1f, 0.03f, 0.75f, 1f),
            new(0.05f, 0.2f, 1f, 1f), new(0.05f, 1f, 0.45f, 1f)
        };

        [Header("Neon")]
        [SerializeField, Min(0f)] private float heningEmission = 0.65f;
        [SerializeField, Min(0f)] private float grooveEmission = 1.4f;
        [SerializeField, Min(0f)] private float fullGrooveEmission = 2.2f;
        [SerializeField, Range(0f, 1f)] private float beatPulseAmount = 0.3f;
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.25f;

        [Header("Moving Spotlights")]
        [SerializeField, Min(0f)] private float grooveSpotlightIntensity = 3.25f;
        [SerializeField, Min(0f)] private float fullGrooveSpotlightIntensity = 7f;
        [SerializeField, Min(0f)] private float spotlightRange = 14f;
        [SerializeField, Range(1f, 179f)] private float outerSpotAngle = 34f;
        [SerializeField, Range(0f, 179f)] private float innerSpotAngle = 18f;
        [SerializeField, Min(0f)] private float panAmplitude = 23f;
        [SerializeField, Min(0f)] private float tiltAmplitude = 11f;
        [SerializeField, Min(0f)] private float movementSpeed = 0.55f;
        [SerializeField, Min(0.1f)] private float colorCycleDuration = 1.6f;

        [Header("Strobe")]
        [SerializeField, Min(0.08f)] private float strobeInterval = 0.34f;
        [SerializeField, Range(0.01f, 0.12f)] private float strobeDuration = 0.055f;
        [SerializeField, Min(0f)] private float strobeIntensity = 4.5f;
        [SerializeField] private bool randomStrobeColor = true;

        [Header("Post Processing")]
        [SerializeField, Min(0f)] private float baselineBloom = 0.5f;
        [SerializeField, Min(0f)] private float fullGrooveBloom = 0.7f;
        [SerializeField, Range(0f, 1f)] private float rgbFilterIntensity = 0.22f;

        public Color Cyan => cyan;
        public Color Magenta => magenta;
        public Color Blue => blue;
        public Color Green => green;
        public Color[] Palette => palette;
        public float HeningEmission => heningEmission;
        public float GrooveEmission => grooveEmission;
        public float FullGrooveEmission => fullGrooveEmission;
        public float BeatPulseAmount => beatPulseAmount;
        public float TransitionDuration => transitionDuration;
        public float GrooveSpotlightIntensity => grooveSpotlightIntensity;
        public float FullGrooveSpotlightIntensity => fullGrooveSpotlightIntensity;
        public float SpotlightRange => spotlightRange;
        public float OuterSpotAngle => outerSpotAngle;
        public float InnerSpotAngle => Mathf.Min(innerSpotAngle, outerSpotAngle);
        public float PanAmplitude => panAmplitude;
        public float TiltAmplitude => tiltAmplitude;
        public float MovementSpeed => movementSpeed;
        public float ColorCycleDuration => colorCycleDuration;
        public float StrobeInterval => strobeInterval;
        public float StrobeDuration => strobeDuration;
        public float StrobeIntensity => strobeIntensity;
        public bool RandomStrobeColor => randomStrobeColor;
        public float BaselineBloom => baselineBloom;
        public float FullGrooveBloom => fullGrooveBloom;
        public float RgbFilterIntensity => rgbFilterIntensity;

        public void ApplyPrototypeDefaults()
        {
            cyan = new Color(0.02f, 0.9f, 1f, 1f);
            magenta = new Color(1f, 0.03f, 0.75f, 1f);
            blue = new Color(0.05f, 0.2f, 1f, 1f);
            green = new Color(0.05f, 1f, 0.45f, 1f);
            palette = new[] { cyan, magenta, blue, green };
            heningEmission = 0.65f;
            grooveEmission = 1.4f;
            fullGrooveEmission = 2.2f;
            beatPulseAmount = 0.3f;
            transitionDuration = 0.25f;
            grooveSpotlightIntensity = 3.25f;
            fullGrooveSpotlightIntensity = 7f;
            spotlightRange = 14f;
            outerSpotAngle = 34f;
            innerSpotAngle = 18f;
            panAmplitude = 23f;
            tiltAmplitude = 11f;
            movementSpeed = 0.55f;
            colorCycleDuration = 1.6f;
            strobeInterval = 0.34f;
            strobeDuration = 0.055f;
            strobeIntensity = 4.5f;
            randomStrobeColor = true;
            baselineBloom = 0.5f;
            fullGrooveBloom = 0.7f;
            rgbFilterIntensity = 0.22f;
        }
    }
}
