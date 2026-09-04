#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuGraphicQualityInstaller
{
    private const string ScenePath = "Assets/Game/Scenes/MainMenu.unity";
    private const string FontPath = "Assets/Fonts/Creepy-Story SDF.asset";

    [MenuItem("Game Jam/Install Graphics Quality Radio Buttons")]
    public static void Install()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var graphic = GameObject.Find("MainMenu/Settings/SoundSetting/Graphic");
        if (graphic == null)
        {
            Debug.LogError($"Could not find {ScenePath}: MainMenu/Settings/SoundSetting/Graphic");
            return;
        }

        var group = graphic.GetComponent<ToggleGroup>() ?? graphic.AddComponent<ToggleGroup>();
        group.allowSwitchOff = false;

        var radio = graphic.GetComponent<GraphicQualityRadioGroup>() ?? graphic.AddComponent<GraphicQualityRadioGroup>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var optionNames = new[] { "Background", "Background (1)", "Background (2)" };
        var labelNames = new[] { "LOW", "MEDIUM", "HIGH" };

        for (var index = 0; index < optionNames.Length; index++)
        {
            var optionTransform = graphic.transform.Find(optionNames[index]);
            if (optionTransform == null)
            {
                continue;
            }

            var option = optionTransform.GetComponent<Toggle>() ?? optionTransform.gameObject.AddComponent<Toggle>();
            option.group = group;
            option.targetGraphic = optionTransform.GetComponent<Image>();
            option.graphic = optionTransform.Find("Checkmark")?.GetComponent<Image>();
            option.SetIsOnWithoutNotify(index == 1);

            var labelTransform = optionTransform.Find($"OptionLabel_{labelNames[index]}");
            if (labelTransform == null)
            {
                var labelObject = new GameObject($"OptionLabel_{labelNames[index]}", typeof(RectTransform));
                labelTransform = labelObject.transform;
                labelTransform.SetParent(optionTransform, false);
            }

            var labelRect = (RectTransform)labelTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(28f, 0f);
            labelRect.sizeDelta = new Vector2(105f, 30f);
            labelRect.localScale = Vector3.one / Mathf.Max(0.001f, optionTransform.localScale.x);

            var label = labelTransform.GetComponent<TextMeshProUGUI>() ?? labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = labelNames[index];
            label.font = font;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        EditorUtility.SetDirty(group);
        EditorUtility.SetDirty(radio);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Installed Low / Medium / High graphics radio buttons.");
    }
}
#endif
