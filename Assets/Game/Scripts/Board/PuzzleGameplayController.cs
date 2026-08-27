using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PuzzleGameplayController : MonoBehaviour
    {
        [SerializeField] private LevelDefinition level;
        [SerializeField] private PuzzleBoard puzzleBoard;
        [SerializeField] private Transform player;
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField, Min(0.01f)] private float movementDuration = 0.16f;
        [SerializeField, Min(0f)] private float settleDuration = 0.05f;
        [SerializeField, Min(0f)] private float settleHeight = 0.035f;
        [SerializeField, Min(0f)] private float failFeedbackDuration = 0.75f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private ObjectiveProgressTracker progressTracker;
        private Vector2Int currentCoordinate;
        private bool initialized;
        private bool completionPublished;
        private bool hasBufferedDirection;
        private Vector2Int bufferedDirection;

        public GameplayState State { get; private set; } = GameplayState.Playing;
        public Vector2Int CurrentCoordinate => currentCoordinate;
        public ObjectiveProgressTracker ProgressTracker => progressTracker;
        public LevelDefinition Level => level;

        public void ConfigureReferences(
            LevelDefinition levelDefinition,
            PuzzleBoard board,
            Transform playerTransform,
            PuzzleGameplayEvents eventHub)
        {
            level = levelDefinition;
            puzzleBoard = board;
            player = playerTransform;
            gameplayEvents = eventHub;
        }

        public void Initialize()
        {
            if (level == null || puzzleBoard == null || player == null || gameplayEvents == null)
            {
                Debug.LogError("PuzzleGameplayController is missing required references.", this);
                enabled = false;
                return;
            }

            if (!level.TryValidate(out var error))
            {
                Debug.LogError(error, level);
                enabled = false;
                return;
            }

            progressTracker = new ObjectiveProgressTracker(level.ObjectiveRows);
            currentCoordinate = level.PlayerStart;
            completionPublished = false;
            hasBufferedDirection = false;
            State = GameplayState.Playing;
            player.position = puzzleBoard.GetPlayerStandPosition(currentCoordinate);
            initialized = true;
            enabled = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (State == GameplayState.Moving)
            {
                if (TryReadDirection(keyboard, out var queuedDirection))
                {
                    bufferedDirection = queuedDirection;
                    hasBufferedDirection = true;
                }

                return;
            }

            if (State != GameplayState.Playing)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ResetImmediately();
                return;
            }

            if (TryReadDirection(keyboard, out var direction))
            {
                TryMove(direction);
            }
        }

        public bool TryMove(Vector2Int direction)
        {
            if (!initialized || State != GameplayState.Playing || !IsOrthogonalUnit(direction))
            {
                return false;
            }

            var targetCoordinate = currentCoordinate + direction;
            if (!puzzleBoard.TryGetTile(targetCoordinate, out var targetTile))
            {
                return false;
            }

            State = GameplayState.Moving;
            StartCoroutine(MoveAndResolve(targetCoordinate, targetTile));
            return true;
        }

        [ContextMenu("Reset Gameplay")]
        public void ResetImmediately()
        {
            if (!initialized)
            {
                return;
            }

            StopAllCoroutines();
            ResetLevelInternal();
        }

        private IEnumerator MoveAndResolve(Vector2Int targetCoordinate, TileSlot targetTile)
        {
            var startPosition = player.position;
            var targetPosition = targetTile.PlayerStandPoint != null
                ? targetTile.PlayerStandPoint.position
                : targetTile.transform.position;
            var elapsed = 0f;

            while (elapsed < movementDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / movementDuration);
                player.position = Vector3.LerpUnclamped(startPosition, targetPosition, movementCurve.Evaluate(normalized));
                yield return null;
            }

            player.position = targetPosition;
            currentCoordinate = targetCoordinate;
            ResolveLanding(targetTile);

            if (State != GameplayState.Moving)
            {
                yield break;
            }

            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, settleDuration));
                var followThrough = Mathf.Sin(normalized * Mathf.PI) * settleHeight;
                player.position = targetPosition + Vector3.up * followThrough;
                yield return null;
            }

            player.position = targetPosition;
            State = GameplayState.Playing;
            ConsumeBufferedMove();
        }

        private void ResolveLanding(TileSlot tile)
        {
            var actualColor = tile.AdvanceColor();
            var resolvedRow = progressTracker.CurrentRowIndex;
            var resolvedNote = progressTracker.CurrentNoteIndex;
            var expectedColor = progressTracker.ExpectedColor;
            gameplayEvents.PublishTileActivated(currentCoordinate, actualColor);

            var result = progressTracker.Resolve(actualColor);
            var snapshot = new GameplayProgressSnapshot(
                resolvedRow,
                resolvedNote,
                progressTracker.MatchedTotal,
                progressTracker.TotalNotes,
                actualColor,
                expectedColor,
                currentCoordinate);

            if (result == ObjectiveMatchResult.Incorrect)
            {
                hasBufferedDirection = false;
                gameplayEvents.PublishChainFailed(snapshot);
                StartCoroutine(FailAndReset());
                return;
            }

            gameplayEvents.PublishChainAdvanced(snapshot);
            if (result == ObjectiveMatchResult.RowCompleted || result == ObjectiveMatchResult.ChainCompleted)
            {
                gameplayEvents.PublishObjectiveRowCompleted(snapshot);
            }

            if (result == ObjectiveMatchResult.ChainCompleted)
            {
                hasBufferedDirection = false;
                State = GameplayState.Completing;
                if (!completionPublished)
                {
                    completionPublished = true;
                    gameplayEvents.PublishChainCompleted(snapshot);
                }

                return;
            }

        }

        private IEnumerator FailAndReset()
        {
            State = GameplayState.FailFeedback;
            if (failFeedbackDuration > 0f)
            {
                yield return new WaitForSeconds(failFeedbackDuration);
            }

            ResetLevelInternal();
        }

        private void ResetLevelInternal()
        {
            puzzleBoard.ResetBoard();
            progressTracker.Reset();
            currentCoordinate = level.PlayerStart;
            player.position = puzzleBoard.GetPlayerStandPosition(currentCoordinate);
            completionPublished = false;
            hasBufferedDirection = false;
            State = GameplayState.Playing;
            gameplayEvents.PublishChainReset();
        }

        private void ConsumeBufferedMove()
        {
            if (!hasBufferedDirection || State != GameplayState.Playing)
            {
                return;
            }

            var direction = bufferedDirection;
            hasBufferedDirection = false;
            TryMove(direction);
        }

        private static bool TryReadDirection(Keyboard keyboard, out Vector2Int direction)
        {
            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                direction = Vector2Int.down;
                return true;
            }

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                direction = Vector2Int.up;
                return true;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                direction = Vector2Int.left;
                return true;
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                direction = Vector2Int.right;
                return true;
            }

            direction = Vector2Int.zero;
            return false;
        }

        private static bool IsOrthogonalUnit(Vector2Int direction)
        {
            return Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1;
        }
    }
}
