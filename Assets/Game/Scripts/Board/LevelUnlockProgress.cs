using UnityEngine;

namespace GameJam.Gameplay
{
    public static class LevelUnlockProgress
    {
        private const string HighestUnlockedLevelKey = "GameJam.HighestUnlockedLevel";

        public static int HighestUnlockedLevel => Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0));

        public static bool IsUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex <= HighestUnlockedLevel;
        }

        public static void MarkReached(int levelIndex)
        {
            if (levelIndex <= HighestUnlockedLevel)
            {
                return;
            }

            PlayerPrefs.SetInt(HighestUnlockedLevelKey, levelIndex);
            PlayerPrefs.Save();
        }
    }
}