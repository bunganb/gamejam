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
        [SerializeField, Min(0f)] private float layerFadeDuration = 0.12f;
        [SerializeField, Min(0f)] private float bassFadeInDuration = 0.45f;
        [SerializeField, Min(0f)] private float bassHoldDuration = 0.35f;
        [SerializeField, Min(0f)] private float fullSongCrossfadeDuration = 0.8f;
        [SerializeField, Range(40f, 220f)] private float bpm = 130f;
        [SerializeField, Range(1, 4)] private int subdivisionsPerBeat = 2;
        [SerializeField, Min(0f)] private float quantizationLeadTime = 0.01f;

        private readonly bool[] unlockedBeatLayers = new bool[3];
        private Coroutine transitionRoutine;
        private bool timelineStarted;
        private double timelineStartDspTime;

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

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            StartSynchronizedTimeline();
        }

        private void OnDisable()
        {
            Unsubscribe();
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
            gameplayEvents.ChainAdvanced += HandleChainAdvanced;
            gameplayEvents.ChainReset += HandleChainReset;
            gameplayEvents.ChainCompleted += HandleChainCompleted;
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
            harmonySource.PlayScheduled(timelineStartDspTime);
            drumKickSource.PlayScheduled(timelineStartDspTime);
            ketipungOneSource.PlayScheduled(timelineStartDspTime);
            ketipungTwoSource.PlayScheduled(timelineStartDspTime);
            bassSource.PlayScheduled(timelineStartDspTime);
            fullSongSource.PlayScheduled(timelineStartDspTime);
        }

        private void HandleChainAdvanced(GameplayProgressSnapshot snapshot)
        {
            var layerIndex = GetLayerIndex(snapshot.ActualColor);
            if (unlockedBeatLayers[layerIndex])
            {
                return;
            }

            unlockedBeatLayers[layerIndex] = true;
            StartCoroutine(FadeSourceOnNextGrid(GetBeatSource(layerIndex)));
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

            harmonySource.volume = 1f;
            drumKickSource.volume = 0f;
            ketipungOneSource.volume = 0f;
            ketipungTwoSource.volume = 0f;
            bassSource.volume = 0f;
            fullSongSource.volume = 0f;
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
            yield return FadeSource(bassSource, 1f, bassFadeInDuration);

            if (bassHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(bassHoldDuration);
            }

            var elapsed = 0f;
            var harmonyStart = harmonySource.volume;
            var kickStart = drumKickSource.volume;
            var ketipungOneStart = ketipungOneSource.volume;
            var ketipungTwoStart = ketipungTwoSource.volume;
            var bassStart = bassSource.volume;
            var fullSongStart = fullSongSource.volume;
            var duration = Mathf.Max(0.0001f, fullSongCrossfadeDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                harmonySource.volume = Mathf.Lerp(harmonyStart, 0f, normalized);
                drumKickSource.volume = Mathf.Lerp(kickStart, 0f, normalized);
                ketipungOneSource.volume = Mathf.Lerp(ketipungOneStart, 0f, normalized);
                ketipungTwoSource.volume = Mathf.Lerp(ketipungTwoStart, 0f, normalized);
                bassSource.volume = Mathf.Lerp(bassStart, 0f, normalized);
                fullSongSource.volume = Mathf.Lerp(fullSongStart, 1f, normalized);
                yield return null;
            }

            transitionRoutine = null;
        }

        private IEnumerator FadeSourceOnNextGrid(AudioSource source)
        {
            yield return WaitForNextQuantizedStep();
            yield return FadeSource(source, 1f, layerFadeDuration);
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
                        drumKickSource != null && drumKickSource.clip != null &&
                        ketipungOneSource != null && ketipungOneSource.clip != null &&
                        ketipungTwoSource != null && ketipungTwoSource.clip != null &&
                        bassSource != null && bassSource.clip != null &&
                        fullSongSource != null && fullSongSource.clip != null;
            if (!ready)
            {
                Debug.LogError("PrototypeMusicDirector requires all six audio clips and sources.", this);
            }

            return ready;
        }

        private static void ConfigureSource(AudioSource source, float volume)
        {
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
