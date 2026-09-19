using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MenuSpotlightMotion : MonoBehaviour
    {
        public Transform focus;
        public Vector3 focusOffset;
        [Min(0f)] public float sweepRadius = 0.5f;
        [Min(1f)] public float sweepPeriod = 12f;
        public float phase;
        [Min(0f)] public float intensity = 5f;
        [Range(0f, 0.2f)] public float breathing = 0.06f;
        private Light source;
        private float elapsed;

        private void Awake() => source = GetComponent<Light>();

        private void Update()
        {
            if (focus == null || source == null) return;
            elapsed += Time.deltaTime;
            float t = elapsed * Mathf.PI * 2f / Mathf.Max(1f, sweepPeriod) + phase;
            Vector3 target = focus.position + focusOffset +
                new Vector3(Mathf.Sin(t), 0f, Mathf.Sin(t * 0.73f)) * sweepRadius;
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
            source.intensity = intensity * (1f + Mathf.Sin(t * 0.8f) * breathing) *
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 2f));
        }
    }
}
