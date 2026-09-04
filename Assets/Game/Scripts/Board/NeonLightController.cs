using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    public enum NeonAnimationMode { Static, SmoothPulse, Flicker }

    [DisallowMultipleComponent]
    public sealed class NeonLightController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();
        [SerializeField] private Color color = Color.cyan;
        [SerializeField, Min(0f)] private float emissionIntensity = 1f;
        [SerializeField] private NeonAnimationMode animationMode = NeonAnimationMode.SmoothPulse;
        [SerializeField, Min(0f)] private float animationSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float animationAmount = 0.2f;
        [SerializeField] private int flickerSeed = 17;
        [SerializeField, Min(0f)] private float startupFadeDuration = 0.8f;

        private MaterialPropertyBlock propertyBlock;
        private float externalPulse;
        private float startupBlend;

        public void Configure(Renderer[] targets, Color neonColor, NeonAnimationMode mode, int seed = 17)
        {
            renderers = targets ?? Array.Empty<Renderer>();
            color = neonColor;
            animationMode = mode;
            flickerSeed = seed;
            Apply(0f);
        }

        public void SetColor(Color value) => color = value;
        public void SetEmissionIntensity(float value) => emissionIntensity = Mathf.Max(0f, value);
        public void SetPulse(float value) => externalPulse = Mathf.Clamp01(value);

        private void Awake() => startupBlend = 0f;

        private void LateUpdate()
        {
            startupBlend = startupFadeDuration <= 0f
                ? 1f
                : Mathf.MoveTowards(startupBlend, 1f, Time.unscaledDeltaTime / startupFadeDuration);
            Apply(Time.unscaledTime);
        }

        public void Apply(float time)
        {
            var animated = EvaluateAnimation(animationMode, time, animationSpeed, animationAmount, flickerSeed);
            var intensity = emissionIntensity * (animated + externalPulse * 0.3f) * startupBlend;
            propertyBlock ??= new MaterialPropertyBlock();
            foreach (var target in renderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color * 0.35f);
                propertyBlock.SetColor(EmissionColorId, color * intensity);
                target.SetPropertyBlock(propertyBlock);
            }
        }

        public static float EvaluateAnimation(NeonAnimationMode mode, float time, float speed, float amount, int seed)
        {
            amount = Mathf.Clamp01(amount);
            if (mode == NeonAnimationMode.Static) return 1f;
            if (mode == NeonAnimationMode.SmoothPulse)
                return 1f - amount + amount * (0.5f + 0.5f * Mathf.Sin(time * speed * Mathf.PI * 2f));

            var sample = Mathf.PerlinNoise(seed * 0.137f, Mathf.Floor(time * Mathf.Max(0.01f, speed) * 12f) * 0.19f);
            return Mathf.Lerp(1f - amount, 1f, sample);
        }
    }
}
