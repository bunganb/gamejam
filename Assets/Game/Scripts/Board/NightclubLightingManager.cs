using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class NightclubLightingManager : MonoBehaviour
    {
        [SerializeField] private NightclubLightingProfile profile;
        [SerializeField] private PrototypeMusicDirector musicDirector;
        [SerializeField] private MovingSpotlightController movingSpotlights;
        [SerializeField] private StrobeLightController strobe;
        [SerializeField] private NeonLightController[] neonGroups = Array.Empty<NeonLightController>();
        [SerializeField] private DanceFloorController danceFloor;
        [SerializeField] private Volume reactionVolume;

        private Bloom bloom;
        private ColorAdjustments colorAdjustments;
        private StageReactionState state = StageReactionState.Hening;
        private float progress;
        private float eventBeatPulse;
        private float rowPulse;
        private float failurePulse;
        private float currentEmission;

        public StageReactionState State => state;
        public float MusicBeatPulse { get; private set; }

        public void ConfigureReferences(NightclubLightingProfile lightingProfile, PrototypeMusicDirector music,
            MovingSpotlightController spotlightController, StrobeLightController strobeController,
            NeonLightController[] neons, DanceFloorController floor, Volume volume)
        {
            profile = lightingProfile;
            musicDirector = music;
            movingSpotlights = spotlightController;
            strobe = strobeController;
            neonGroups = neons ?? Array.Empty<NeonLightController>();
            danceFloor = floor;
            reactionVolume = volume;
            CacheVolumeOverrides();
            ResetImmediately();
        }

        public void ApplyState(StageReactionState reactionState, float normalizedProgress,
            float beat, float row, float failure)
        {
            state = reactionState;
            progress = Mathf.Clamp01(normalizedProgress);
            eventBeatPulse = Mathf.Clamp01(beat);
            rowPulse = Mathf.Clamp01(row);
            failurePulse = Mathf.Clamp01(failure);
            ApplyVisuals();
        }

        public void ResetImmediately()
        {
            state = StageReactionState.Hening;
            progress = eventBeatPulse = rowPulse = failurePulse = MusicBeatPulse = 0f;
            currentEmission = profile != null ? profile.HeningEmission : 0f;
            movingSpotlights?.ResetImmediately();
            strobe?.SetEnabled(false);
            ApplyVisuals();
        }

        private void Awake() => CacheVolumeOverrides();

        private void Update()
        {
            if (profile == null) return;
            MusicBeatPulse = musicDirector != null && musicDirector.TimelineStarted
                ? CalculateBeatPulse(AudioSettings.dspTime, musicDirector.TimelineStartDspTime, musicDirector.BeatsPerMinute)
                : 0f;
            var target = state switch
            {
                StageReactionState.FullGroove => profile.FullGrooveEmission,
                StageReactionState.Groove => Mathf.Lerp(profile.HeningEmission, profile.GrooveEmission, progress),
                _ => profile.HeningEmission
            };
            currentEmission = Mathf.MoveTowards(currentEmission, target,
                Time.unscaledDeltaTime * Mathf.Max(0.01f, profile.FullGrooveEmission) / profile.TransitionDuration);
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (profile == null) return;
            var pulse = Mathf.Max(MusicBeatPulse, Mathf.Max(eventBeatPulse, rowPulse));
            foreach (var neon in neonGroups)
            {
                if (neon == null) continue;
                neon.SetEmissionIntensity(currentEmission);
                neon.SetPulse(pulse * profile.BeatPulseAmount);
            }

            if (state == StageReactionState.FullGroove)
            {
                movingSpotlights?.SetTarget(1f, 4);
                strobe?.SetEnabled(true);
                danceFloor?.SetPattern(DanceFloorPatternMode.BeatPulse, profile.FullGrooveEmission * 0.45f, pulse);
            }
            else if (state == StageReactionState.Groove)
            {
                movingSpotlights?.SetTarget(Mathf.Lerp(0.25f, 0.65f, progress), 2);
                strobe?.SetEnabled(false);
                danceFloor?.SetPattern(DanceFloorPatternMode.Wave, profile.GrooveEmission * 0.25f, pulse);
            }
            else
            {
                movingSpotlights?.SetTarget(0f, 0);
                strobe?.SetEnabled(false);
                danceFloor?.SetPattern(DanceFloorPatternMode.Static, profile.HeningEmission * 0.18f, 0f);
            }

            if (failurePulse > 0f)
            {
                movingSpotlights?.SetTarget(0f, 0);
                strobe?.SetEnabled(false);
            }

            if (bloom != null)
                bloom.intensity.value = Mathf.Lerp(profile.BaselineBloom, profile.FullGrooveBloom,
                    state == StageReactionState.FullGroove ? progress : progress * 0.3f);
            if (colorAdjustments != null)
                colorAdjustments.colorFilter.value = EvaluateFullGrooveFilter(Time.unscaledTime,
                    state == StageReactionState.FullGroove ? profile.RgbFilterIntensity : 0f, profile.Palette);
        }

        private void CacheVolumeOverrides()
        {
            bloom = null;
            colorAdjustments = null;
            if (reactionVolume == null || reactionVolume.profile == null) return;
            reactionVolume.profile.TryGet(out bloom);
            reactionVolume.profile.TryGet(out colorAdjustments);
        }

        public static float CalculateBeatPulse(double dspTime, double timelineStart, float beatsPerMinute)
        {
            if (beatsPerMinute <= 0f || dspTime < timelineStart) return 0f;
            var phase = Mathf.Repeat((float)((dspTime - timelineStart) * beatsPerMinute / 60d), 1f);
            return Mathf.Pow(1f - phase, 4f);
        }

        public static Color EvaluateFullGrooveFilter(float time, float amount, Color[] colors)
        {
            if (amount <= 0f || colors == null || colors.Length == 0) return Color.white;
            var target = MovingSpotlightController.EvaluatePalette(colors, time * 1.25f);
            return Color.Lerp(Color.white, target, Mathf.Clamp01(amount));
        }
    }
}
