using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [CreateAssetMenu(menuName = "Game Jam/Level Music Definition", fileName = "LevelMusic_01")]
    public sealed class LevelMusicDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = "Level_01";
        [SerializeField, Range(40f, 220f)] private float bpm = 130f;
        [SerializeField, Range(1, 4)] private int subdivisionsPerBeat = 2;
        [SerializeField, Range(1, 32)] private int loopStepCount = 8;
        [SerializeField] private AudioClip baseHarmony;
        [SerializeField] private AudioClip secondaryLayer;
        [SerializeField, Range(0f, 1f)] private float secondaryLayerThreshold = 1f;
        [SerializeField] private AudioClip buildLayer;
        [SerializeField, Range(0f, 1f)] private float buildLayerThreshold = 0.8f;
        [SerializeField] private AudioClip topLoopLayer;
        [SerializeField, Min(-1)] private int topLoopUnlockRow = -1;
        [SerializeField] private AudioClip fullSong;
        [SerializeField] private AudioClip[] noteSamples = Array.Empty<AudioClip>();
        [SerializeField] private int[] sequenceSlots = Array.Empty<int>();
        [SerializeField] private float[] noteVolumes = Array.Empty<float>();
        [SerializeField, Range(0f, 0.25f)] private float tileFadeInDuration = 0.06f;
        [SerializeField, Range(0f, 2f)] private float loopEntryDelayBeats = 0.5f;

        public string LevelId => levelId;
        public float Bpm => bpm;
        public int SubdivisionsPerBeat => subdivisionsPerBeat;
        public int LoopStepCount => loopStepCount;
        public AudioClip BaseHarmony => baseHarmony;
        public AudioClip SecondaryLayer => secondaryLayer;
        public float SecondaryLayerThreshold => secondaryLayerThreshold;
        public AudioClip BuildLayer => buildLayer;
        public float BuildLayerThreshold => buildLayerThreshold;
        public AudioClip TopLoopLayer => topLoopLayer;
        public int TopLoopUnlockRow => topLoopUnlockRow;
        public AudioClip FullSong => fullSong;
        public IReadOnlyList<AudioClip> NoteSamples => noteSamples;
        public IReadOnlyList<int> SequenceSlots => sequenceSlots;
        public IReadOnlyList<float> NoteVolumes => noteVolumes;
        public float TileFadeInDuration => tileFadeInDuration;
        public float LoopEntryDelayBeats => loopEntryDelayBeats;

#if UNITY_EDITOR
        public void SetData(
            string id,
            float beatsPerMinute,
            int subdivisions,
            int stepsPerLoop,
            AudioClip harmony,
            AudioClip secondary,
            float secondaryThreshold,
            AudioClip build,
            float buildThreshold,
            AudioClip topLoop,
            int topLoopRow,
            AudioClip completeSong,
            IReadOnlyList<AudioClip> samples,
            IReadOnlyList<int> slots,
            IReadOnlyList<float> volumes,
            float tileFadeDuration = 0.06f,
            float loopDelayBeats = 0.5f)
        {
            levelId = id;
            bpm = Mathf.Clamp(beatsPerMinute, 40f, 220f);
            subdivisionsPerBeat = Mathf.Clamp(subdivisions, 1, 4);
            loopStepCount = Mathf.Clamp(stepsPerLoop, 1, 32);
            baseHarmony = harmony;
            secondaryLayer = secondary;
            secondaryLayerThreshold = Mathf.Clamp01(secondaryThreshold);
            buildLayer = build;
            buildLayerThreshold = Mathf.Clamp01(buildThreshold);
            topLoopLayer = topLoop;
            topLoopUnlockRow = Mathf.Max(-1, topLoopRow);
            fullSong = completeSong;
            noteSamples = Copy(samples);
            sequenceSlots = Copy(slots);
            noteVolumes = Copy(volumes);
            tileFadeInDuration = Mathf.Clamp(tileFadeDuration, 0f, 0.25f);
            loopEntryDelayBeats = Mathf.Clamp(loopDelayBeats, 0f, 2f);
        }
#endif

        public bool TryValidate(int expectedNoteCount, out string error)
        {
            if (string.IsNullOrWhiteSpace(levelId) || baseHarmony == null || fullSong == null)
            {
                error = "Music profile requires a level id, base harmony, and full song.";
                return false;
            }

            if (noteSamples == null || sequenceSlots == null || noteVolumes == null ||
                noteSamples.Length != expectedNoteCount || sequenceSlots.Length != expectedNoteCount ||
                noteVolumes.Length != expectedNoteCount)
            {
                error = $"{levelId} music profile must contain exactly {expectedNoteCount} samples, slots, and volumes.";
                return false;
            }

            for (var index = 0; index < expectedNoteCount; index++)
            {
                if (noteSamples[index] == null || sequenceSlots[index] < 0 || sequenceSlots[index] >= loopStepCount)
                {
                    error = $"{levelId} music note {index + 1} has a missing sample or invalid slot.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
            {
                return Array.Empty<T>();
            }

            var result = new T[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }
#endif
    }
}
