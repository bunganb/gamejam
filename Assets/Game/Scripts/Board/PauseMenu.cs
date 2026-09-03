using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Panel Pause")]
    public GameObject pauseMenuUI; // Masukkan objek 'PauseMenu' panel utama
    public Button resumeButton;
    public Button exitButton;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer; // Masukkan file AudioMixer kamu

    [Header("Master Settings")]
    public Slider masterSlider;
    public TextMeshProUGUI masterValueText;

    [Header("SFX Settings")]
    public Slider sfxSlider;
    public TextMeshProUGUI sfxValueText;

    [Header("BGM Settings")]
    public Slider musicSlider;
    public TextMeshProUGUI musicValueText;

    [Header("Nama Scene Main Menu")]
    public string mainMenuSceneName = "MainMenu"; // Sesuaikan nama scene Main Menu kamu

    private bool isPaused;

    void Start()
    {
        // 1. Daftarkan listener slider agar otomatis merespons saat digeser
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);

        // 2. Load nilai yang tersimpan di PlayerPrefs (default 0.5f / 50%)
        if (masterSlider != null) masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.5f);

        // Panggil fungsi sekali saat mulai agar tampilan teks & audio langsung sinkron
        if (masterSlider != null) SetMasterVolume(masterSlider.value);
        if (sfxSlider != null) SetSFXVolume(sfxSlider.value);
        if (musicSlider != null) SetMusicVolume(musicSlider.value);

        BindPauseButtons();
        Resume();
    }

    private void BindPauseButtons()
    {
        if (pauseMenuUI == null) return;

        if (resumeButton == null) resumeButton = FindButton("Resume");
        if (exitButton == null) exitButton = FindButton("Exit");

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (exitButton != null) exitButton.onClick.AddListener(LoadMainMenu);
    }

    private Button FindButton(string buttonName)
    {
        Button[] buttons = pauseMenuUI.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.name.Equals(buttonName, System.StringComparison.OrdinalIgnoreCase))
                return button;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && label.text.Trim().Equals(buttonName, System.StringComparison.OrdinalIgnoreCase))
                return button;
        }

        return null;
    }

    void Update()
    {
        // Tombol ESC untuk Pause / Unpause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; 
        isPaused = false;
    }

    public void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; 
        isPaused = true;
    }

    // --- PENGATURAN AUDIO & TEKS (0 - 100) ---

    public void SetMasterVolume(float value)
    {
        float percentage = Mathf.InverseLerp(masterSlider.minValue, masterSlider.maxValue, value);
        int displayValue = Mathf.RoundToInt(percentage * 100);
        
        if (masterValueText != null) masterValueText.text = displayValue.ToString();

        PlayerPrefs.SetFloat("MasterVolume", value);

        float dbVolume;
        if (percentage < 0.5f)
            dbVolume = Mathf.Lerp(-80f, 0f, percentage * 2f);
        else
            dbVolume = Mathf.Lerp(0f, 20f, (percentage - 0.5f) * 2f);

        if (audioMixer != null) audioMixer.SetFloat("MasterVol", dbVolume);
    }

    public void SetSFXVolume(float value)
    {
        float percentage = Mathf.InverseLerp(sfxSlider.minValue, sfxSlider.maxValue, value);
        int displayValue = Mathf.RoundToInt(percentage * 100);
        
        if (sfxValueText != null) sfxValueText.text = displayValue.ToString();

        PlayerPrefs.SetFloat("SFXVolume", value);

        float dbVolume;
        if (percentage < 0.5f)
            dbVolume = Mathf.Lerp(-80f, 0f, percentage * 2f);
        else
            dbVolume = Mathf.Lerp(0f, 20f, (percentage - 0.5f) * 2f);

        if (audioMixer != null) audioMixer.SetFloat("SFXVol", dbVolume);
    }

    public void SetMusicVolume(float value)
    {
        float percentage = Mathf.InverseLerp(musicSlider.minValue, musicSlider.maxValue, value);
        int displayValue = Mathf.RoundToInt(percentage * 100);
        
        if (musicValueText != null) musicValueText.text = displayValue.ToString();

        PlayerPrefs.SetFloat("BGMVolume", value);

        float dbVolume;
        if (percentage < 0.5f)
            dbVolume = Mathf.Lerp(-80f, 0f, percentage * 2f);
        else
            dbVolume = Mathf.Lerp(0f, 20f, (percentage - 0.5f) * 2f);

        if (audioMixer != null) audioMixer.SetFloat("MusicVol", dbVolume);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(mainMenuSceneName);
    }
}