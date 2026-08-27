using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayPrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelDefinition prototypeLevel;
        [SerializeField] private PuzzleBoard puzzleBoard;
        [SerializeField] private Transform prototypePlayer;
        [SerializeField] private PuzzleGameplayController gameplayController;
        [SerializeField] private bool enableVSync = true;
        [SerializeField, Min(30)] private int targetFrameRate = 60;

        private void Awake()
        {
            ConfigureFramePacing();
            BuildPrototype();
        }

        private void ConfigureFramePacing()
        {
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;
            Application.targetFrameRate = targetFrameRate;
        }

        public void ConfigureReferences(LevelDefinition level, PuzzleBoard board, Transform player)
        {
            ConfigureReferences(level, board, player, null);
        }

        public void ConfigureReferences(
            LevelDefinition level,
            PuzzleBoard board,
            Transform player,
            PuzzleGameplayController controller)
        {
            prototypeLevel = level;
            puzzleBoard = board;
            prototypePlayer = player;
            gameplayController = controller;
        }

        [ContextMenu("Build Prototype")]
        public void BuildPrototype()
        {
            if (prototypeLevel == null || puzzleBoard == null)
            {
                Debug.LogError("GameplayPrototypeBootstrap is missing its level or board reference.", this);
                return;
            }

            puzzleBoard.BuildBoard(prototypeLevel);
            PlacePlayerAtStart();
            if (gameplayController != null)
            {
                gameplayController.Initialize();
            }
        }

        [ContextMenu("Reset Prototype")]
        public void ResetPrototype()
        {
            if (puzzleBoard == null || prototypeLevel == null)
            {
                return;
            }

            if (gameplayController != null && gameplayController.ProgressTracker != null)
            {
                gameplayController.ResetImmediately();
            }
            else
            {
                puzzleBoard.ResetBoard();
                PlacePlayerAtStart();
            }
        }

        private void PlacePlayerAtStart()
        {
            if (prototypePlayer == null)
            {
                return;
            }

            prototypePlayer.position = puzzleBoard.GetPlayerStandPosition(prototypeLevel.PlayerStart);
        }
    }
}
