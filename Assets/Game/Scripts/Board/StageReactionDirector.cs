using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StageReactionDirector : MonoBehaviour
    {
        [SerializeField] private PuzzleGameplayEvents eventHub;
        [SerializeField] private StageReactionProfile profile;
        [SerializeField] private StageRingPresenter stageRing;
        [SerializeField] private AudienceReactionPresenter audience;
        [SerializeField] private StageBaseLightingPresenter baseLighting;
        [SerializeField] private DiscoSpotlightRig spotlights;
        [SerializeField] private NightclubLightingManager nightclubLighting;
        [SerializeField] private Volume reactionVolume;

        private Bloom bloom;
        private ColorAdjustments colorAdjustments;
        private float targetProgress;
        private float visualProgress;
        private float progressVelocity;
        private float beatPulse;
        private float rowPulse;
        private float failurePulse;
        private bool subscribed;
        private bool completed;
        private float rgbFilterWeight;

        public StageReactionState State { get; private set; } = StageReactionState.Hening;
        public float TargetProgress => targetProgress;
        public float VisualProgress => visualProgress;

        public void ConfigureNightclubLighting(NightclubLightingManager manager)
        {
            nightclubLighting = manager;
        }

        public void ConfigureReferences(
            PuzzleGameplayEvents events,
            StageReactionProfile reactionProfile,
            StageRingPresenter ringPresenter,
            AudienceReactionPresenter audiencePresenter,
            DiscoSpotlightRig spotlightPresenter,
            Volume volume)
        {
            ConfigureReferences(
                events,
                reactionProfile,
                ringPresenter,
                audiencePresenter,
                null,
                spotlightPresenter,
                volume);
        }

        public void ConfigureReferences(
            PuzzleGameplayEvents events,
            StageReactionProfile reactionProfile,
            StageRingPresenter ringPresenter,
            AudienceReactionPresenter audiencePresenter,
            StageBaseLightingPresenter baseLightingPresenter,
            DiscoSpotlightRig spotlightPresenter,
            Volume volume)
        {
            Unsubscribe();
            eventHub = events;
            profile = reactionProfile;
            stageRing = ringPresenter;
            audience = audiencePresenter;
            baseLighting = baseLightingPresenter;
            spotlights = spotlightPresenter;
            reactionVolume = volume;
            CacheBloom();
            Subscribe();
            ResetReaction(true);
        }

        private void OnEnable()
        {
            CacheBloom();
            Subscribe();
        }

        private void Start()
        {
            if (audience == null)
            {
                Debug.LogWarning("StageReactionDirector has no audience presenter; crowd reactions are disabled.", this);
            }

            if (reactionVolume == null || bloom == null || colorAdjustments == null)
            {
                Debug.LogWarning("StageReactionDirector requires a reaction volume with Bloom and Color Adjustments.", this);
            }
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (profile == null)
            {
                return;
            }

            visualProgress = Mathf.SmoothDamp(
                visualProgress,
                targetProgress,
                ref progressVelocity,
                profile.TransitionSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            beatPulse = Decay(beatPulse, profile.BeatPulseDuration);
            rowPulse = Decay(rowPulse, profile.RowPulseDuration);
            failurePulse = Decay(failurePulse, profile.FailurePulseDuration);
            var rgbTarget = State == StageReactionState.FullGroove ? 1f : 0f;
            rgbFilterWeight = Mathf.MoveTowards(
                rgbFilterWeight,
                rgbTarget,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, profile.RgbFilterFadeDuration));
            ApplyPresenters();
        }

        private void Subscribe()
        {
            if (subscribed || eventHub == null)
            {
                return;
            }

            eventHub.ChainAdvanced += HandleChainAdvanced;
            eventHub.ObjectiveRowCompleted += HandleObjectiveRowCompleted;
            eventHub.ChainFailed += HandleChainFailed;
            eventHub.ChainReset += HandleChainReset;
            eventHub.ChainCompleted += HandleChainCompleted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || eventHub == null)
            {
                subscribed = false;
                return;
            }

            eventHub.ChainAdvanced -= HandleChainAdvanced;
            eventHub.ObjectiveRowCompleted -= HandleObjectiveRowCompleted;
            eventHub.ChainFailed -= HandleChainFailed;
            eventHub.ChainReset -= HandleChainReset;
            eventHub.ChainCompleted -= HandleChainCompleted;
            subscribed = false;
        }

        private void HandleChainAdvanced(GameplayProgressSnapshot snapshot)
        {
            if (completed)
            {
                return;
            }

            targetProgress = Mathf.Clamp01(snapshot.NormalizedProgress);
            State = StageReactionMath.ResolveState(targetProgress, false);
            beatPulse = 1f;
            ApplyPresenters();
        }

        private void HandleObjectiveRowCompleted(GameplayProgressSnapshot snapshot)
        {
            if (completed)
            {
                return;
            }

            targetProgress = Mathf.Max(targetProgress, Mathf.Clamp01(snapshot.NormalizedProgress));
            State = StageReactionMath.ResolveState(targetProgress, false);
            rowPulse = 1f;
            ApplyPresenters();
        }

        private void HandleChainFailed(GameplayProgressSnapshot snapshot)
        {
            completed = false;
            targetProgress = 0f;
            State = StageReactionState.Hening;
            failurePulse = 1f;
            beatPulse = 0f;
            rowPulse = 0f;
            if (nightclubLighting == null) spotlights?.SetFullGroove(false);
            ApplyPresenters();
        }

        private void HandleChainReset() => ResetReaction(true);

        private void HandleChainCompleted(GameplayProgressSnapshot snapshot)
        {
            if (completed)
            {
                return;
            }

            completed = true;
            targetProgress = 1f;
            State = StageReactionState.FullGroove;
            beatPulse = 1f;
            rowPulse = 1f;
            if (nightclubLighting == null) spotlights?.SetFullGroove(true);
            ApplyPresenters();
        }

        private void ResetReaction(bool immediate)
        {
            completed = false;
            targetProgress = 0f;
            State = StageReactionState.Hening;
            beatPulse = 0f;
            rowPulse = 0f;
            failurePulse = 0f;
            progressVelocity = 0f;
            if (immediate)
            {
                visualProgress = 0f;
                rgbFilterWeight = 0f;
                if (nightclubLighting == null) spotlights?.ResetImmediately();
                nightclubLighting?.ResetImmediately();
            }
            else
            {
                if (nightclubLighting == null) spotlights?.SetFullGroove(false);
            }

            ApplyPresenters();
        }

        private float Decay(float value, float duration)
        {
            return Mathf.MoveTowards(value, 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));
        }

        private void ApplyPresenters()
        {
            var energy = Mathf.Clamp01(visualProgress);
            stageRing?.Apply(visualProgress, energy, beatPulse, rowPulse, failurePulse);
            audience?.Apply(State, energy, beatPulse, rowPulse, failurePulse);
            baseLighting?.Apply(State, energy, beatPulse, rowPulse, failurePulse);
            nightclubLighting?.ApplyState(State, energy, beatPulse, rowPulse, failurePulse);
            if (nightclubLighting == null && bloom != null && profile != null)
            {
                var fullWeight = State == StageReactionState.FullGroove ? energy : energy * 0.35f;
                bloom.intensity.value = Mathf.Lerp(profile.BaselineBloom, profile.FullGrooveBloom, fullWeight);
            }

            if (nightclubLighting == null && colorAdjustments != null && profile != null)
            {
                colorAdjustments.colorFilter.value = EvaluateRgbFilter(Time.unscaledTime, rgbFilterWeight);
            }
        }

        private void CacheBloom()
        {
            bloom = null;
            colorAdjustments = null;
            if (reactionVolume != null && reactionVolume.profile != null)
            {
                reactionVolume.profile.TryGet(out bloom);
                reactionVolume.profile.TryGet(out colorAdjustments);
            }
        }

        private Color EvaluateRgbFilter(float time, float weight)
        {
            var colors = profile.RgbFilterColors;
            if (weight <= 0f || colors == null || colors.Length == 0)
            {
                return Color.white;
            }

            var cycle = Mathf.Repeat(time * profile.RgbFilterFrequency, colors.Length);
            var from = Mathf.FloorToInt(cycle);
            var to = (from + 1) % colors.Length;
            var blend = CameraAnimationPrinciples.SmootherStep(cycle - from);
            var rgbColor = Color.Lerp(colors[from], colors[to], blend);
            return Color.Lerp(Color.white, rgbColor, profile.RgbFilterIntensity * weight);
        }
    }
}
