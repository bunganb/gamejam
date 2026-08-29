using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DiscoSpotlightRig : MonoBehaviour
    {
        [SerializeField] private Transform[] panPivots;
        [SerializeField] private Transform[] tiltPivots;
        [SerializeField] private Light[] spotlights;
        [SerializeField] private StageReactionProfile profile;

        private Quaternion[] basePanRotations = Array.Empty<Quaternion>();
        private Quaternion[] baseTiltRotations = Array.Empty<Quaternion>();
        private float currentWeight;
        private bool fullGroove;

        public int SpotlightCount => spotlights?.Length ?? 0;
        public float CurrentWeight => currentWeight;

        public void ConfigureReferences(Transform[] pans, Transform[] tilts, Light[] lights, StageReactionProfile reactionProfile)
        {
            panPivots = pans;
            tiltPivots = tilts;
            spotlights = lights;
            profile = reactionProfile;
            CacheBaselines();
            ApplyLightDefaults();
            ResetImmediately();
        }

        public void SetFullGroove(bool active)
        {
            fullGroove = active;
        }

        public void ResetImmediately()
        {
            fullGroove = false;
            currentWeight = 0f;
            for (var index = 0; index < SpotlightCount; index++)
            {
                if (spotlights[index] != null)
                {
                    spotlights[index].intensity = 0f;
                    spotlights[index].enabled = false;
                }
            }

            RestorePivots();
        }

        private void Awake()
        {
            CacheBaselines();
            ApplyLightDefaults();
            ResetImmediately();
        }

        private void Update()
        {
            if (profile == null)
            {
                return;
            }

            var target = fullGroove ? 1f : 0f;
            var step = Time.unscaledDeltaTime / Mathf.Max(0.01f, profile.SpotlightFadeDuration);
            currentWeight = Mathf.MoveTowards(currentWeight, target, step);
            AnimateLights(Time.unscaledTime);
        }

        private void CacheBaselines()
        {
            basePanRotations = CaptureRotations(panPivots);
            baseTiltRotations = CaptureRotations(tiltPivots);
        }

        private void ApplyLightDefaults()
        {
            if (profile == null || spotlights == null)
            {
                return;
            }

            foreach (var spotlight in spotlights)
            {
                if (spotlight == null)
                {
                    continue;
                }

                spotlight.type = LightType.Spot;
                spotlight.range = profile.SpotlightRange;
                spotlight.spotAngle = profile.SpotlightOuterAngle;
                spotlight.innerSpotAngle = profile.SpotlightInnerAngle;
                spotlight.shadows = LightShadows.None;
            }
        }

        private void AnimateLights(float time)
        {
            var count = SpotlightCount;
            var colors = profile.SpotlightColors;
            for (var index = 0; index < count; index++)
            {
                var spotlight = spotlights[index];
                if (spotlight == null)
                {
                    continue;
                }

                var phase = index * Mathf.PI * 0.5f;
                var movementTime = time * profile.SpotlightMovementSpeed * Mathf.PI * 2f;
                var pan = Mathf.Sin(movementTime + phase) * profile.SpotlightPanAmplitude * currentWeight;
                var tilt = Mathf.Sin(movementTime * 0.73f + phase * 1.31f) * profile.SpotlightTiltAmplitude * currentWeight;

                if (panPivots != null && index < panPivots.Length && panPivots[index] != null && index < basePanRotations.Length)
                {
                    panPivots[index].localRotation = basePanRotations[index] * Quaternion.Euler(0f, pan, 0f);
                }

                if (tiltPivots != null && index < tiltPivots.Length && tiltPivots[index] != null && index < baseTiltRotations.Length)
                {
                    tiltPivots[index].localRotation = baseTiltRotations[index] * Quaternion.Euler(tilt, 0f, 0f);
                }

                spotlight.enabled = currentWeight > 0.001f;
                spotlight.intensity = profile.SpotlightIntensity * currentWeight;
                spotlight.color = EvaluatePalette(colors, time / profile.SpotlightColorCycleDuration + index * 0.37f);
            }
        }

        private void RestorePivots()
        {
            RestoreRotations(panPivots, basePanRotations);
            RestoreRotations(tiltPivots, baseTiltRotations);
        }

        private static Quaternion[] CaptureRotations(Transform[] pivots)
        {
            if (pivots == null)
            {
                return Array.Empty<Quaternion>();
            }

            var rotations = new Quaternion[pivots.Length];
            for (var index = 0; index < pivots.Length; index++)
            {
                rotations[index] = pivots[index] != null ? pivots[index].localRotation : Quaternion.identity;
            }

            return rotations;
        }

        private static void RestoreRotations(Transform[] pivots, Quaternion[] rotations)
        {
            if (pivots == null)
            {
                return;
            }

            for (var index = 0; index < pivots.Length && index < rotations.Length; index++)
            {
                if (pivots[index] != null)
                {
                    pivots[index].localRotation = rotations[index];
                }
            }
        }

        private static Color EvaluatePalette(Color[] colors, float cycle)
        {
            if (colors == null || colors.Length == 0)
            {
                return Color.white;
            }

            var wrapped = Mathf.Repeat(cycle, colors.Length);
            var from = Mathf.FloorToInt(wrapped);
            var to = (from + 1) % colors.Length;
            var blend = Mathf.SmoothStep(0f, 1f, wrapped - from);
            return Color.Lerp(colors[from], colors[to], blend);
        }
    }
}
