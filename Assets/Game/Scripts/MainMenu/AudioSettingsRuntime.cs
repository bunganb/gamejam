using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Single runtime entry point for menu and in-game audio settings.
/// Keeps the same PlayerPrefs values and mixer parameters across scenes.
/// </summary>
public static class AudioSettingsRuntime
{
    public const string MasterKey = "MasterVolume";
    public const string SfxKey = "SFXVolume";
    public const string MusicKey = "BGMVolume";

    public static void ApplySavedValues(AudioMixer mixer)
    {
        if (mixer == null)
            return;

        ApplyValue(mixer, "MasterVol", PlayerPrefs.GetFloat(MasterKey, 0.5f));
        ApplyValue(mixer, "SFXVol", PlayerPrefs.GetFloat(SfxKey, 0.5f));
        ApplyValue(mixer, "MusicVol", PlayerPrefs.GetFloat(MusicKey, 0.5f));
    }

    public static void ApplyValue(AudioMixer mixer, string exposedParameter, float sliderValue)
    {
        if (mixer == null)
            return;

        mixer.SetFloat(exposedParameter, SliderToDecibels(sliderValue));
    }

    public static float SliderToDecibels(float sliderValue)
    {
        // Preserve the project's existing volume response so saved settings do
        // not suddenly change when this shared path is introduced.
        var percentage = Mathf.Clamp01(sliderValue);
        return percentage < 0.5f
            ? Mathf.Lerp(-80f, 0f, percentage * 2f)
            : Mathf.Lerp(0f, 20f, (percentage - 0.5f) * 2f);
    }

    public static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }
}
