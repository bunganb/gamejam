using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Three-option graphics selector for the settings menu.
/// The ToggleGroup guarantees that only one quality option is selected.
/// </summary>
[DisallowMultipleComponent]
public sealed class GraphicQualityRadioGroup : MonoBehaviour
{
    private const string QualityPreferenceKey = "GraphicsQualityPreset";

    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private Toggle[] options = Array.Empty<Toggle>();
    [SerializeField] private TMP_Text[] optionLabels = Array.Empty<TMP_Text>();
    [SerializeField] private string[] labels = { "LOW", "MEDIUM", "HIGH" };

    private bool isApplyingSelection;

    private void Awake()
    {
        if (toggleGroup == null)
        {
            toggleGroup = GetComponent<ToggleGroup>();
        }

        if (options == null || options.Length == 0)
        {
            options = GetComponentsInChildren<Toggle>(true);
        }

        if (optionLabels == null || optionLabels.Length == 0)
        {
            optionLabels = GetComponentsInChildren<TMP_Text>(true);
            var filteredLabels = new System.Collections.Generic.List<TMP_Text>();
            foreach (var label in optionLabels)
            {
                if (label != null && label.name.StartsWith("OptionLabel_", StringComparison.Ordinal))
                {
                    filteredLabels.Add(label);
                }
            }

            optionLabels = filteredLabels.ToArray();
        }

        if (toggleGroup != null)
        {
            toggleGroup.allowSwitchOff = false;
        }

        ConfigureOptions();

        SetLabels();
        RegisterListeners();
        ApplySavedSelection();
    }

    private void OnDestroy()
    {
        UnregisterListeners();
    }

    private void RegisterListeners()
    {
        if (options == null)
        {
            return;
        }

        foreach (var option in options)
        {
            if (option != null)
            {
                option.onValueChanged.AddListener(HandleOptionChanged);
            }
        }
    }

    private void ConfigureOptions()
    {
        if (options == null)
        {
            return;
        }

        foreach (var option in options)
        {
            if (option == null)
            {
                continue;
            }

            option.group = toggleGroup;
            if (option.graphic == null)
            {
                var checkmark = option.transform.Find("Checkmark");
                if (checkmark != null)
                {
                    option.graphic = checkmark.GetComponent<Image>();
                }
            }
        }
    }

    private void UnregisterListeners()
    {
        if (options == null)
        {
            return;
        }

        foreach (var option in options)
        {
            if (option != null)
            {
                option.onValueChanged.RemoveListener(HandleOptionChanged);
            }
        }
    }

    private void SetLabels()
    {
        if (optionLabels == null || labels == null)
        {
            return;
        }

        var count = Mathf.Min(optionLabels.Length, labels.Length);
        for (var index = 0; index < count; index++)
        {
            if (optionLabels[index] != null)
            {
                optionLabels[index].text = labels[index];
            }
        }
    }

    private void ApplySavedSelection()
    {
        if (options == null || options.Length == 0)
        {
            return;
        }

        var selectedIndex = PlayerPrefs.GetInt(QualityPreferenceKey, 1);
        selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Length - 1);

        isApplyingSelection = true;
        for (var index = 0; index < options.Length; index++)
        {
            if (options[index] != null)
            {
                options[index].SetIsOnWithoutNotify(index == selectedIndex);
            }
        }

        isApplyingSelection = false;
        ApplyQualityPreset(selectedIndex);
    }

    private void HandleOptionChanged(bool isOn)
    {
        if (!isOn || isApplyingSelection || options == null)
        {
            return;
        }

        for (var index = 0; index < options.Length; index++)
        {
            if (options[index] != null && options[index].isOn)
            {
                PlayerPrefs.SetInt(QualityPreferenceKey, index);
                PlayerPrefs.Save();
                ApplyQualityPreset(index);
                return;
            }
        }
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

        // The project currently contains Mobile and PC quality assets only.
        // Keep the three UI choices useful by applying a small runtime profile
        // on top of those assets instead of pretending Medium and High are the
        // same setting.
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

        // This changes the internal render buffer, not the window resolution.
        // It gives Low and Medium a predictable GPU saving while preserving UI
        // layout and aspect ratio.
        ScalableBufferManager.ResizeBuffers(renderScale, renderScale);
    }
}
