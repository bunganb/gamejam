using UnityEngine;
using UnityEngine.SceneManagement;
using GameJam.Gameplay;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private GameLevelDefinition myLevelData;
    [SerializeField, Min(0)] private int levelIndex;
    [SerializeField] private string gameplaySceneName = "GameplayPrototype";
    [SerializeField] private string loadingSceneName = "Loading";

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
    }

    private void Awake()
    {
        RefreshLockState();
    }

    private void OnEnable()
    {
        RefreshLockState();
    }

    private void RefreshLockState()
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.interactable = GameJam.Gameplay.LevelUnlockProgress.IsUnlocked(levelIndex);
        }
    }

    public void LoadThisLevel()
    {
        if (!GameJam.Gameplay.LevelUnlockProgress.IsUnlocked(levelIndex))
        {
            Debug.Log("Level ini belum terbuka.", this);
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
