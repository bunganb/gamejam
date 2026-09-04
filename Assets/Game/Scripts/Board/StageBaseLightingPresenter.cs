using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StageBaseLightingPresenter : MonoBehaviour
    {
        [SerializeField] private Light magentaFill;
        [SerializeField] private Light cyanRim;
        [SerializeField] private StageReactionProfile profile;
        [SerializeField] private Color magentaColor = new(1f, 0.14f, 0.56f);
        [SerializeField] private Color cyanColor = new(0.12f, 0.62f, 1f);
        [SerializeField, Range(0f, 2f)] private float rimIntensityRatio = 0.65f;

        public float CurrentIntensity { get; private set; }

        public void ConfigureReferences(Light fill, Light rim, StageReactionProfile reactionProfile)
        {
            magentaFill = fill;
            cyanRim = rim;
            profile = reactionProfile;
            ApplyLightDefaults();
        }

        private void Awake() => ApplyLightDefaults();

        public void Apply(
            StageReactionState state,
            float energy,
            float beatPulse,
            float rowPulse,
            float failurePulse)
        {
            if (profile == null)
            {
                return;
            }

            var progressIntensity = Mathf.Lerp(
                profile.HeningLightIntensity,
                profile.GrooveLightIntensity,
                Mathf.Clamp01(energy));
            if (state == StageReactionState.FullGroove)
            {
                progressIntensity = profile.FullGrooveLightIntensity;
            }

            var pulse = beatPulse * profile.LightBeatPulse + rowPulse * profile.LightRowPulse;
            var failureDip = failurePulse * profile.LightFailurePulse;
            CurrentIntensity = Mathf.Max(0f, progressIntensity + pulse - failureDip);

            ApplyLight(magentaFill, magentaColor, CurrentIntensity);
            ApplyLight(cyanRim, cyanColor, CurrentIntensity * rimIntensityRatio);
        }

        private void ApplyLightDefaults()
        {
            ConfigureLight(magentaFill, magentaColor);
            ConfigureLight(cyanRim, cyanColor);
            if (profile != null)
            {
                Apply(StageReactionState.Hening, 0f, 0f, 0f, 0f);
            }
        }

        private static void ConfigureLight(Light light, Color color)
        {
            if (light == null)
            {
                return;
            }

            light.type = LightType.Point;
            light.color = color;
            light.range = 10f;
            light.shadows = LightShadows.None;
            light.enabled = true;
        }

        private static void ApplyLight(Light light, Color color, float intensity)
        {
            if (light == null)
            {
                return;
            }

            light.color = color;
            light.intensity = intensity;
        }
    }
}
