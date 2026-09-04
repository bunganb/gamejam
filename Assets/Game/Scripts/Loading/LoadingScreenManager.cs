using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject readyText;

    private AsyncOperation operation;
    private bool isLoadingDone = false; 

    void Start()
    {
        if (readyText != null) readyText.SetActive(false);
        if (slider != null) slider.gameObject.SetActive(true);

        // Baris ini mencari class LoaderUtils yang ada di bagian paling bawah script ini
        StartCoroutine(LoadAsynchronously(LoaderUtils.TargetSceneName));
    }

    void Update()
    {
        if (isLoadingDone)
        {
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool pressedKey = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

            if (clicked || pressedKey)
            {
                ActivateTargetScene();
            }
        }
    }

    private void ActivateTargetScene()
    {
        if (operation != null)
        {
            operation.allowSceneActivation = true;
        }
    }

    IEnumerator LoadAsynchronously(string sceneName)
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

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (slider != null)
            {
                slider.value = progress;
            }

            if (operation.progress >= 0.9f)
            {
                if (slider != null) slider.gameObject.SetActive(false);
                if (readyText != null) readyText.SetActive(true);

                isLoadingDone = true; 
            }

            yield return null; 
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