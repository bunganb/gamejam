using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class LightStartupFade : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float duration = 0.8f;
        [SerializeField] private bool disableWhenComplete = false;

        private Light target;
        private float targetIntensity;
        private float progress;

        private void Awake()
        {
            target = GetComponent<Light>();
            targetIntensity = target != null ? target.intensity : 0f;
            progress = 0f;
            if (target != null) target.intensity = 0f;
        }

        private void Update()
        {
            if (target == null) return;
            progress = duration <= 0f ? 1f : Mathf.MoveTowards(progress, 1f, Time.unscaledDeltaTime / duration);
            target.intensity = targetIntensity * progress;
            if (progress >= 1f && disableWhenComplete) enabled = false;
        }
    }
}
