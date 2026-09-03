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
        masterSlider.onValueChanged.AddListener(UpdateMasterValue);
        sfxSlider.onValueChanged.AddListener(UpdateSFXValue);
        bgmSlider.onValueChanged.AddListener(UpdateBGMValue);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        // Load nilai yang tersimpan di PlayerPrefs (default 0.5f untuk audio)
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.5f);

        // Load nilai Fullscreen (default 1 berarti true/fullscreen)
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        fullscreenToggle.isOn = isFullscreen; 

        // Panggil fungsi sekali saat mulai agar UI text dan Mixer langsung menyesuaikan
        UpdateMasterValue(masterSlider.value);
        UpdateSFXValue(sfxSlider.value);
        UpdateBGMValue(bgmSlider.value);
        
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
        float percentage = Mathf.InverseLerp(masterSlider.minValue, masterSlider.maxValue, value);

        int displayValue = Mathf.RoundToInt(percentage * 100);
        masterValueText.text = displayValue.ToString(); 

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
        
        mainMixer.SetFloat("MasterVol", dbVolume);
    }

    private void UpdateSFXValue(float value)
    {
        float percentage = Mathf.InverseLerp(sfxSlider.minValue, sfxSlider.maxValue, value);

        int displayValue = Mathf.RoundToInt(percentage * 100);
        sfxValueText.text = displayValue.ToString(); 

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
        
        mainMixer.SetFloat("SFXVol", dbVolume);
    }

    private void UpdateBGMValue(float value)
    {
        float percentage = Mathf.InverseLerp(bgmSlider.minValue, bgmSlider.maxValue, value);
        
        int displayValue = Mathf.RoundToInt(percentage * 100);
        bgmValueText.text = displayValue.ToString();
        
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
        
        mainMixer.SetFloat("MusicVol", dbVolume);
    }
}