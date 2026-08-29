using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LevelSelectButton : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private GameLevelDefinition level;

        public GameLevelDefinition Level => level;

        public void Configure(LevelLoader loader, GameLevelDefinition gameLevel)
        {
            levelLoader = loader;
            level = gameLevel;
        }

        public void LoadAssignedLevel()
        {
            if (levelLoader == null || level == null)
            {
                Debug.LogError("LevelSelectButton requires a LevelLoader and GameLevelDefinition.", this);
                return;
            }

            levelLoader.LoadLevel(level);
        }
    }
}
