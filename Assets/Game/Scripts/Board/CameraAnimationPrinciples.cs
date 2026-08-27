using UnityEngine;

namespace GameJam.Gameplay
{
    public static class CameraAnimationPrinciples
    {
        public static float SmootherStep(float normalizedTime)
        {
            var t = Mathf.Clamp01(normalizedTime);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        public static float FadeEnvelope(float time, float duration, float fadeInDuration, float fadeOutDuration)
        {
            if (duration <= 0f)
            {
                return 0f;
            }

            var fadeIn = SmootherStep(time / Mathf.Max(0.0001f, fadeInDuration));
            var fadeOut = SmootherStep((duration - time) / Mathf.Max(0.0001f, fadeOutDuration));
            return fadeIn * fadeOut;
        }

        public static Vector2 EllipticalArc(float phase, float amplitude, float depth)
        {
            return new Vector2(
                Mathf.Sin(phase) * amplitude,
                Mathf.Cos(phase) * amplitude * depth);
        }

        public static float AnticipationAction(float normalizedTime, float anticipationRatio)
        {
            var normalized = Mathf.Clamp01(normalizedTime);
            var ratio = Mathf.Clamp(anticipationRatio, 0.05f, 0.4f);
            if (normalized < ratio)
            {
                return -0.22f * SmootherStep(normalized / ratio);
            }

            var actionTime = (normalized - ratio) / (1f - ratio);
            var releaseAnticipation = Mathf.Lerp(-0.22f, 0f, SmootherStep(actionTime));
            var actionEnvelope = Mathf.Sin(actionTime * Mathf.PI);
            var action = Mathf.Sin(actionTime * Mathf.PI * 2f) * actionEnvelope;
            return releaseAnticipation + action;
        }
    }
}
