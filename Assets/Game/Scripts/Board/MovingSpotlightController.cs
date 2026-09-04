using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MovingSpotlightController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform[] panPivots = Array.Empty<Transform>();
        [SerializeField] private Transform[] tiltPivots = Array.Empty<Transform>();
        [SerializeField] private Light[] spotlights = Array.Empty<Light>();
        [SerializeField] private Renderer[] beamRenderers = Array.Empty<Renderer>();
        [SerializeField] private NightclubLightingProfile profile;

        private Quaternion[] basePanRotations = Array.Empty<Quaternion>();
        private Quaternion[] baseTiltRotations = Array.Empty<Quaternion>();
        private float targetWeight;
        private float currentWeight;
        private int activeLightCount;
        private MaterialPropertyBlock beamPropertyBlock;

        public int SpotlightCount => spotlights?.Length ?? 0;
        public float CurrentWeight => currentWeight;

        public void Configure(Transform[] pans, Transform[] tilts, Light[] lights, NightclubLightingProfile lightingProfile)
        {
            Configure(pans, tilts, lights, null, lightingProfile);
        }

        public void Configure(Transform[] pans, Transform[] tilts, Light[] lights, Renderer[] beams,
            NightclubLightingProfile lightingProfile)
        {
            panPivots = pans ?? Array.Empty<Transform>();
            tiltPivots = tilts ?? Array.Empty<Transform>();
            spotlights = lights ?? Array.Empty<Light>();
            beamRenderers = beams ?? Array.Empty<Renderer>();
            profile = lightingProfile;
            CacheBaselines();
            ApplyDefaults();
            ResetImmediately();
        }

        public void SetTarget(float weight, int enabledLightCount)
        {
            targetWeight = Mathf.Clamp01(weight);
            activeLightCount = Mathf.Clamp(enabledLightCount, 0, SpotlightCount);
            if (targetWeight > 0f && currentWeight <= 0f)
                currentWeight = Mathf.Min(targetWeight, 0.05f);
            if (profile != null) Animate(Time.unscaledTime);
        }

        public void ResetImmediately()
        {
            currentWeight = targetWeight = 0f;
            activeLightCount = 0;
            for (var i = 0; i < SpotlightCount; i++)
                if (spotlights[i] != null) { spotlights[i].enabled = false; spotlights[i].intensity = 0f; }
            foreach (var beam in beamRenderers)
                if (beam != null) beam.enabled = false;
            RestorePivots();
        }

        private void Awake()
        {
            CacheBaselines();
            ApplyDefaults();
            ResetImmediately();
        }

        private void Update()
        {
            if (profile == null) return;
            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, profile.TransitionDuration));
            Animate(Time.unscaledTime);
        }

        private void CacheBaselines()
        {
            basePanRotations = Capture(panPivots);
            baseTiltRotations = Capture(tiltPivots);
        }

        private void ApplyDefaults()
        {
            if (profile == null || spotlights == null) return;
            foreach (var light in spotlights)
            {
                if (light == null) continue;
                light.type = LightType.Spot;
                light.range = profile.SpotlightRange;
                light.spotAngle = profile.OuterSpotAngle;
                light.innerSpotAngle = profile.InnerSpotAngle;
                light.shadows = LightShadows.None;
            }
        }

        private void Animate(float time)
        {
            var palette = profile.Palette;
            for (var i = 0; i < SpotlightCount; i++)
            {
                var light = spotlights[i];
                if (light == null) continue;
                var enabledWeight = i < activeLightCount ? currentWeight : 0f;
                var phase = i * Mathf.PI * 0.5f;
                var motion = time * profile.MovementSpeed * Mathf.PI * 2f;
                var pan = Mathf.Sin(motion + phase) * profile.PanAmplitude * enabledWeight;
                var tilt = Mathf.Sin(motion * 0.73f + phase * 1.31f) * profile.TiltAmplitude * enabledWeight;
                if (i < panPivots.Length && panPivots[i] != null && i < basePanRotations.Length)
                    panPivots[i].localRotation = basePanRotations[i] * Quaternion.Euler(0f, pan, 0f);
                if (i < tiltPivots.Length && tiltPivots[i] != null && i < baseTiltRotations.Length)
                    tiltPivots[i].localRotation = baseTiltRotations[i] * Quaternion.Euler(tilt, 0f, 0f);

                light.enabled = enabledWeight > 0.001f;
                var peak = activeLightCount >= 4 ? profile.FullGrooveSpotlightIntensity : profile.GrooveSpotlightIntensity;
                light.intensity = peak * enabledWeight;
                light.color = EvaluatePalette(palette, time / profile.ColorCycleDuration + i * 0.41f);
                ApplyBeam(i, light.color, enabledWeight);
            }
        }

        private void ApplyBeam(int index, Color lightColor, float weight)
        {
            if (index >= beamRenderers.Length || beamRenderers[index] == null) return;
            var beam = beamRenderers[index];
            beam.enabled = weight > 0.001f;
            if (!beam.enabled) return;

            beamPropertyBlock ??= new MaterialPropertyBlock();
            beam.GetPropertyBlock(beamPropertyBlock);
            beamPropertyBlock.SetColor(BaseColorId,
                new Color(lightColor.r, lightColor.g, lightColor.b, 0.055f * weight));
            beamPropertyBlock.SetColor(EmissionColorId, lightColor * (0.45f * weight));
            beam.SetPropertyBlock(beamPropertyBlock);
        }

        private void RestorePivots()
        {
            Restore(panPivots, basePanRotations);
            Restore(tiltPivots, baseTiltRotations);
        }

        private static Quaternion[] Capture(Transform[] values)
        {
            if (values == null) return Array.Empty<Quaternion>();
            var result = new Quaternion[values.Length];
            for (var i = 0; i < values.Length; i++) result[i] = values[i] != null ? values[i].localRotation : Quaternion.identity;
            return result;
        }

        private static void Restore(Transform[] values, Quaternion[] rotations)
        {
            if (values == null) return;
            for (var i = 0; i < values.Length && i < rotations.Length; i++)
                if (values[i] != null) values[i].localRotation = rotations[i];
        }

        public static Color EvaluatePalette(Color[] colors, float cycle)
        {
            if (colors == null || colors.Length == 0) return Color.white;
            var wrapped = Mathf.Repeat(cycle, colors.Length);
            var from = Mathf.FloorToInt(wrapped);
            var to = (from + 1) % colors.Length;
            return Color.Lerp(colors[from], colors[to], Mathf.SmoothStep(0f, 1f, wrapped - from));
        }
    }
}
