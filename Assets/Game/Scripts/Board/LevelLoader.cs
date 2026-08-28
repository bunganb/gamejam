using System;
using System.Collections;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LevelLoader : MonoBehaviour
    {
        [SerializeField] private LevelDefinition[] levels = Array.Empty<LevelDefinition>();
        [SerializeField] private PuzzleBoard puzzleBoard;
        [SerializeField] private Transform player;
        [SerializeField] private PuzzleGameplayController gameplayController;
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private bool loadFirstLevelOnStart = true;

        private Coroutine completionRoutine;

        public event Action<int, LevelDefinition> LevelChanged;
        public event Action GameCompleted;

        public int CurrentLevelIndex { get; private set; } = -1;
        public LevelDefinition CurrentLevel =>
            CurrentLevelIndex >= 0 && CurrentLevelIndex < levels.Length ? levels[CurrentLevelIndex] : null;
        public bool IsEnding { get; private set; }
        public int LevelCount => levels?.Length ?? 0;

        public void ConfigureReferences(
            LevelDefinition[] levelDefinitions,
            PuzzleBoard board,
            Transform playerTransform,
            PuzzleGameplayController controller,
            PuzzleGameplayEvents eventHub,
            bool autoLoadFirstLevel = true)
        {
            Unsubscribe();
            levels = levelDefinitions != null ? (LevelDefinition[])levelDefinitions.Clone() : Array.Empty<LevelDefinition>();
            puzzleBoard = board;
            player = playerTransform;
            gameplayController = controller;
            gameplayEvents = eventHub;
            loadFirstLevelOnStart = autoLoadFirstLevel;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            if (loadFirstLevelOnStart && CurrentLevelIndex < 0)
            {
                LoadLevel(0);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            CancelCompletionRoutine();
        }

        public bool LoadLevel(int levelIndex)
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("LevelLoader is missing required references.", this);
                return false;
            }

            if (levels == null || levelIndex < 0 || levelIndex >= levels.Length)
            {
                Debug.LogError($"Level index {levelIndex} is outside the configured level catalog.", this);
                return false;
            }

            var level = levels[levelIndex];
            if (!LevelSolutionValidator.TryValidate(level, out var error))
            {
                Debug.LogError(error, level);
                return false;
            }

            CancelCompletionRoutine();
            IsEnding = false;
            puzzleBoard.BuildBoard(level);
            gameplayController.ConfigureReferences(level, puzzleBoard, player, gameplayEvents);
            gameplayController.Initialize();
            CurrentLevelIndex = levelIndex;
            gameplayEvents.PublishChainReset();
            LevelChanged?.Invoke(CurrentLevelIndex, level);
            return true;
        }

        public bool LoadNextLevel()
        {
            if (IsEnding)
            {
                return false;
            }

            if (CurrentLevelIndex < 0)
            {
                return LoadLevel(0);
            }

            var nextIndex = CurrentLevelIndex + 1;
            if (nextIndex < LevelCount)
            {
                return LoadLevel(nextIndex);
            }

            EnterEndingState();
            return false;
        }

        public void RestartCurrentLevel()
        {
            if (CurrentLevel == null || gameplayController == null)
            {
                return;
            }

            CancelCompletionRoutine();
            IsEnding = false;
            gameplayController.ResetImmediately();
        }

        private void Subscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            gameplayEvents.WinPresentationReady -= HandleWinPresentationReady;
            gameplayEvents.WinPresentationReady += HandleWinPresentationReady;
        }

        private void Unsubscribe()
        {
            if (gameplayEvents != null)
            {
                gameplayEvents.WinPresentationReady -= HandleWinPresentationReady;
            }
        }

        private void HandleWinPresentationReady()
        {
            if (CurrentLevel == null || completionRoutine != null || IsEnding)
            {
                return;
            }

            completionRoutine = StartCoroutine(ContinueAfterCompletionHold());
        }

        private IEnumerator ContinueAfterCompletionHold()
        {
            var holdDuration = CurrentLevel.CompletionHoldDuration;
            if (holdDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(holdDuration);
            }

            completionRoutine = null;
            LoadNextLevel();
        }

        private void EnterEndingState()
        {
            if (IsEnding)
            {
                return;
            }

            CancelCompletionRoutine();
            IsEnding = true;
            GameCompleted?.Invoke();
        }

        private void CancelCompletionRoutine()
        {
            if (completionRoutine == null)
            {
                return;
            }

            StopCoroutine(completionRoutine);
            completionRoutine = null;
        }

        private bool HasRequiredReferences()
        {
            return puzzleBoard != null && player != null && gameplayController != null && gameplayEvents != null;
        }
    }
}
