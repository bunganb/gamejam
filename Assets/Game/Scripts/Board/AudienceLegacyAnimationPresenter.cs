using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animation))]
    public sealed class AudienceLegacyAnimationPresenter : MonoBehaviour
    {
        private const int BoogieComboThreshold = 6;
        [SerializeField] private Animation legacyAnimation;
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private string idleClip;
        [SerializeField] private string grooveClip;
        [SerializeField] private string fullGrooveClip;
        [SerializeField, Min(0f)] private float fadeDuration = 0.18f;

        private bool fullGroove;
        private string activeClip;

        public void Configure(Animation animationComponent, PuzzleGameplayEvents events,
            string idle, string groove, string full, float fade = 0.18f)
        {
            Unsubscribe();
            legacyAnimation = animationComponent;
            gameplayEvents = events;
            idleClip = idle;
            grooveClip = groove;
            fullGrooveClip = full;
            fadeDuration = Mathf.Max(0f, fade);
            ConfigureClips();
            Subscribe();
            PlayIdle();
        }

        private void Awake()
        {
            legacyAnimation ??= GetComponent<Animation>();
            gameplayEvents ??= FindAnyObjectByType<PuzzleGameplayEvents>();
            ConfigureClips();
            StopAutoplay();
        }

        private void OnEnable() => Subscribe();
        private void Start() => PlayIdle();

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (gameplayEvents == null) return;
            gameplayEvents.ChainAdvanced -= HandleAdvanced;
            gameplayEvents.ObjectiveRowCompleted -= HandleAdvanced;
            gameplayEvents.ChainFailed -= HandleReset;
            gameplayEvents.ChainReset -= HandleResetEvent;
            gameplayEvents.ChainCompleted -= HandleCompleted;
            gameplayEvents.ChainAdvanced += HandleAdvanced;
            gameplayEvents.ObjectiveRowCompleted += HandleAdvanced;
            gameplayEvents.ChainFailed += HandleReset;
            gameplayEvents.ChainReset += HandleResetEvent;
            gameplayEvents.ChainCompleted += HandleCompleted;
        }

        private void Unsubscribe()
        {
            if (gameplayEvents == null) return;
            gameplayEvents.ChainAdvanced -= HandleAdvanced;
            gameplayEvents.ObjectiveRowCompleted -= HandleAdvanced;
            gameplayEvents.ChainFailed -= HandleReset;
            gameplayEvents.ChainReset -= HandleResetEvent;
            gameplayEvents.ChainCompleted -= HandleCompleted;
        }

        private void HandleAdvanced(GameplayProgressSnapshot snapshot)
        {
            if (!fullGroove)
                Play(snapshot.MatchedTotal >= BoogieComboThreshold ? grooveClip : idleClip);
        }

        private void HandleReset(GameplayProgressSnapshot snapshot)
        {
            fullGroove = false;
            PlayIdle();
        }

        private void HandleResetEvent()
        {
            fullGroove = false;
            PlayIdle();
        }

        private void HandleCompleted(GameplayProgressSnapshot snapshot)
        {
            fullGroove = true;
            Play(fullGrooveClip);
        }

        private void StopAutoplay()
        {
            if (legacyAnimation == null) return;
            legacyAnimation.playAutomatically = false;
            legacyAnimation.Stop();
        }

        private void ConfigureClips()
        {
            ConfigureClip(idleClip, WrapMode.Loop);
            ConfigureClip(grooveClip, WrapMode.Loop);
            ConfigureClip(fullGrooveClip, WrapMode.Loop);
        }

        private void ConfigureClip(string clipName, WrapMode mode)
        {
            var state = Resolve(clipName);
            if (state != null) state.wrapMode = mode;
        }

        private void PlayIdle() => Play(idleClip);

        private void Play(string clipName)
        {
            var state = Resolve(clipName);
            if (state == null || activeClip == clipName) return;
            activeClip = clipName;
            if (fadeDuration <= 0f) legacyAnimation.Play(clipName, PlayMode.StopAll);
            else legacyAnimation.CrossFade(clipName, fadeDuration, PlayMode.StopAll);
        }

        private AnimationState Resolve(string clipName)
        {
            return legacyAnimation != null && !string.IsNullOrWhiteSpace(clipName)
                ? legacyAnimation[clipName]
                : null;
        }
    }
}
