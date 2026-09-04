using System.Collections;
using System.Collections.Generic;
using GameJam.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject readyText;
    [SerializeField, Min(0f)] private float audioPreloadTimeout = 8f;
    [SerializeField] private Material radialTransitionMaterial;
    [SerializeField, Min(0.01f)] private float transitionFadeOutDuration = 0.5f;

    private AsyncOperation operation;
    private bool isLoadingDone;
    private bool isActivating;
    private float displayedProgress;
    private Image transitionImage;
    private Material runtimeTransitionMaterial;

    public bool IsReady => isLoadingDone;
    public float Progress => displayedProgress;

    private void Start()
    {
        CreateTransitionOverlay();
        if (readyText != null) readyText.SetActive(false);
        if (slider != null) slider.gameObject.SetActive(true);

        // Baris ini mencari class LoaderUtils yang ada di bagian paling bawah script ini
        StartCoroutine(LoadAsynchronously(LoaderUtils.TargetSceneName));
    }

    private void Update()
    {
        if (isLoadingDone)
        {
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool pressedKey = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

            if ((clicked || pressedKey) && !isActivating)
            {
                StartCoroutine(FadeOutAndActivate());
            }
        }
    }

    public void ConfigureTransitionMaterial(Material material)
    {
        radialTransitionMaterial = material;
    }

    private void ActivateTargetScene()
    {
        if (operation != null)
        {
            operation.allowSceneActivation = true;
        }
    }

    private void CreateTransitionOverlay()
    {
        var overlayObject = new GameObject("LoadingTransitionFade");

        var canvas = overlayObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10000;
        overlayObject.AddComponent<GraphicRaycaster>();

        var imageObject = new GameObject("RadialFadeImage");
        imageObject.transform.SetParent(overlayObject.transform, false);
        transitionImage = imageObject.AddComponent<Image>();
        transitionImage.color = Color.black;
        transitionImage.raycastTarget = false;

        var rect = transitionImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var material = radialTransitionMaterial;
        if (material == null)
        {
            var shader = Shader.Find("Game Jam/Radial Level Transition");
            if (shader != null) material = new Material(shader);
        }

        if (material != null)
        {
            runtimeTransitionMaterial = new Material(material);
            transitionImage.material = runtimeTransitionMaterial;
            runtimeTransitionMaterial.SetVector("_Center", new Vector4(0.5f, 0.5f, 0f, 0f));
            runtimeTransitionMaterial.SetFloat("_Radius", 2f);
            runtimeTransitionMaterial.SetFloat("_Softness", 0.08f);
        }
    }

    private IEnumerator FadeOutAndActivate()
    {
        isActivating = true;
        if (runtimeTransitionMaterial != null)
        {
            var elapsed = 0f;
            while (elapsed < transitionFadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / transitionFadeOutDuration);
                progress = progress * progress * (3f - 2f * progress);
                var bounce = Mathf.Sin(progress * Mathf.PI * 2f) * 0.045f * (1f - progress);
                runtimeTransitionMaterial.SetFloat("_Radius", Mathf.Clamp(2f * (1f - progress) + bounce, 0f, 2f));
                yield return null;
            }

            runtimeTransitionMaterial.SetFloat("_Radius", 0f);
        }

        ActivateTargetScene();
    }

    private IEnumerator LoadAsynchronously(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Target scene loading belum diatur.", this);
            yield break;
        }

        operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"Scene target tidak ditemukan di Build Settings: {sceneName}", this);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            SetProgress(Mathf.Clamp01(operation.progress / 0.9f) * 0.9f);
            yield return null;
        }

        SetProgress(0.9f);

        // Scene activation is intentionally still blocked here. Loading the
        // selected level's clips now moves disk/decompression work away from
        // the first puzzle input and keeps the loading screen responsive.
        yield return PreloadSelectedLevelAudio();

        SetProgress(1f);
        if (slider != null) slider.gameObject.SetActive(false);
        if (readyText != null) readyText.SetActive(true);
        isLoadingDone = true;
    }

    private IEnumerator PreloadSelectedLevelAudio()
    {
        var selectedLevel = LevelSelectionSession.SelectedLevel;
        var music = selectedLevel != null ? selectedLevel.Music : null;
        if (music == null)
        {
            yield break;
        }

        var clips = CollectUniqueClips(music);
        if (clips.Count == 0)
        {
            yield break;
        }

        foreach (var clip in clips)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }
        }

        var elapsed = 0f;
        while (elapsed < audioPreloadTimeout)
        {
            var loaded = 0;
            var finished = 0;
            foreach (var clip in clips)
            {
                if (clip == null)
                {
                    finished++;
                    continue;
                }

                if (clip.loadState == AudioDataLoadState.Loaded)
                {
                    loaded++;
                    finished++;
                }
                else if (clip.loadState == AudioDataLoadState.Failed)
                {
                    // Do not hold the entire game behind one bad optional
                    // clip. The music director can still report the issue.
                    finished++;
                }
            }

            var audioProgress = (float)loaded / clips.Count;
            SetProgress(Mathf.Lerp(0.9f, 1f, audioProgress));
            if (finished >= clips.Count)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning($"Audio preload timed out after {audioPreloadTimeout:0.##} seconds. Continuing to gameplay.", this);
    }

    private static List<AudioClip> CollectUniqueClips(LevelMusicDefinition music)
    {
        var clips = new List<AudioClip>();
        AddUnique(clips, music.BaseHarmony);
        AddUnique(clips, music.SecondaryLayer);
        AddUnique(clips, music.BuildLayer);
        AddUnique(clips, music.TopLoopLayer);
        AddUnique(clips, music.FullSong);

        if (music.NoteSamples != null)
        {
            foreach (var clip in music.NoteSamples)
            {
                AddUnique(clips, clip);
            }
        }

        return clips;
    }

    private static void AddUnique(List<AudioClip> clips, AudioClip clip)
    {
        if (clip != null && !clips.Contains(clip))
        {
            clips.Add(clip);
        }
    }

    private void SetProgress(float value)
    {
        displayedProgress = Mathf.Clamp01(value);
        if (slider != null)
        {
            slider.value = displayedProgress;
        }
    }
}

// --- PASTIKAN BAGIAN INI IKUT TERSALIN ---
// Class bantuan ini wajib ada agar "LoaderUtils.TargetSceneName" di atas tidak error
public static class LoaderUtils 
{
    public static string TargetSceneName { get; private set; } = "GameplayPrototype";

    public static void SetTargetScene(string sceneName)
    {
        TargetSceneName = sceneName;
    }
}
