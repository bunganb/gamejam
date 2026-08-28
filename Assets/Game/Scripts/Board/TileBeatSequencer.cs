using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TileBeatSequencer : MonoBehaviour
    {
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private PrototypeMusicDirector musicDirector;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private AudioClip kick;
        [SerializeField] private AudioClip ketipungOneSoft;
        [SerializeField] private AudioClip ketipungOneAccent;
        [SerializeField] private AudioClip ketipungTwoSoft;
        [SerializeField] private AudioClip ketipungTwoAccent;
        [SerializeField] private LevelMusicDefinition[] musicProfiles = Array.Empty<LevelMusicDefinition>();
        [SerializeField, Min(0)] private int enabledLevelIndex;
        [SerializeField, Range(1, 4)] private int subdivisionsPerBeat = 2;
        [SerializeField, Range(1, 32)] private int loopStepCount = 8;
        [SerializeField] private int[] sequenceSlots = { 0, 3, 5, 6, 1, 3, 5, 7, 2, 4, 6 };
        [SerializeField] private bool[] sequenceAccents = { false, false, false, false, false, false, false, true, false, true, true };
        [SerializeField, Range(2, 12)] private int previewVoiceCount = 8;
        [SerializeField, Range(4, 24)] private int loopVoiceCount = 12;
        [SerializeField, Range(0f, 1f)] private float kickVolume = 0.72f;
        [SerializeField, Range(0f, 1f)] private float softVolume = 0.52f;
        [SerializeField, Range(0f, 1f)] private float accentVolume = 0.90f;
        [SerializeField, Range(0f, 0.25f)] private float tileBeatFadeInDuration = 0.06f;
        [SerializeField, Min(0f)] private float schedulingLeadTime = 0.03f;
        [SerializeField, Range(0.05f, 1f)] private float loopScheduleAhead = 0.35f;

        private readonly List<SequencerStep> authoredSteps = new();
        private readonly Dictionary<AudioSource, float> voiceBaseVolumes = new();
        private readonly Dictionary<AudioSource, Coroutine> voiceFadeRoutines = new();
        private AudioSource[] previewVoices;
        private AudioSource[] loopVoices;
        private int nextPreviewVoice;
        private int nextLoopVoice;
        private int committedStepCount;
        private bool isActiveForLevel = true;
        private bool hasPendingStep;
        private bool loopActive;
        private GameplayProgressSnapshot pendingSnapshot;
        private double lastPreviewDspTime;
        private float outputGain = 1f;
        private Coroutine outputFadeRoutine;
        private LevelMusicDefinition activeProfile;

        public int CommittedStepCount => committedStepCount;

        public void ConfigureReferences(
            PuzzleGameplayEvents eventHub,
            PrototypeMusicDirector timeline,
            LevelLoader loader,
            AudioClip kickClip,
            AudioClip ketipungOneSoftClip,
            AudioClip ketipungOneAccentClip,
            AudioClip ketipungTwoSoftClip,
            AudioClip ketipungTwoAccentClip,
            int levelIndex = 0)
        {
            Unsubscribe();
            gameplayEvents = eventHub;
            musicDirector = timeline;
            levelLoader = loader;
            kick = kickClip;
            ketipungOneSoft = ketipungOneSoftClip;
            ketipungOneAccent = ketipungOneAccentClip;
            ketipungTwoSoft = ketipungTwoSoftClip;
            ketipungTwoAccent = ketipungTwoAccentClip;
            enabledLevelIndex = Mathf.Max(0, levelIndex);
            isActiveForLevel = levelLoader == null || levelLoader.CurrentLevelIndex < 0 ||
                               levelLoader.CurrentLevelIndex == enabledLevelIndex;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void ConfigureLevelScope(LevelLoader loader, int levelIndex = 0)
        {
            Unsubscribe();
            levelLoader = loader;
            enabledLevelIndex = Mathf.Max(0, levelIndex);
            isActiveForLevel = levelLoader == null || levelLoader.CurrentLevelIndex < 0 ||
                               levelLoader.CurrentLevelIndex == enabledLevelIndex;
            ClearSequence();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void ConfigureMusicCatalog(LevelLoader loader, IReadOnlyList<LevelMusicDefinition> profiles)
        {
            Unsubscribe();
            levelLoader = loader;
            musicProfiles = profiles == null ? Array.Empty<LevelMusicDefinition>() : CopyProfiles(profiles);
            var initialIndex = levelLoader != null && levelLoader.CurrentLevelIndex >= 0
                ? levelLoader.CurrentLevelIndex
                : 0;
            ApplyMusicProfile(initialIndex);
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void ConfigurePattern(
            int stepsPerLoop,
            int subdivisions,
            IReadOnlyList<int> slots,
            IReadOnlyList<bool> accents)
        {
            loopStepCount = Mathf.Max(1, stepsPerLoop);
            subdivisionsPerBeat = Mathf.Clamp(subdivisions, 1, 4);
            var count = slots?.Count ?? 0;
            sequenceSlots = new int[count];
            sequenceAccents = new bool[count];
            for (var index = 0; index < count; index++)
            {
                sequenceSlots[index] = Mathf.Clamp(slots[index], 0, loopStepCount - 1);
                sequenceAccents[index] = accents != null && index < accents.Count && accents[index];
            }
        }

        public void ConfigureTileBeatTransition(float fadeInDuration)
        {
            tileBeatFadeInDuration = Mathf.Clamp(fadeInDuration, 0f, 0.25f);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearSequence();
        }

        private void Update()
        {
            ScheduleLoopAhead();
        }

        private void LateUpdate()
        {
            CommitPendingStep();
        }

        private void Subscribe()
        {
            if (gameplayEvents != null)
            {
                gameplayEvents.ChainAdvanced -= HandleChainAdvanced;
                gameplayEvents.ChainFailed -= HandleChainFailed;
                gameplayEvents.ChainReset -= HandleChainReset;
                gameplayEvents.ChainAdvanced += HandleChainAdvanced;
                gameplayEvents.ChainFailed += HandleChainFailed;
                gameplayEvents.ChainReset += HandleChainReset;
            }

            if (levelLoader != null)
            {
                levelLoader.LevelChanged -= HandleLevelChanged;
                levelLoader.LevelChanged += HandleLevelChanged;
            }

            if (musicDirector != null)
            {
                musicDirector.FullSongTransitionStarted -= HandleFullSongTransitionStarted;
                musicDirector.FullSongActivated -= HandleFullSongActivated;
                musicDirector.FullSongTransitionStarted += HandleFullSongTransitionStarted;
                musicDirector.FullSongActivated += HandleFullSongActivated;
            }
        }

        private void Unsubscribe()
        {
            if (gameplayEvents != null)
            {
                gameplayEvents.ChainAdvanced -= HandleChainAdvanced;
                gameplayEvents.ChainFailed -= HandleChainFailed;
                gameplayEvents.ChainReset -= HandleChainReset;
            }

            if (levelLoader != null)
            {
                levelLoader.LevelChanged -= HandleLevelChanged;
            }

            if (musicDirector != null)
            {
                musicDirector.FullSongTransitionStarted -= HandleFullSongTransitionStarted;
                musicDirector.FullSongActivated -= HandleFullSongActivated;
            }
        }

        private void HandleChainAdvanced(GameplayProgressSnapshot snapshot)
        {
            if (!isActiveForLevel)
            {
                return;
            }

            CommitPendingStep();
            pendingSnapshot = snapshot;
            hasPendingStep = true;
        }

        private void HandleChainFailed(GameplayProgressSnapshot snapshot)
        {
            ClearSequence();
        }

        private void HandleChainReset()
        {
            ClearSequence();
        }

        private void HandleLevelChanged(int levelIndex, LevelDefinition level)
        {
            ClearSequence();
            ApplyMusicProfile(levelIndex);
        }

        private void HandleFullSongActivated()
        {
            StopVoices(previewVoices);
            StopLoopPlayback();
        }

        private void HandleFullSongTransitionStarted(float duration)
        {
            if (outputFadeRoutine != null)
            {
                StopCoroutine(outputFadeRoutine);
            }

            outputFadeRoutine = StartCoroutine(FadeOutputToZero(duration));
        }

        private void CommitPendingStep()
        {
            if (!hasPendingStep || !isActiveForLevel || musicDirector == null || !musicDirector.TimelineStarted)
            {
                return;
            }

            var patternIndex = Mathf.Max(0, pendingSnapshot.MatchedTotal - 1);
            var profileSlots = activeProfile?.SequenceSlots;
            var slotIndex = profileSlots != null && patternIndex < profileSlots.Count
                ? profileSlots[patternIndex]
                : patternIndex < sequenceSlots.Length
                    ? sequenceSlots[patternIndex]
                : patternIndex % Mathf.Max(1, loopStepCount);
            var accent = patternIndex < sequenceAccents.Length && sequenceAccents[patternIndex];
            var previewDspTime = CalculatePreviewDspTime();
            var loopDelay = activeProfile != null && musicDirector.BeatsPerMinute > 0f
                ? activeProfile.LoopEntryDelayBeats * 60d / musicDirector.BeatsPerMinute
                : 0d;
            var step = new SequencerStep(
                pendingSnapshot.ActualColor,
                accent,
                slotIndex,
                patternIndex,
                CalculateNextSlotDspTime(
                    musicDirector.TimelineStartDspTime,
                    AudioSettings.dspTime,
                    previewDspTime + loopDelay,
                    musicDirector.BeatsPerMinute,
                    subdivisionsPerBeat,
                    loopStepCount,
                    slotIndex,
                    schedulingLeadTime));
            authoredSteps.Add(step);
            SchedulePreview(step, previewDspTime);
            committedStepCount = authoredSteps.Count;
            EnableLoopPlayback();

            hasPendingStep = false;
        }

        private double CalculatePreviewDspTime()
        {
            return CalculateNextSequenceDspTime(
                musicDirector.TimelineStartDspTime,
                AudioSettings.dspTime,
                musicDirector.BeatsPerMinute,
                subdivisionsPerBeat,
                schedulingLeadTime,
                lastPreviewDspTime);
        }

        private void SchedulePreview(SequencerStep step, double targetDspTime)
        {
            var clip = ResolveClip(step);
            if (clip == null)
            {
                return;
            }

            EnsurePreviewVoices();
            ScheduleVoice(
                previewVoices,
                ref nextPreviewVoice,
                clip,
                ResolveVolume(step),
                targetDspTime,
                tileBeatFadeInDuration);
            lastPreviewDspTime = targetDspTime;
        }

        private void EnableLoopPlayback()
        {
            loopActive = committedStepCount > 0;
        }

        private void ScheduleLoopAhead()
        {
            if (!loopActive || !isActiveForLevel || musicDirector == null || !musicDirector.TimelineStarted)
            {
                return;
            }

            EnsureLoopVoices();
            var bpm = musicDirector.BeatsPerMinute;
            if (bpm <= 0f || subdivisionsPerBeat <= 0 || loopStepCount <= 0)
            {
                return;
            }

            var stepDuration = 60d / bpm / subdivisionsPerBeat;
            var loopDuration = stepDuration * loopStepCount;
            var scheduleHorizon = AudioSettings.dspTime + loopScheduleAhead;
            for (var index = 0; index < committedStepCount; index++)
            {
                var step = authoredSteps[index];
                var clip = ResolveClip(step);
                if (clip == null)
                {
                    continue;
                }

                while (step.NextLoopDspTime <= scheduleHorizon)
                {
                    ScheduleVoice(
                        loopVoices,
                        ref nextLoopVoice,
                        clip,
                        ResolveVolume(step),
                        step.NextLoopDspTime,
                        0f);
                    step.NextLoopDspTime += loopDuration;
                }
            }
        }

        private AudioClip ResolveClip(SequencerStep step)
        {
            if (activeProfile != null && step.PatternIndex >= 0 && step.PatternIndex < activeProfile.NoteSamples.Count)
            {
                return activeProfile.NoteSamples[step.PatternIndex];
            }

            return step.Color switch
            {
                BeatColor.Magenta => kick,
                BeatColor.Yellow => step.Accent ? ketipungOneAccent : ketipungOneSoft,
                _ => step.Accent ? ketipungTwoAccent : ketipungTwoSoft
            };
        }

        private float ResolveVolume(SequencerStep step)
        {
            if (activeProfile != null && step.PatternIndex >= 0 && step.PatternIndex < activeProfile.NoteVolumes.Count)
            {
                return Mathf.Clamp01(activeProfile.NoteVolumes[step.PatternIndex]);
            }

            if (step.Color == BeatColor.Magenta)
            {
                return kickVolume;
            }

            return step.Accent ? accentVolume : softVolume;
        }

        private void EnsurePreviewVoices()
        {
            previewVoices = EnsureVoices(previewVoices, previewVoiceCount, "SequencerPreviewVoice");
        }

        private void EnsureLoopVoices()
        {
            loopVoices = EnsureVoices(loopVoices, loopVoiceCount, "SequencerLoopVoice");
        }

        private AudioSource[] EnsureVoices(AudioSource[] existingVoices, int requestedCount, string prefix)
        {
            var desiredCount = Mathf.Clamp(requestedCount, 2, 24);
            if (existingVoices != null && existingVoices.Length == desiredCount)
            {
                return existingVoices;
            }

            var result = new AudioSource[desiredCount];
            for (var index = 0; index < desiredCount; index++)
            {
                var voiceTransform = transform.Find($"{prefix}_{index + 1:00}");
                if (voiceTransform == null)
                {
                    voiceTransform = new GameObject($"{prefix}_{index + 1:00}").transform;
                    voiceTransform.SetParent(transform, false);
                }

                var voice = voiceTransform.TryGetComponent<AudioSource>(out var existing)
                    ? existing
                    : voiceTransform.gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
                result[index] = voice;
            }

            return result;
        }

        private void ScheduleVoice(
            AudioSource[] sourceVoices,
            ref int voiceIndex,
            AudioClip clip,
            float volume,
            double targetDspTime,
            float fadeInDuration)
        {
            var voice = sourceVoices[voiceIndex];
            voiceIndex = (voiceIndex + 1) % sourceVoices.Length;
            StopVoiceFade(voice);
            voice.Stop();
            voice.clip = clip;
            voiceBaseVolumes[voice] = volume;
            voice.volume = fadeInDuration > 0f ? 0f : volume * outputGain;
            voice.PlayScheduled(targetDspTime);
            if (fadeInDuration > 0f)
            {
                voiceFadeRoutines[voice] = StartCoroutine(
                    FadeScheduledVoiceIn(voice, volume, targetDspTime, fadeInDuration));
            }
        }

        private void ClearSequence()
        {
            if (outputFadeRoutine != null)
            {
                StopCoroutine(outputFadeRoutine);
                outputFadeRoutine = null;
            }

            outputGain = 1f;
            authoredSteps.Clear();
            committedStepCount = 0;
            hasPendingStep = false;
            lastPreviewDspTime = 0d;
            nextPreviewVoice = 0;
            StopAllVoiceFades();
            StopVoices(previewVoices);
            StopLoopPlayback();
        }

        private IEnumerator FadeScheduledVoiceIn(
            AudioSource voice,
            float baseVolume,
            double startDspTime,
            float duration)
        {
            while (AudioSettings.dspTime < startDspTime)
            {
                yield return null;
            }

            var endDspTime = startDspTime + duration;
            while (AudioSettings.dspTime < endDspTime)
            {
                var gain = CalculateFadeInGain(AudioSettings.dspTime, startDspTime, duration);
                voice.volume = baseVolume * outputGain * gain;
                yield return null;
            }

            voice.volume = baseVolume * outputGain;
            voiceFadeRoutines.Remove(voice);
        }

        private void StopVoiceFade(AudioSource voice)
        {
            if (voice == null || !voiceFadeRoutines.TryGetValue(voice, out var routine))
            {
                return;
            }

            StopCoroutine(routine);
            voiceFadeRoutines.Remove(voice);
        }

        private void StopAllVoiceFades()
        {
            foreach (var routine in voiceFadeRoutines.Values)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            voiceFadeRoutines.Clear();
        }

        private IEnumerator FadeOutputToZero(float duration)
        {
            var startGain = outputGain;
            if (duration <= 0f)
            {
                outputGain = 0f;
                ApplyOutputGain();
                outputFadeRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                outputGain = Mathf.Lerp(startGain, 0f, normalized);
                ApplyOutputGain();
                yield return null;
            }

            outputGain = 0f;
            ApplyOutputGain();
            outputFadeRoutine = null;
        }

        private void ApplyOutputGain()
        {
            foreach (var pair in voiceBaseVolumes)
            {
                if (pair.Key != null)
                {
                    pair.Key.volume = pair.Value * outputGain;
                }
            }
        }

        private void StopLoopPlayback()
        {
            loopActive = false;
            nextLoopVoice = 0;
            StopVoices(loopVoices);
        }

        private void ApplyMusicProfile(int levelIndex)
        {
            activeProfile = musicProfiles != null && levelIndex >= 0 && levelIndex < musicProfiles.Length
                ? musicProfiles[levelIndex]
                : null;
            if (activeProfile != null)
            {
                enabledLevelIndex = levelIndex;
                subdivisionsPerBeat = activeProfile.SubdivisionsPerBeat;
                loopStepCount = activeProfile.LoopStepCount;
                tileBeatFadeInDuration = activeProfile.TileFadeInDuration;
                isActiveForLevel = true;
                return;
            }

            isActiveForLevel = musicProfiles == null || musicProfiles.Length == 0
                ? levelIndex == enabledLevelIndex
                : false;
        }

        private static LevelMusicDefinition[] CopyProfiles(IReadOnlyList<LevelMusicDefinition> profiles)
        {
            var result = new LevelMusicDefinition[profiles.Count];
            for (var index = 0; index < profiles.Count; index++)
            {
                result[index] = profiles[index];
            }

            return result;
        }

        private static void StopVoices(AudioSource[] sourceVoices)
        {
            if (sourceVoices == null)
            {
                return;
            }

            foreach (var voice in sourceVoices)
            {
                voice?.Stop();
            }
        }

        public static double CalculateNextSequenceDspTime(
            double timelineStart,
            double currentTime,
            float beatsPerMinute,
            int subdivisions,
            float leadTime,
            double lastScheduledTime)
        {
            var nextGrid = PrototypeMusicDirector.CalculateNextGridDspTime(
                timelineStart,
                currentTime,
                beatsPerMinute,
                subdivisions,
                leadTime);
            if (beatsPerMinute <= 0f || subdivisions <= 0 || lastScheduledTime <= 0d)
            {
                return nextGrid;
            }

            var stepDuration = 60d / beatsPerMinute / subdivisions;
            return Math.Max(nextGrid, lastScheduledTime + stepDuration);
        }

        public static float CalculateFadeInGain(double dspTime, double startDspTime, float duration)
        {
            if (duration <= 0f)
            {
                return 1f;
            }

            var normalized = Mathf.Clamp01((float)((dspTime - startDspTime) / duration));
            return Mathf.SmoothStep(0f, 1f, normalized);
        }

        public static double CalculateNextLoopBoundaryDspTime(
            double timelineStart,
            double currentTime,
            float beatsPerMinute,
            int subdivisions,
            int stepsPerLoop,
            float leadTime)
        {
            if (beatsPerMinute <= 0f || subdivisions <= 0 || stepsPerLoop <= 0)
            {
                return currentTime;
            }

            var loopDuration = 60d / beatsPerMinute / subdivisions * stepsPerLoop;
            var elapsed = Math.Max(0d, currentTime - timelineStart);
            var nextLoopIndex = Math.Floor(elapsed / loopDuration) + 1d;
            var targetTime = timelineStart + nextLoopIndex * loopDuration;
            if (targetTime - currentTime < leadTime)
            {
                targetTime += loopDuration;
            }

            return targetTime;
        }

        public static double CalculateNextSlotDspTime(
            double timelineStart,
            double currentTime,
            double earliestTime,
            float beatsPerMinute,
            int subdivisions,
            int stepsPerLoop,
            int slotIndex,
            float leadTime)
        {
            if (beatsPerMinute <= 0f || subdivisions <= 0 || stepsPerLoop <= 0)
            {
                return Math.Max(currentTime, earliestTime);
            }

            var stepDuration = 60d / beatsPerMinute / subdivisions;
            var loopDuration = stepDuration * stepsPerLoop;
            var firstSlotTime = timelineStart + Mathf.Clamp(slotIndex, 0, stepsPerLoop - 1) * stepDuration;
            var threshold = Math.Max(earliestTime, currentTime + Math.Max(0f, leadTime));
            if (firstSlotTime >= threshold)
            {
                return firstSlotTime;
            }

            var loopCount = Math.Ceiling((threshold - firstSlotTime) / loopDuration);
            return firstSlotTime + Math.Max(0d, loopCount) * loopDuration;
        }

        private sealed class SequencerStep
        {
            public SequencerStep(
                BeatColor color,
                bool accent,
                int slotIndex,
                int patternIndex,
                double nextLoopDspTime)
            {
                Color = color;
                Accent = accent;
                SlotIndex = slotIndex;
                PatternIndex = patternIndex;
                NextLoopDspTime = nextLoopDspTime;
            }

            public BeatColor Color { get; }
            public bool Accent { get; }
            public int SlotIndex { get; }
            public int PatternIndex { get; }
            public double NextLoopDspTime { get; set; }
        }
    }
}
