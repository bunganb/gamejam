using UnityEngine;
using UnityEngine.SceneManagement;
using GameJam.Gameplay;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private GameLevelDefinition myLevelData;
    [SerializeField] private string gameplaySceneName = "GameplayPrototype";
    [SerializeField] private string loadingSceneName = "Loading";

    public GameLevelDefinition Level => myLevelData;

    public void Configure(
        GameLevelDefinition level,
        string sceneName = "GameplayPrototype",
        string loadingScene = "Loading")
    {
        myLevelData = level;
        gameplaySceneName = sceneName;
        loadingSceneName = loadingScene;
    }

    public void LoadThisLevel()
    {
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
