using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Lightweight haze above the audience area. It uses one particle system for the
    /// whole crowd and samples nearby lights so the haze follows the nightclub palette.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudienceSmokeEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private Material smokeMaterial;
        [SerializeField, Min(0f)] private float emissionRate = 7f;
        [SerializeField, Min(0.01f)] private float colorUpdateInterval = 0.12f;
        [SerializeField, Range(0f, 1f)] private float smokeAlpha = 0.075f;
        [SerializeField] private Color fallbackColor = Color.white;

        private Light[] lights = Array.Empty<Light>();
        private float colorTimer;

        private void Awake()
        {
            EnsureParticleSystem();
            CacheLights();
            ApplyColor(EvaluateLightingColor());
        }

        private void Update()
        {
            if (smoke == null)
            {
                return;
            }

            colorTimer -= Time.unscaledDeltaTime;
            if (colorTimer <= 0f)
            {
                colorTimer = colorUpdateInterval;
                CacheLights();
                ApplyColor(EvaluateLightingColor());
            }
        }

        private void EnsureParticleSystem()
        {
            if (smoke == null)
            {
                smoke = GetComponentInChildren<ParticleSystem>(true);
            }

            if (smoke == null)
            {
                var smokeObject = new GameObject("AudienceSmoke");
                smokeObject.transform.SetParent(transform, false);
                smoke = smokeObject.AddComponent<ParticleSystem>();
            }

            var main = smoke.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, smokeAlpha));

            var emission = smoke.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            var shape = smoke.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 0.25f, 8f);

            var velocity = smoke.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            // All axes must use the same curve mode. Use constant curves for the
            // lightweight upward drift instead of mixing a two-constant Y curve
            // with the default constant X/Z curves.
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.08f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            var noise = smoke.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.25f;

            var renderer = smoke.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && smokeMaterial != null)
            {
                renderer.sharedMaterial = smokeMaterial;
            }

            if (!smoke.isPlaying)
            {
                smoke.Play(true);
            }
        }

        private void CacheLights()
        {
            lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        }

        private Color EvaluateLightingColor()
        {
            var weighted = Vector3.zero;
            var weight = 0f;
            foreach (var light in lights)
            {
                if (light == null || !light.enabled || light.intensity <= 0.01f)
                {
                    continue;
                }

                var distance = Vector3.Distance(transform.position, light.transform.position);
                var distanceWeight = 1f / (1f + distance * 0.08f);
                var lightWeight = Mathf.Max(0.05f, light.intensity) * distanceWeight;
                weighted += new Vector3(light.color.r, light.color.g, light.color.b) * lightWeight;
                weight += lightWeight;
            }

            if (weight <= 0.001f)
            {
                return fallbackColor;
            }

            var color = weighted / weight;
            return new Color(color.x, color.y, color.z, 1f);
        }

        private void ApplyColor(Color lightingColor)
        {
            if (smoke == null)
            {
                return;
            }

            var color = Color.Lerp(Color.white, lightingColor, 0.72f);
            color.a = smokeAlpha;
            var main = smoke.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
        }
    }
}
