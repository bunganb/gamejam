"""Build deterministic PCM16 one-shots for Level 2-6 from source stems."""

from pathlib import Path
import struct
import wave


ROOT = Path(__file__).resolve().parents[2]
BEAT_ROOT = ROOT / "Assets" / "Game" / "Art" / "Beat"

MAPPINGS = {
    2: [
        ("2_DRUM KICK.wav", 0, "Kick"), ("2_DRUM KICK.wav", 6, "Kick"),
        ("2_DRUM KICK.wav", 10, "Kick"), ("2_DRUM KICK.wav", 12, "Kick"),
        ("3_KETIPUNG 1.wav", 2, "Ketipung1"), ("3_KETIPUNG 1.wav", 8, "Ketipung1"),
        ("3_KETIPUNG 1.wav", 14, "Ketipung1"),
        ("4_KETIPUNG 2.wav", 4, "Ketipung2"), ("4_KETIPUNG 2.wav", 11, "Ketipung2"),
    ],
    3: [
        ("2_DRUM KICK.wav", 0, "Kick"), ("2_DRUM KICK.wav", 6, "Kick"),
        ("2_DRUM KICK.wav", 10, "Kick"), ("2_DRUM KICK.wav", 12, "Kick"),
        ("3_KETIPUNG 1.wav", 2, "Ketipung1"), ("3_KETIPUNG 1.wav", 6, "Ketipung1"),
        ("3_KETIPUNG 1.wav", 10, "Ketipung1"), ("3_KETIPUNG 1.wav", 14, "Ketipung1"),
        ("4_KETIPUNG 2.wav", 4, "Ketipung2"), ("4_KETIPUNG 2.wav", 9, "Ketipung2"),
        ("4_KETIPUNG 2.wav", 12, "Ketipung2"),
    ],
    4: [
        ("2_DRUM KICK.wav", 0, "Kick"), ("2_DRUM KICK.wav", 6, "Kick"),
        ("2_DRUM KICK.wav", 10, "Kick"), ("2_DRUM KICK.wav", 12, "Kick"),
        ("3_KETIPUNG 1.wav", 2, "Ketipung1"), ("3_KETIPUNG 1.wav", 6, "Ketipung1"),
        ("3_KETIPUNG 1.wav", 14, "Ketipung1"),
        ("4_KETIPUNG 2.wav", 8, "Ketipung2Roll", 0.42),
        ("5_KETIPUNG 3.wav", 4, "Ketipung3"), ("5_KETIPUNG 3.wav", 12, "Ketipung3"),
    ],
    5: [
        ("2_DRUM KICK.wav", 0, "Kick"), ("2_DRUM KICK.wav", 6, "Kick"),
        ("2_DRUM KICK.wav", 10, "Kick"), ("2_DRUM KICK.wav", 12, "Kick"),
        ("3_KETIPUNG 1.wav", 2, "Ketipung1"), ("3_KETIPUNG 1.wav", 6, "Ketipung1"),
        ("4_KETIPUNG 2.wav", 4, "Ketipung2"), ("4_KETIPUNG 2.wav", 12, "Ketipung2"),
        ("5_KETIPUNG 3.wav", 8, "Ketipung3Roll", 0.42),
        ("5_KETIPUNG 3.wav", 14, "Ketipung3Accent"),
    ],
    6: [
        ("3_KETIPUNG 1.wav", 0, "Ketipung1"), ("3_KETIPUNG 1.wav", 6, "Ketipung1"),
        ("3_KETIPUNG 1.wav", 10, "Ketipung1"), ("3_KETIPUNG 1.wav", 12, "Ketipung1"),
        ("4_KETIPUNG 2.wav", 2, "Ketipung2"), ("4_KETIPUNG 2.wav", 6, "Ketipung2"),
        ("4_KETIPUNG 2.wav", 8, "Ketipung2Roll", 0.42),
        ("4_KETIPUNG 2.wav", 14, "Ketipung2Accent"),
        ("5_KETIPUNG 3.wav", 4, "Ketipung3"), ("5_KETIPUNG 3.wav", 12, "Ketipung3"),
        ("6_SNARE & HIHAT.wav", 8, "SnareTrigger"),
    ],
}


def cut_one_shot(source_path: Path, output_path: Path, slot: int, duration: float = 0.20) -> None:
    with wave.open(str(source_path), "rb") as source:
        channels = source.getnchannels()
        sample_width = source.getsampwidth()
        sample_rate = source.getframerate()
        frame_count = source.getnframes()
        if sample_width != 2:
            raise ValueError(f"Only PCM16 is supported: {source_path}")
        raw = source.readframes(frame_count)

    samples = list(struct.unpack("<" + "h" * (len(raw) // 2), raw))
    bar_duration = frame_count / sample_rate / 4.0
    onset_frame = round(slot * bar_duration / 16.0 * sample_rate)
    pre_roll_frames = round(0.003 * sample_rate)
    requested_frames = round(duration * sample_rate)
    source_start = max(0, onset_frame - pre_roll_frames)
    available_pre_roll = onset_frame - source_start
    output_frames = requested_frames + pre_roll_frames
    result = [0] * (output_frames * channels)
    destination_start = pre_roll_frames - available_pre_roll
    copy_frames = min(output_frames - destination_start, frame_count - source_start)
    for frame in range(copy_frames):
        for channel in range(channels):
            result[(destination_start + frame) * channels + channel] = samples[(source_start + frame) * channels + channel]

    fade_frames = max(1, round(0.008 * sample_rate))
    for fade_index in range(fade_frames):
        gain = 1.0 - fade_index / fade_frames
        frame = output_frames - fade_frames + fade_index
        for channel in range(channels):
            sample_index = frame * channels + channel
            result[sample_index] = round(result[sample_index] * gain)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(output_path), "wb") as output:
        output.setnchannels(channels)
        output.setsampwidth(sample_width)
        output.setframerate(sample_rate)
        output.writeframes(struct.pack("<" + "h" * len(result), *result))


def main() -> None:
    for level, notes in MAPPINGS.items():
        source_folder = BEAT_ROOT / f"Level_{level}"
        output_folder = BEAT_ROOT / "OneShots" / f"Level_{level:02}"
        for note_index, mapping in enumerate(notes, 1):
            source_name, slot, label, *custom_duration = mapping
            duration = custom_duration[0] if custom_duration else 0.20
            output = output_folder / f"Note_{note_index:02}_{label}_S{slot:02}.wav"
            cut_one_shot(source_folder / source_name, output, slot, duration)
            print(output.relative_to(ROOT))


if __name__ == "__main__":
    main()
