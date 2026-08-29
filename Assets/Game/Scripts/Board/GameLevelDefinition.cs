using UnityEngine;

namespace GameJam.Gameplay
{
    [CreateAssetMenu(menuName = "Game Jam/Game Level Definition", fileName = "GameLevel_01")]
    public sealed class GameLevelDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Level 01";
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private LevelDefinition puzzle;
        [SerializeField] private LevelMusicDefinition music;
        [SerializeField] private GameLevelDefinition nextLevel;

        public string DisplayName => displayName;
        public Sprite Thumbnail => thumbnail;
        public LevelDefinition Puzzle => puzzle;
        public LevelMusicDefinition Music => music;
        public GameLevelDefinition NextLevel => nextLevel;
        public string LevelId => puzzle != null ? puzzle.LevelId : name;

#if UNITY_EDITOR
        public void SetData(
            string levelDisplayName,
            LevelDefinition puzzleDefinition,
            LevelMusicDefinition musicDefinition,
            GameLevelDefinition followingLevel = null,
            Sprite levelThumbnail = null)
        {
            displayName = levelDisplayName;
            puzzle = puzzleDefinition;
            music = musicDefinition;
            nextLevel = followingLevel;
            thumbnail = levelThumbnail;
        }
#endif

        public bool TryValidate(out string error)
        {
            if (puzzle == null || music == null)
            {
                error = $"{name} requires puzzle and music definitions.";
                return false;
            }

            if (!puzzle.TryValidate(out error))
            {
                return false;
            }

            if (music.LevelId != puzzle.LevelId)
            {
                error = $"{name} pairs puzzle {puzzle.LevelId} with music {music.LevelId}.";
                return false;
            }

            return music.TryValidate(puzzle.TotalNotes, out error);
        }
    }
}
