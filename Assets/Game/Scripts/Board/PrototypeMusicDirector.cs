using System;
using System.Collections;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMusicDirector : MonoBehaviour
    {
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private AudioSource harmonySource;
        [SerializeField] private AudioSource drumKickSource;
        [SerializeField] private AudioSource ketipungOneSource;
        [SerializeField] private AudioSource ketipungTwoSource;
        [SerializeField] private AudioSource bassSource;
        [SerializeField] private AudioSource fullSongSource;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelMusicDefinition[] musicProfiles = Array.Empty<LevelMusicDefinition>();
        [SerializeField, Min(0)] private int sampleSequencerLevelIndex;
        [SerializeField, Min(0f)] private float layerFadeDuration = 0.12f;
        [SerializeField, Min(0f)] private float bassFadeInDuration = 0.45f;
        [SerializeField, Min(1)] private int bassUnlockRemainingNotes = 1;
        [SerializeField, Min(0f)] private float earlyBassFadeInDuration = 0.75f;
        [SerializeField, Min(0f)] private float bassHoldDuration = 0.35f;
        [SerializeField, Min(0f)] private float fullSongCrossfadeDuration = 1.15f;
        [SerializeField, Range(40f, 220f)] private float bpm = 130f;
        [SerializeField, Range(1, 4)] private int subdivisionsPerBeat = 2;
        [SerializeField, Min(0f)] private float quantizationLeadTime = 0.01f;

        private readonly bool[] unlockedBeatLayers = new bool[3];
        private bool progressiveStemLayersEnabled = true;
        private bool bassUnlocked;
        private bool secondaryLayerUnlocked;
        private bool topLoopUnlocked;
        private Coroutine transitionRoutine;
        private bool timelineStarted;
        private double timelineStartDspTime;
        private LevelMusicDefinition activeProfile;

        public bool TimelineStarted => timelineStarted;
        public double TimelineStartDspTime => timelineStartDspTime;
        public float BeatsPerMinute => bpm;
        public event Action FullSongActivated;
        public event Action<float> FullSongTransitionStarted;

        public void ConfigureReferences(
            PuzzleGameplayEvents eventHub,
            AudioSource harmony,
            AudioSource drumKick,
            AudioSource ketipungOne,
            AudioSource ketipungTwo,
            AudioSource bass,
            AudioSource fullSong)
        {
            gameplayEvents = eventHub;
            harmonySource = harmony;
            drumKickSource = drumKick;
            ketipungOneSource = ketipungOne;
            ketipungTwoSource = ketipungTwo;
            bassSource = bass;
            fullSongSource = fullSong;
        }

        public void ConfigureSampleSequencerScope(LevelLoader loader, int enabledLevelIndex = 0)
        {
            UnsubscribeFromLevelLoader();
            levelLoader = loader;
            sampleSequencerLevelIndex = Mathf.Max(0, enabledLevelIndex);
            progressiveStemLayersEnabled = levelLoader != null && levelLoader.CurrentLevelIndex >= 0 &&
                                           levelLoader.CurrentLevelIndex != sampleSequencerLevelIndex;
            SubscribeToLevelLoader();
        }

        public void ConfigureFullSongTransition(
            int bassRemainingNotes,
            float bassFadeDuration,
            float crossfadeDuration)
        {
            bassUnlockRemainingNotes = Mathf.Max(1, bassRemainingNotes);
            earlyBassFadeInDuration = Mathf.Max(0f, bassFadeDuration);
            fullSongCrossfadeDuration = Mathf.Max(0f, crossfadeDuration);
        }

        public void ConfigureMusicCatalog(LevelLoader loader, LevelMusicDefinition[] profiles)
        {
            UnsubscribeFromLevelLoader();
            levelLoader = loader;
            musicProfiles = profiles != null ? (LevelMusicDefinition[])profiles.Clone() : Array.Empty<LevelMusicDefinition>();
            var initialIndex = levelLoader != null && levelLoader.CurrentLevelIndex >= 0
                ? levelLoader.CurrentLevelIndex
                : 0;
            ApplyMusicProfile(initialIndex);
            SubscribeToLevelLoader();
        }

        private void OnEnable()
        {
            Subscribe();
            SubscribeToLevelLoader();
        }

        private void Start()
        {
            StartSynchronizedTimeline();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnsubscribeFromLevelLoader();
        }

        private void Subscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            gameplayEvents.ChainAdvanced -= HandleChainAdvanced;
            gameplayEvents.ChainReset -= HandleChainReset;
            gameplayEvents.ChainCompleted -= HandleChainCompleted;
            gameplayEvents.ObjectiveRowCompleted -= HandleObjectiveRowCompleted;
            gameplayEvents.ChainAdvanced += HandleChainAdvanced;
            gameplayEvents.ChainReset += HandleChainReset;
            gameplayEvents.ChainCompleted += HandleChainCompleted;
            gameplayEvents.ObjectiveRowCompleted += HandleObjectiveRowCompleted;
        }

        private void Unsubscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            gameplayEvents.ChainAdvanced -= HandleChainAdvanced;
            gameplayEvents.ChainReset -= HandleChainReset;
            gameplayEvents.ChainCompleted -= HandleChainCompleted;
            gameplayEvents.ObjectiveRowCompleted -= HandleObjectiveRowCompleted;
        }

        private void StartSynchronizedTimeline()
        {
            if (timelineStarted || !AllSourcesReady())
            {
                return;
            }

            timelineStarted = true;
            ConfigureSource(harmonySource, 1f);
            ConfigureSource(drumKickSource, 0f);
            ConfigureSource(ketipungOneSource, 0f);
            ConfigureSource(ketipungTwoSource, 0f);
            ConfigureSource(bassSource, 0f);
            ConfigureSource(fullSongSource, 0f);

            timelineStartDspTime = AudioSettings.dspTime + 0.2d;
            PlayScheduled(harmonySource, timelineStartDspTime);
            PlayScheduled(drumKickSource, timelineStartDspTime);
            PlayScheduled(ketipungOneSource, timelineStartDspTime);
            PlayScheduled(ketipungTwoSource, timelineStartDspTime);
            PlayScheduled(bassSource, timelineStartDspTime);
            PlayScheduled(fullSongSource, timelineStartDspTime);
        }

        private void HandleChainAdvanced(GameplayProgressSnapshot snapshot)
        {
            if (activeProfile != null)
            {
                if (!secondaryLayerUnlocked && activeProfile.SecondaryLayer != null &&
                    snapshot.NormalizedProgress >= activeProfile.SecondaryLayerThreshold)
                {
                    secondaryLayerUnlocked = true;
                    StartCoroutine(FadeSourceOnNextGrid(drumKickSource, earlyBassFadeInDuration));
                }

                if (!bassUnlocked && activeProfile.BuildLayer != null &&
                    ShouldUnlockBassWithRemainingNotes(
                        snapshot.MatchedTotal,
                        snapshot.TotalNotes,
                        activeProfile.BuildLayerRemainingNotes,
                        bassUnlocked))
                {
                    bassUnlocked = true;
                    StartCoroutine(FadeSourceOnNextGrid(bassSource, earlyBassFadeInDuration));
                }

                return;
            }

            if (!progressiveStemLayersEnabled)
            {
                if (ShouldUnlockBassWithRemainingNotes(
                        snapshot.MatchedTotal,
                        snapshot.TotalNotes,
                        bassUnlockRemainingNotes,
                        bassUnlocked))
                {
                    bassUnlocked = true;
                    StartCoroutine(FadeSourceOnNextGrid(bassSource, earlyBassFadeInDuration));
                }

                return;
            }

            var layerIndex = GetLayerIndex(snapshot.ActualColor);
            if (unlockedBeatLayers[layerIndex])
            {
                return;
            }

            unlockedBeatLayers[layerIndex] = true;
            StartCoroutine(FadeSourceOnNextGrid(GetBeatSource(layerIndex)));
        }

        public static bool ShouldUnlockBassWithRemainingNotes(
            int matchedNotes,
            int totalNotes,
            int unlockRemainingNotes,
            bool alreadyUnlocked)
        {
            var remainingNotes = totalNotes - matchedNotes;
            return !alreadyUnlocked && totalNotes > 0 &&
                   remainingNotes > 0 && remainingNotes <= Mathf.Max(1, unlockRemainingNotes);
        }

        private void HandleChainReset()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            StopAllCoroutines();
            for (var index = 0; index < unlockedBeatLayers.Length; index++)
            {
                unlockedBeatLayers[index] = false;
            }

            bassUnlocked = false;
            secondaryLayerUnlocked = false;
            topLoopUnlocked = false;

            SetVolume(harmonySource, 1f);
            SetVolume(drumKickSource, 0f);
            SetVolume(ketipungOneSource, 0f);
            SetVolume(ketipungTwoSource, 0f);
            SetVolume(bassSource, 0f);
            SetVolume(fullSongSource, 0f);
        }

        private void HandleObjectiveRowCompleted(GameplayProgressSnapshot snapshot)
        {
            if (activeProfile == null || topLoopUnlocked || activeProfile.TopLoopLayer == null ||
                snapshot.ObjectiveRowIndex != activeProfile.TopLoopUnlockRow)
            {
                return;
            }

            topLoopUnlocked = true;
            StartCoroutine(FadeSourceOnNextGrid(ketipungOneSource, earlyBassFadeInDuration));
        }

        private void HandleChainCompleted(GameplayProgressSnapshot snapshot)
        {
            if (transitionRoutine == null)
            {
                transitionRoutine = StartCoroutine(TransitionToFullSong());
            }
        }

        private IEnumerator TransitionToFullSong()
        {
            yield return WaitForNextQuantizedStep();
            if (!bassUnlocked && bassSource != null && bassSource.clip != null)
            {
                bassUnlocked = true;
                yield return FadeSource(bassSource, 1f, bassFadeInDuration);
                if (bassHoldDuration > 0f)
                {
                    yield return new WaitForSecondsRealtime(bassHoldDuration);
                }
            }

            var elapsed = 0f;
            var harmonyStart = GetVolume(harmonySource);
            var kickStart = GetVolume(drumKickSource);
            var ketipungOneStart = GetVolume(ketipungOneSource);
            var ketipungTwoStart = GetVolume(ketipungTwoSource);
            var bassStart = GetVolume(bassSource);
            var fullSongStart = GetVolume(fullSongSource);
            var duration = Mathf.Max(0.0001f, fullSongCrossfadeDuration);
            FullSongTransitionStarted?.Invoke(duration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetVolume(harmonySource, Mathf.Lerp(harmonyStart, 0f, normalized));
                SetVolume(drumKickSource, Mathf.Lerp(kickStart, 0f, normalized));
                SetVolume(ketipungOneSource, Mathf.Lerp(ketipungOneStart, 0f, normalized));
                SetVolume(ketipungTwoSource, Mathf.Lerp(ketipungTwoStart, 0f, normalized));
                SetVolume(bassSource, Mathf.Lerp(bassStart, 0f, normalized));
                SetVolume(fullSongSource, Mathf.Lerp(fullSongStart, 1f, normalized));
                yield return null;
            }

            FullSongActivated?.Invoke();
            transitionRoutine = null;
        }

        private IEnumerator FadeSourceOnNextGrid(AudioSource source)
        {
            yield return FadeSourceOnNextGrid(source, layerFadeDuration);
        }

        private IEnumerator FadeSourceOnNextGrid(AudioSource source, float duration)
        {
            yield return WaitForNextQuantizedStep();
            yield return FadeSource(source, 1f, duration);
        }

        private IEnumerator WaitForNextQuantizedStep()
        {
            if (!timelineStarted || bpm <= 0f || subdivisionsPerBeat <= 0)
            {
                yield break;
            }

            var now = AudioSettings.dspTime;
            var targetDspTime = CalculateNextGridDspTime(
                timelineStartDspTime,
                now,
                bpm,
                subdivisionsPerBeat,
                quantizationLeadTime);

            while (AudioSettings.dspTime < targetDspTime)
            {
                yield return null;
            }
        }

        public static double CalculateNextGridDspTime(
            double timelineStart,
            double currentTime,
            float beatsPerMinute,
            int subdivisions,
            float leadTime)
        {
            if (beatsPerMinute <= 0f || subdivisions <= 0)
            {
                return currentTime;
            }

            var stepDuration = 60d / beatsPerMinute / subdivisions;
            var elapsed = System.Math.Max(0d, currentTime - timelineStart);
            var nextStepIndex = System.Math.Floor(elapsed / stepDuration) + 1d;
            var targetTime = timelineStart + nextStepIndex * stepDuration;
            if (targetTime - currentTime < leadTime)
            {
                targetTime += stepDuration;
            }

            return targetTime;
        }

        private static IEnumerator FadeSource(AudioSource source, float targetVolume, float duration)
        {
            if (source == null || source.clip == null)
            {
                yield break;
            }

            var startVolume = source.volume;
            if (duration <= 0f)
            {
                source.volume = targetVolume;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            source.volume = targetVolume;
        }

        private bool AllSourcesReady()
        {
            var ready = harmonySource != null && harmonySource.clip != null &&
                        fullSongSource != null && fullSongSource.clip != null;
            if (!ready)
            {
                Debug.LogError("PrototypeMusicDirector requires base harmony and full-song clips.", this);
            }

            return ready;
        }

        private static void ConfigureSource(AudioSource source, float volume)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            if (source.clip.loadState == AudioDataLoadState.Unloaded)
            {
                source.clip.LoadAudioData();
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = volume;
        }

        private AudioSource GetBeatSource(int layerIndex)
        {
            return layerIndex switch
            {
                0 => drumKickSource,
                1 => ketipungOneSource,
                _ => ketipungTwoSource
            };
        }

        private void SubscribeToLevelLoader()
        {
            if (levelLoader == null)
            {
                return;
            }

            levelLoader.LevelChanged -= HandleLevelChanged;
            levelLoader.LevelChanged += HandleLevelChanged;
            levelLoader.GameLevelChanged -= HandleGameLevelChanged;
            levelLoader.GameLevelChanged += HandleGameLevelChanged;
        }

        private void UnsubscribeFromLevelLoader()
        {
            if (levelLoader != null)
            {
                levelLoader.LevelChanged -= HandleLevelChanged;
                levelLoader.GameLevelChanged -= HandleGameLevelChanged;
            }
        }

        private void HandleLevelChanged(int levelIndex, LevelDefinition level)
        {
            if (levelLoader != null && levelLoader.CurrentGameLevel != null)
            {
                return;
            }

            if (musicProfiles != null && musicProfiles.Length > 0)
            {
                ApplyMusicProfile(levelIndex);
                RestartSynchronizedTimeline();
                return;
            }

            progressiveStemLayersEnabled = levelIndex != sampleSequencerLevelIndex;
        }

        private void HandleGameLevelChanged(GameLevelDefinition gameLevel)
        {
            if (gameLevel == null || gameLevel.Music == null)
            {
                return;
            }

            ApplyMusicProfile(gameLevel.Music);
            RestartSynchronizedTimeline();
        }

        private void ApplyMusicProfile(int levelIndex)
        {
            var profile = musicProfiles != null && levelIndex >= 0 && levelIndex < musicProfiles.Length
                ? musicProfiles[levelIndex]
                : null;
            ApplyMusicProfile(profile);
        }

        private void ApplyMusicProfile(LevelMusicDefinition profile)
        {
            activeProfile = profile;
            if (activeProfile == null)
            {
                return;
            }

            bpm = activeProfile.Bpm;
            subdivisionsPerBeat = activeProfile.SubdivisionsPerBeat;
            progressiveStemLayersEnabled = false;
            harmonySource.clip = activeProfile.BaseHarmony;
            drumKickSource.clip = activeProfile.SecondaryLayer;
            ketipungOneSource.clip = activeProfile.TopLoopLayer;
            ketipungTwoSource.clip = null;
            bassSource.clip = activeProfile.BuildLayer;
            fullSongSource.clip = activeProfile.FullSong;
            secondaryLayerUnlocked = false;
            topLoopUnlocked = false;
            bassUnlocked = false;
        }

        private void RestartSynchronizedTimeline()
        {
            StopAllCoroutines();
            transitionRoutine = null;
            StopSource(harmonySource);
            StopSource(drumKickSource);
            StopSource(ketipungOneSource);
            StopSource(ketipungTwoSource);
            StopSource(bassSource);
            StopSource(fullSongSource);
            timelineStarted = false;
            StartSynchronizedTimeline();
        }

        private static void PlayScheduled(AudioSource source, double dspTime)
        {
            if (source != null && source.clip != null)
            {
                source.PlayScheduled(dspTime);
            }
        }

        private static void StopSource(AudioSource source)
        {
            source?.Stop();
        }

        private static float GetVolume(AudioSource source)
        {
            return source != null && source.clip != null ? source.volume : 0f;
        }

        private static void SetVolume(AudioSource source, float volume)
        {
            if (source != null && source.clip != null)
            {
                source.volume = volume;
            }
        }

        private static int GetLayerIndex(BeatColor color)
        {
            return color switch
            {
                BeatColor.Magenta => 0,
                BeatColor.Yellow => 1,
                _ => 2
            };
        }
    }
}
