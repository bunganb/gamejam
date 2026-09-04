using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    public enum DanceFloorPatternMode { Static, Random, Wave, BeatPulse }

    [DisallowMultipleComponent]
    public sealed class DanceFloorController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] tiles = Array.Empty<Renderer>();
        [SerializeField] private Color[] palette = Array.Empty<Color>();
        [SerializeField] private DanceFloorPatternMode patternMode = DanceFloorPatternMode.Static;
        [SerializeField, Min(0f)] private float emissionIntensity = 0.5f;
        [SerializeField, Min(0.01f)] private float patternSpeed = 1f;
        [SerializeField, Min(0.01f)] private float waveSpacing = 0.45f;
        [SerializeField] private int randomSeed = 31;
        [SerializeField, Min(0f)] private float startupFadeDuration = 0.8f;
        [SerializeField, Min(0.01f)] private float patternFadeDuration = 0.35f;

        private MaterialPropertyBlock propertyBlock;
        private float beatPulse;
        private float startupBlend;
        private float currentEmissionIntensity;
        private float targetEmissionIntensity;
        private float currentProgress;
        private float targetProgress;

        public int TileCount => tiles?.Length ?? 0;

        public void Configure(Renderer[] floorTiles, Color[] colors)
        {
            tiles = floorTiles ?? Array.Empty<Renderer>();
            palette = colors ?? Array.Empty<Color>();
            currentEmissionIntensity = 0f;
            targetEmissionIntensity = emissionIntensity;
            currentProgress = targetProgress = 0f;
            Apply(0f);
        }

        public void SetPattern(DanceFloorPatternMode mode, float intensity, float pulse)
        {
            patternMode = mode;
            targetEmissionIntensity = Mathf.Max(0f, intensity);
            beatPulse = Mathf.Clamp01(pulse);
        }

        public void SetProgress(float normalizedProgress)
        {
            targetProgress = Mathf.Clamp01(normalizedProgress);
        }

        private void Awake() => startupBlend = 0f;

        private void LateUpdate()
        {
            startupBlend = startupFadeDuration <= 0f
                ? 1f
                : Mathf.MoveTowards(startupBlend, 1f, Time.unscaledDeltaTime / startupFadeDuration);
            currentEmissionIntensity = Mathf.MoveTowards(currentEmissionIntensity, targetEmissionIntensity,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, patternFadeDuration));
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, patternFadeDuration));
            Apply(Time.unscaledTime);
        }

        public void Apply(float time)
        {
            if (tiles == null || palette == null || palette.Length == 0) return;
            propertyBlock ??= new MaterialPropertyBlock();
            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile == null) continue;
                var color = EvaluateColor(palette, patternMode, i, time, patternSpeed, waveSpacing, randomSeed);
                var pulse = patternMode == DanceFloorPatternMode.BeatPulse ? beatPulse : 0f;
                var progressPosition = currentProgress * tiles.Length - i;
                var tileReveal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progressPosition));
                var intensity = currentEmissionIntensity * (1f + pulse * 0.4f) * startupBlend * tileReveal;
                tile.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color * (0.12f * tileReveal));
                propertyBlock.SetColor(EmissionColorId, color * intensity);
                tile.SetPropertyBlock(propertyBlock);
            }
        }

        public static Color EvaluateColor(Color[] colors, DanceFloorPatternMode mode, int tileIndex,
            float time, float speed, float spacing, int seed)
        {
            if (colors == null || colors.Length == 0) return Color.black;
            switch (mode)
            {
                case DanceFloorPatternMode.Random:
                    var step = Mathf.FloorToInt(time * Mathf.Max(0.01f, speed));
                    var hash = unchecked((tileIndex + seed) * 73856093 ^ step * 19349663) & int.MaxValue;
                    return colors[hash % colors.Length];
                case DanceFloorPatternMode.Wave:
                    var wave = time * speed + tileIndex * spacing;
                    return MovingSpotlightController.EvaluatePalette(colors, wave);
                case DanceFloorPatternMode.BeatPulse:
                    return colors[tileIndex % colors.Length];
                default:
                    return colors[tileIndex % colors.Length] * 0.45f;
            }
        }
    }
}
