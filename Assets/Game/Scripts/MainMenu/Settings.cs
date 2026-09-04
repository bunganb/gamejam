using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class Settings : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;

    [Header("Master Settings")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterValueText;

    [Header("SFX Settings")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    [Header("BGM Settings")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;

    [Header("Display Settings")]
    [SerializeField] private Toggle fullscreenToggle;

    void Start()
    {
        // Daftarkan listener untuk setiap slider dan toggle
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(UpdateMasterValue);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(UpdateSFXValue);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(UpdateBGMValue);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        // Load nilai yang tersimpan di PlayerPrefs (default 0.5f untuk audio)
        if (masterSlider != null) masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        if (bgmSlider != null) bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.5f);

        // Load nilai Fullscreen (default 1 berarti true/fullscreen)
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;

        // Panggil fungsi sekali saat mulai agar UI text dan Mixer langsung menyesuaikan
        if (masterSlider != null) UpdateMasterValue(masterSlider.value);
        if (sfxSlider != null) UpdateSFXValue(sfxSlider.value);
        if (bgmSlider != null) UpdateBGMValue(bgmSlider.value);
        
        // Pastikan mode layar sesuai saat game pertama kali dijalankan
        Screen.fullScreen = isFullscreen;
    }

    private void SetFullscreen(bool isFullscreen)
    {
        // Mengubah mode layar Unity
        Screen.fullScreen = isFullscreen;

        // Menyimpan pengaturan (1 untuk true, 0 untuk false)
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void UpdateMasterValue(float value)
    {
        if (masterSlider == null) return;
        float percentage = Mathf.InverseLerp(masterSlider.minValue, masterSlider.maxValue, value);

        int displayValue = Mathf.RoundToInt(percentage * 100);
        if (masterValueText != null) masterValueText.text = displayValue.ToString();

        PlayerPrefs.SetFloat("MasterVolume", value);

        float dbVolume;
        if (percentage < 0.5f)
        {
            dbVolume = Mathf.Lerp(-80f, 0f, percentage * 2f);
        }
        else
        {
            dbVolume = Mathf.Lerp(0f, 20f, (percentage - 0.5f) * 2f);
        }
        
        if (mainMixer != null) mainMixer.SetFloat("MasterVol", dbVolume);
    }

    private void UpdateSFXValue(float value)
    {
        if (sfxSlider == null) return;
        float percentage = Mathf.InverseLerp(sfxSlider.minValue, sfxSlider.maxValue, value);

        int displayValue = Mathf.RoundToInt(percentage * 100);
        if (sfxValueText != null) sfxValueText.text = displayValue.ToString();

        PlayerPrefs.SetFloat("SFXVolume", value);

        float dbVolume;
        if (percentage < 0.5f)
        {
            dbVolume = Mathf.Lerp(-80f, 0f, percentage * 2f);
        }
        else
        {
            dbVolume = Mathf.Lerp(0f, 20f, (percentage - 0.5f) * 2f);
        }
        
        if (mainMixer != null) mainMixer.SetFloat("SFXVol", dbVolume);
    }

    private void UpdateBGMValue(float value)
    {
        if (bgmSlider == null) return;
        float percentage = Mathf.InverseLerp(bgmSlider.minValue, bgmSlider.maxValue, value);
        
        int displayValue = Mathf.RoundToInt(percentage * 100);
        if (bgmValueText != null) bgmValueText.text = displayValue.ToString();
        
        PlayerPrefs.SetFloat("BGMVolume", value);

        float dbVolume;
        if (percentage < 0.5f)
        {
            dbVolume = Mathf.Lerp(-80f, 0f, percentage * 2f);
        }
        else
        {
            dbVolume = Mathf.Lerp(0f, 20f, (percentage - 0.5f) * 2f);
        }
        
        if (mainMixer != null) mainMixer.SetFloat("MusicVol", dbVolume);
    }
}
