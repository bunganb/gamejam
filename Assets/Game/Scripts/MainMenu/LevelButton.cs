using UnityEngine;
using UnityEngine.SceneManagement;
using GameJam.Gameplay;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private GameLevelDefinition myLevelData;
    [SerializeField] private string gameplaySceneName = "GameplayPrototype";

    public GameLevelDefinition Level => myLevelData;

    public void Configure(GameLevelDefinition level, string sceneName = "GameplayPrototype")
    {
        myLevelData = level;
        gameplaySceneName = sceneName;
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

        Debug.Log($"Membuka {gameplaySceneName} dengan {myLevelData.LevelId}");
        LevelSelectionSession.Select(myLevelData);
        SceneManager.LoadScene(gameplaySceneName);
    }
}
