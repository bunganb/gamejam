using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Horizontal Option Selector untuk memilih preset kualitas grafik.
/// Menggunakan tombol panah kiri/kanan dan Teks nilai.
/// </summary>
[DisallowMultipleComponent]
public sealed class GraphicQualityRadioGroup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("Configuration")]
    [SerializeField] private string qualityPreferenceKey = "GraphicsQualityPreset";
    [SerializeField] private string[] labels = { "LOW", "MEDIUM", "HIGH" };
    [SerializeField] private int defaultIndex = 1; // Default: MEDIUM

    private int currentIndex;

    private void ResolveButtonReferences()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            var buttonName = button.name.ToLowerInvariant();
            if (leftButton == null && (buttonName.Contains("left") || buttonName.Contains("previous") || buttonName.Contains("back")))
            {
                leftButton = button;
            }

            if (rightButton == null && (buttonName.Contains("right") || buttonName.Contains("next") || buttonName.Contains("forward")))
            {
                rightButton = button;
            }
        }

        if (buttons.Length >= 2)
        {
            leftButton ??= buttons[0];
            rightButton ??= buttons[buttons.Length - 1];
        }
    }

    private void Awake()
    {
        ResolveButtonReferences();
        RegisterListeners();
        ApplySavedSelection();
    }

    private void OnDestroy()
    {
        UnregisterListeners();
    }

    private void RegisterListeners()
    {
        if (leftButton != null)
        {
            leftButton.onClick.AddListener(PreviousOption);
        }

        if (rightButton != null)
        {
            rightButton.onClick.AddListener(NextOption);
        }
    }

    private void UnregisterListeners()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(PreviousOption);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(NextOption);
        }
    }

    public void NextOption()
    {
        if (labels == null || labels.Length == 0) return;

        currentIndex = (currentIndex + 1) % labels.Length;
        UpdateUIAndApplyQuality();
    }

    public void PreviousOption()
    {
        if (labels == null || labels.Length == 0) return;

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = labels.Length - 1;
        }

        UpdateUIAndApplyQuality();
    }

    private void ApplySavedSelection()
    {
        if (labels == null || labels.Length == 0) return;

        currentIndex = PlayerPrefs.GetInt(qualityPreferenceKey, defaultIndex);
        currentIndex = Mathf.Clamp(currentIndex, 0, labels.Length - 1);

        UpdateUIAndApplyQuality();
    }

    private void UpdateUIAndApplyQuality()
    {
        // Update tampilan teks
        if (valueText != null && labels.Length > 0)
        {
            valueText.text = labels[currentIndex];
        }

        // Simpan ke PlayerPrefs
        PlayerPrefs.SetInt(qualityPreferenceKey, currentIndex);
        PlayerPrefs.Save();

        // Terapkan ke Unity Quality Settings
        ApplyQualityPreset(currentIndex);
    }

    private static void ApplyQualityPreset(int presetIndex)
    {
        var qualityNames = QualitySettings.names;
        if (qualityNames == null || qualityNames.Length == 0)
        {
            return;
        }

        var expectedName = presetIndex switch
        {
            0 => "low",
            1 => "medium",
            _ => "high"
        };

        var qualityIndex = Array.FindIndex(
            qualityNames,
            name => name.IndexOf(expectedName, StringComparison.OrdinalIgnoreCase) >= 0);

        if (qualityIndex < 0)
        {
            qualityIndex = presetIndex == 0
                ? 0
                : presetIndex == 1
                    ? qualityNames.Length / 2
                    : qualityNames.Length - 1;
        }

        QualitySettings.SetQualityLevel(Mathf.Clamp(qualityIndex, 0, qualityNames.Length - 1), true);

        switch (Mathf.Clamp(presetIndex, 0, 2))
        {
            case 0:
                ApplyRuntimeProfile(
                    renderScale: 0.80f,
                    shadowDistance: 20f,
                    lodBias: 0.7f,
                    maximumLodLevel: 1,
                    pixelLightCount: 1,
                    shadowQuality: ShadowQuality.HardOnly,
                    textureMipmapLimit: 1,
                    reflectionProbes: false,
                    softParticles: false,
                    particleRaycastBudget: 64);
                break;
            case 1:
                ApplyRuntimeProfile(
                    renderScale: 0.90f,
                    shadowDistance: 32f,
                    lodBias: 1f,
                    maximumLodLevel: 0,
                    pixelLightCount: 2,
                    shadowQuality: ShadowQuality.HardOnly,
                    textureMipmapLimit: 0,
                    reflectionProbes: true,
                    softParticles: true,
                    particleRaycastBudget: 128);
                break;
            default:
                ApplyRuntimeProfile(
                    renderScale: 1f,
                    shadowDistance: 50f,
                    lodBias: 2f,
                    maximumLodLevel: 0,
                    pixelLightCount: 4,
                    shadowQuality: ShadowQuality.All,
                    textureMipmapLimit: 0,
                    reflectionProbes: true,
                    softParticles: true,
                    particleRaycastBudget: 256);
                break;
        }
    }

    private static void ApplyRuntimeProfile(
        float renderScale,
        float shadowDistance,
        float lodBias,
        int maximumLodLevel,
        int pixelLightCount,
        ShadowQuality shadowQuality,
        int textureMipmapLimit,
        bool reflectionProbes,
        bool softParticles,
        int particleRaycastBudget)
    {
        QualitySettings.shadowDistance = shadowDistance;
        QualitySettings.lodBias = lodBias;
        QualitySettings.maximumLODLevel = maximumLodLevel;
        QualitySettings.pixelLightCount = pixelLightCount;
        QualitySettings.shadows = shadowQuality;
        QualitySettings.globalTextureMipmapLimit = textureMipmapLimit;
        QualitySettings.realtimeReflectionProbes = reflectionProbes;
        QualitySettings.softParticles = softParticles;
        QualitySettings.particleRaycastBudget = particleRaycastBudget;
        QualitySettings.anisotropicFiltering = textureMipmapLimit > 0
            ? AnisotropicFiltering.Disable
            : AnisotropicFiltering.Enable;

        ScalableBufferManager.ResizeBuffers(renderScale, renderScale);
    }
}