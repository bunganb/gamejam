using System;
using System.IO;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    /// <summary>Re-cuts Level 1's existing sequencer slots from the revised stems.</summary>
    public static class LevelOneAudioRevisionInstaller
    {
        private const string StemRoot = "Assets/Game/Art/Beat/Level_1/";
        private const string ShotRoot = "Assets/Game/Art/Beat/OneShots/";
        private const string MusicPath = "Assets/Game/Data/Music/LevelMusic_01.asset";
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const float Bpm = 130f;
        private const int StepsPerBeat = 2;

        private readonly struct Cut
        {
            public readonly string Stem;
            public readonly string Output;
            public readonly int Slot;
            public readonly float Seconds;
            public readonly float TargetPeak;

            public Cut(string stem, string output, int slot, float seconds, float targetPeak)
            {
                Stem = stem;
                Output = output;
                Slot = slot;
                Seconds = seconds;
                TargetPeak = targetPeak;
            }
        }

        private static readonly Cut[] Cuts =
        {
            new("2_DRUM KICK.wav", "Magenta_Kick.wav", 0, .30f, .55f),
            new("3_KETIPUNG 1.wav", "Yellow_Ketipung1.wav", 1, .22f, .54f),
            new("3_KETIPUNG 1.wav", "Yellow_Ketipung1_Accent.wav", 7, .26f, .43f),
            new("4_KETIPUNG 2.wav", "Blue_Ketipung2.wav", 2, .22f, .53f),
            new("4_KETIPUNG 2.wav", "Blue_Ketipung2_Accent.wav", 6, .28f, .48f)
        };

        [MenuItem("Game Jam/Gameplay/Apply Revised Level 1 Audio")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before revising Level 1 audio.");

            var profile = AssetDatabase.LoadAssetAtPath<LevelMusicDefinition>(MusicPath);
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                "Assets/Game/Data/Levels/Level_01_Prototype.asset");
            if (profile == null || level == null)
                throw new InvalidOperationException("Level 1 music/level asset is missing.");

            var harmony = RequireClip(StemRoot + "1_HARMONY.wav");
            var fullSong = RequireClip(StemRoot + "5_FULLSONG.wav");
            if (Math.Abs(harmony.length - fullSong.length) > .02f)
                throw new InvalidOperationException("Level 1 harmony and full song have different loop durations.");

            foreach (var cut in Cuts)
                WriteCut(RequireClip(StemRoot + cut.Stem), ShotRoot + cut.Output,
                    cut.Slot, cut.Seconds, cut.TargetPeak);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var samples = new AudioClip[profile.NoteSamples.Count];
            for (var index = 0; index < samples.Length; index++)
            {
                var oldSample = profile.NoteSamples[index];
                if (oldSample == null)
                    throw new InvalidOperationException($"Level 1 note {index + 1} is missing its sample.");
                samples[index] = RequireClip(ShotRoot + oldSample.name + ".wav");
            }

            profile.SetData(
                profile.LevelId,
                Bpm,
                profile.SubdivisionsPerBeat,
                profile.LoopStepCount,
                harmony,
                null,
                profile.SecondaryLayerThreshold,
                null,
                profile.BuildLayerRemainingNotes,
                null,
                profile.TopLoopUnlockRow,
                fullSong,
                samples,
                profile.SequenceSlots,
                profile.NoteVolumes,
                profile.TileFadeInDuration,
                profile.LoopEntryDelayBeats);
            if (!profile.TryValidate(level.TotalNotes, out var error))
                throw new InvalidOperationException(error);
            EditorUtility.SetDirty(profile);

            UpdateSceneAudioSources(harmony, fullSong);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL_1_AUDIO_REVISED: harmony/full song linked, five one-shots re-cut, obsolete bass cleared.");
        }

        private static AudioClip RequireClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            return clip != null ? clip : throw new InvalidOperationException($"Missing audio clip: {path}");
        }

        private static void WriteCut(
            AudioClip source, string outputPath, int slot, float duration, float targetPeak)
        {
            if (source.frequency != 44100 || source.channels != 2)
                throw new InvalidOperationException($"{source.name} must be stereo 44.1 kHz for the Level 1 cut.");

            if (source.loadState == AudioDataLoadState.Unloaded)
                source.LoadAudioData();

            var startFrame = (int)Math.Round(slot * 60d / Bpm / StepsPerBeat * source.frequency);
            var frameCount = Mathf.RoundToInt(duration * source.frequency);
            if (startFrame + frameCount > source.samples)
                throw new InvalidOperationException($"Cut {outputPath} extends past its source clip.");

            var samples = new float[frameCount * source.channels];
            if (!source.GetData(samples, startFrame))
                throw new InvalidOperationException($"Could not decode samples from {source.name}.");

            // New exported stems are substantially quieter than the previous
            // one-shots. Match the old peak range while retaining each hit's dynamics.
            var peak = 0f;
            foreach (var sample in samples)
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            if (peak < .001f)
                throw new InvalidOperationException($"Cut {outputPath} contains no audible hit.");
            var gainFactor = targetPeak / peak;
            for (var index = 0; index < samples.Length; index++)
                samples[index] *= gainFactor;

            // Short tail ramp prevents a click when the one-shot ends before the next beat.
            var fadeFrames = Mathf.Min(frameCount, Mathf.RoundToInt(.008f * source.frequency));
            for (var frame = frameCount - fadeFrames; frame < frameCount; frame++)
            {
                var gain = (frameCount - frame - 1f) / fadeFrames;
                for (var channel = 0; channel < source.channels; channel++)
                    samples[frame * source.channels + channel] *= gain;
            }

            using var stream = File.Create(outputPath);
            using var writer = new BinaryWriter(stream);
            var dataBytes = samples.Length * sizeof(short);
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)source.channels);
            writer.Write(source.frequency);
            writer.Write(source.frequency * source.channels * sizeof(short));
            writer.Write((short)(source.channels * sizeof(short)));
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);
            foreach (var sample in samples)
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * 32767f));
        }

        private static void UpdateSceneAudioSources(AudioClip harmony, AudioClip fullSong)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                    {
                        var parent = source.transform.parent;
                        if (parent == null || parent.name != "PrototypeMusic")
                            continue;

                        switch (source.name)
                        {
                            case "Harmony_01": source.clip = harmony; break;
                            case "FullSong_06": source.clip = fullSong; break;
                            case "DrumKick_02":
                            case "Ketipung1_03":
                            case "Ketipung2_04":
                            case "BassGuitar_05": source.clip = null; break;
                        }
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
