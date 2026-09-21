using UnityEngine;
using UnityEngine.SceneManagement;
using GameJam.Gameplay;

[RequireComponent(typeof(Collider))]
public class LevelButton : MonoBehaviour
{
    [Header("Level Data & Settings")]
    [SerializeField] private GameLevelDefinition myLevelData;
    [SerializeField, Min(0)] private int levelIndex;
    [SerializeField] private string gameplaySceneName = "GameplayPrototype";
    [SerializeField] private string loadingSceneName = "Loading";

    [Header("3D Visual Feedback")]
    [SerializeField] private Renderer objectRenderer;
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    public int LevelIndex => levelIndex;
    
    private Color originalColor;
    private bool isUnlocked;
    private bool isHovered;

    public GameLevelDefinition Level => myLevelData;

    public void Configure(
        GameLevelDefinition level,
        int index,
        string sceneName = "GameplayPrototype",
        string loadingScene = "Loading")
    {
        myLevelData = level;
        levelIndex = index;
        gameplaySceneName = sceneName;
        loadingSceneName = loadingScene;

        RefreshLockState();
    }

    private void Awake()
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }

        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }

        RefreshLockState();
    }

    private void OnEnable()
    {
        RefreshLockState();
    }

    public void RefreshLockState()
    {
        isUnlocked = LevelUnlockProgress.IsUnlocked(levelIndex);

        if (objectRenderer != null)
        {
            if (!isUnlocked)
            {
                objectRenderer.material.color = lockedColor;
            }
            else
            {
                objectRenderer.material.color = originalColor;
            }
        }

        var uiButton = GetComponent<UnityEngine.UI.Button>();
        if (uiButton != null)
        {
            uiButton.interactable = isUnlocked;
        }
    }

    /// <summary>
    /// Dipanggil oleh Raycast3DButtonDetector untuk mencatat status hover (tanpa merubah visual/animasi tombol)
    /// </summary>
    public void SetHovered(bool state)
    {
        if (!isUnlocked) return;
        isHovered = state;
    }

    /// <summary>
    /// Dipanggil saat diklik oleh raycast
    /// </summary>
    public void OnButtonClicked()
    {
        if (isHovered)
        {
            LoadThisLevel();
        }
    }

    public void LoadThisLevel()
    {
        if (!LevelUnlockProgress.IsUnlocked(levelIndex))
        {
            Debug.Log($"Level {levelIndex} ini belum terbuka.", this);
            return;
        }

        if (myLevelData == null)
        {
            Debug.LogWarning("Data level belum di-assign.", this);
            return;
        }

        if (!myLevelData.TryValidate(out var error))
        {
            Debug.LogWarning($"Data level belum valid: {error}", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogWarning("Nama scene gameplay belum diisi.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(loadingSceneName))
        {
            Debug.LogWarning("Nama scene loading belum diisi.", this);
            return;
        }

        Debug.Log($"Membuka {myLevelData.LevelId} melalui {loadingSceneName} menuju {gameplaySceneName}");
        LevelSelectionSession.Select(myLevelData);
        LoaderUtils.SetTargetScene(gameplaySceneName);
        SceneManager.LoadScene(loadingSceneName);
    }
}