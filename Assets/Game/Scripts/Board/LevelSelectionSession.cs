namespace GameJam.Gameplay
{
    public static class LevelSelectionSession
    {
        private static GameLevelDefinition selectedLevel;

        public static bool HasSelection => selectedLevel != null;

        public static void Select(GameLevelDefinition level)
        {
            selectedLevel = level;
        }

        public static bool TryConsume(out GameLevelDefinition level)
        {
            level = selectedLevel;
            selectedLevel = null;
            return level != null;
        }

        public static void Clear()
        {
            selectedLevel = null;
        }
    }
}
