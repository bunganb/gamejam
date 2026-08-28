using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class Settings : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private TextMeshProUGUI bgmValueText;

    void Start()
    {
        sfxSlider.onValueChanged.AddListener(UpdateSFXValue);
        bgmSlider.onValueChanged.AddListener(UpdateBGMValue);

        // Load nilai yang tersimpan (default 0.5f)
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.5f);

        UpdateSFXValue(sfxSlider.value);
        UpdateBGMValue(bgmSlider.value);
    }

    private void UpdateSFXValue(float value)
    {
        float percentage = Mathf.InverseLerp(sfxSlider.minValue, sfxSlider.maxValue, value);

        int displayValue = Mathf.RoundToInt(percentage * 100);
        sfxValueText.text = displayValue.ToString(); 

        PlayerPrefs.SetFloat("SFXVolume", value);

        // Membagi dua jalur perhitungan agar 50 = 0 dB
        float dbVolume;
        if (percentage < 0.5f)
        {
            // Jika slider di bawah 50, hitung dari -80 sampai 0
            // Dikali 2 agar persentase 0.0-0.5 menjadi skala penuh 0.0-1.0 untuk Lerp
            dbVolume = Mathf.Lerp(-80f, 0f, percentage * 2f);
        }
        else
        {
            // Jika slider 50 ke atas, hitung dari 0 sampai 20
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