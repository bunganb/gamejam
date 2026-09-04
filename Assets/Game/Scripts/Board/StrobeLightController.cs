using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StrobeLightController : MonoBehaviour
    {
        [SerializeField] private Light strobeLight;
        [SerializeField, Min(0.08f)] private float flashInterval = 0.34f;
        [SerializeField, Range(0.01f, 0.12f)] private float flashDuration = 0.055f;
        [SerializeField, Min(0f)] private float intensity = 4.5f;
        [SerializeField] private Color color = Color.cyan;
        [SerializeField] private bool randomColorMode = true;
        [SerializeField] private Color[] palette = Array.Empty<Color>();

        private bool running;
        private float elapsed;
        private int flashIndex;

        public bool IsRunning => running;

        public void Configure(Light target, float interval, float duration, float peakIntensity, Color[] colors, bool randomColors)
        {
            strobeLight = target;
            flashInterval = Mathf.Max(0.08f, interval);
            flashDuration = Mathf.Clamp(duration, 0.01f, 0.12f);
            intensity = Mathf.Max(0f, peakIntensity);
            palette = colors ?? Array.Empty<Color>();
            randomColorMode = randomColors;
            ResetImmediately();
        }

        public void SetEnabled(bool value)
        {
            running = value;
            if (!running) ResetImmediately();
        }

        public void ResetImmediately()
        {
            elapsed = 0f;
            if (strobeLight != null)
            {
                strobeLight.enabled = false;
                strobeLight.intensity = 0f;
            }
        }

        private void Update()
        {
            if (!running || strobeLight == null) return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= flashInterval)
            {
                elapsed %= flashInterval;
                flashIndex++;
            }

            var flashing = elapsed < Mathf.Min(flashDuration, flashInterval * 0.35f);
            strobeLight.enabled = flashing;
            strobeLight.intensity = flashing ? intensity : 0f;
            strobeLight.color = randomColorMode ? EvaluateDeterministicColor(palette, flashIndex) : color;
        }

        public static Color EvaluateDeterministicColor(Color[] colors, int index)
        {
            if (colors == null || colors.Length == 0) return Color.white;
            var hashed = unchecked(index * 1103515245 + 12345) & int.MaxValue;
            return colors[hashed % colors.Length];
        }
    }
}
