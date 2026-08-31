using System.Collections;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animation))]
    public sealed class McLegacyAnimationPresenter : MonoBehaviour
    {
        [SerializeField] private Animation legacyAnimation;
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private PuzzleGameplayController gameplayController;
        [SerializeField] private string idleClip = "IDLE";
        [SerializeField] private string jumpClip = "LOMPAT";
        [SerializeField] private string winClip = "dance";
        [SerializeField] private bool synchronizeMovementToJumpClip = true;
        [SerializeField, Min(0f)] private float animationFadeDuration = 0.06f;
        [SerializeField, Min(0f)] private float turnDuration = 0.12f;
        [SerializeField] private float modelForwardYawOffset;

        private Coroutine turnRoutine;
        private bool winLocked;

        public void ConfigureReferences(Animation animationComponent, PuzzleGameplayEvents eventHub)
        {
            Unsubscribe();
            legacyAnimation = animationComponent;
            gameplayEvents = eventHub;
            Subscribe();
        }

        private void Awake()
        {
            // Use Unity's overloaded null check. Serialized UnityEngine.Object fields
            // can be "fake null", which the C# ??= operator does not detect.
            if (legacyAnimation == null)
            {
                legacyAnimation = GetComponent<Animation>();
            }

            if (gameplayEvents == null)
            {
                gameplayEvents = FindAnyObjectByType<PuzzleGameplayEvents>();
            }

            if (gameplayController == null)
            {
                gameplayController = FindAnyObjectByType<PuzzleGameplayController>();
            }
            if (legacyAnimation != null)
            {
                // glTFast assigns the first imported clip (dance) as the default clip.
                // Disable Legacy autoplay before Start so MC cannot flash or remain in
                // the win animation when gameplay begins.
                legacyAnimation.playAutomatically = false;
                legacyAnimation.Stop();
            }

            ConfigureClipStates();
            SynchronizeMovementTiming();
            PlayImmediate(idleClip);
        }

        private void OnEnable() => Subscribe();

        private void Start() => PlayIdle();

        private void OnDisable()
        {
            Unsubscribe();
            if (turnRoutine != null)
            {
                StopCoroutine(turnRoutine);
                turnRoutine = null;
            }
        }

        private void Subscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            gameplayEvents.PlayerMoveStarted -= HandlePlayerMoveStarted;
            gameplayEvents.TileActivated -= HandleTileActivated;
            gameplayEvents.ChainCompleted -= HandleChainCompleted;
            gameplayEvents.ChainReset -= HandleChainReset;
            gameplayEvents.PlayerMoveStarted += HandlePlayerMoveStarted;
            gameplayEvents.TileActivated += HandleTileActivated;
            gameplayEvents.ChainCompleted += HandleChainCompleted;
            gameplayEvents.ChainReset += HandleChainReset;
        }

        private void Unsubscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            gameplayEvents.PlayerMoveStarted -= HandlePlayerMoveStarted;
            gameplayEvents.TileActivated -= HandleTileActivated;
            gameplayEvents.ChainCompleted -= HandleChainCompleted;
            gameplayEvents.ChainReset -= HandleChainReset;
        }

        private void ConfigureClipStates()
        {
            ConfigureState(idleClip, WrapMode.Loop, 1f);
            ConfigureState(jumpClip, WrapMode.Once, 1f);
            ConfigureState(winClip, WrapMode.Loop, 1f);
        }

        private void ConfigureState(string clipName, WrapMode wrapMode, float speed)
        {
            var state = ResolveState(clipName);
            if (state == null)
            {
                return;
            }

            state.wrapMode = wrapMode;
            state.speed = speed;
        }

        private void SynchronizeMovementTiming()
        {
            var state = ResolveState(jumpClip);
            if (synchronizeMovementToJumpClip && gameplayController != null && state != null)
            {
                gameplayController.SetMovementDuration(state.length);
            }
        }

        private void HandlePlayerMoveStarted(Vector2Int boardDirection)
        {
            if (winLocked)
            {
                return;
            }

            FaceDirection(boardDirection);
            ConfigureState(jumpClip, WrapMode.Once, 1f);
            CrossFade(jumpClip);
        }

        private void HandleTileActivated(Vector2Int coordinate, BeatColor color)
        {
            if (!winLocked)
            {
                PlayIdle();
            }
        }

        private void HandleChainCompleted(GameplayProgressSnapshot snapshot)
        {
            winLocked = true;
            CrossFade(winClip);
        }

        private void HandleChainReset()
        {
            winLocked = false;
            PlayIdle();
        }

        private void PlayIdle()
        {
            if (winLocked)
            {
                return;
            }

            CrossFade(idleClip);
        }

        private void CrossFade(string clipName)
        {
            if (ResolveState(clipName) == null)
            {
                return;
            }

            legacyAnimation.CrossFade(clipName, animationFadeDuration, PlayMode.StopAll);
        }

        private void PlayImmediate(string clipName)
        {
            if (ResolveState(clipName) == null)
            {
                return;
            }

            legacyAnimation.Play(clipName, PlayMode.StopAll);
        }

        private AnimationState ResolveState(string clipName)
        {
            return legacyAnimation != null && !string.IsNullOrWhiteSpace(clipName)
                ? legacyAnimation[clipName]
                : null;
        }

        private void FaceDirection(Vector2Int boardDirection)
        {
            var worldDirection = BoardDirectionToWorld(boardDirection);
            if (worldDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up)
                * Quaternion.Euler(0f, modelForwardYawOffset, 0f);
            if (turnRoutine != null)
            {
                StopCoroutine(turnRoutine);
            }

            turnRoutine = StartCoroutine(TurnTo(targetRotation));
        }

        private IEnumerator TurnTo(Quaternion targetRotation)
        {
            var startRotation = transform.localRotation;
            if (turnDuration <= 0f)
            {
                transform.localRotation = targetRotation;
                turnRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < turnDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / turnDuration));
                transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, normalized);
                yield return null;
            }

            transform.localRotation = targetRotation;
            turnRoutine = null;
        }

        public static Vector3 BoardDirectionToWorld(Vector2Int boardDirection)
        {
            return new Vector3(boardDirection.x, 0f, -boardDirection.y);
        }
    }
}
