using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Inspector-driven level tutorial. The first two lines block gameplay input,
    /// while the third line is shown only after the player breaks the chain.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayTutorialController : MonoBehaviour
    {
        [Serializable]
        public sealed class TutorialLine
        {
            public string id;
            [TextArea(2, 5)] public string message;
            public Sprite carlSprite;
            public Transform worldHighlightTarget;
            public RectTransform uiHighlightTarget;
        }

        [Header("Activation")]
        [SerializeField] private bool onlyLevelOne = true;
        [SerializeField] private string tutorialLevelId = "Level_01";
        [SerializeField] private PuzzleGameplayController gameplayController;
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;

        [Header("Bubble UI")]
        [SerializeField] private GameObject bubble;
        [SerializeField] private RectTransform bubbleRoot;
        [SerializeField] private Animator bubbleAnimator;
        [SerializeField] private string showTrigger = "Show";
        [SerializeField] private string hideTrigger = "Hide";
        [SerializeField, Min(0.05f)] private float hideAnimationDuration = 0.24f;
        [SerializeField] private int bubbleSortingOrder = 10000;
        [SerializeField] private Image carlImage;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField, Min(0f)] private float typingInterval = 0.025f;

        [Header("Tutorial Lines")]
        [SerializeField] private TutorialLine[] lines = Array.Empty<TutorialLine>();

        [Header("Gameplay UI")]
        [SerializeField] private GameObject movementUi;
        [SerializeField] private GameObject restartUi;

        [Header("Highlight")]
        [SerializeField] private Renderer ledGoalTarget;
        [SerializeField] private RectTransform rulesTarget;
        [SerializeField] private Material radialHighlightMaterial;
        [SerializeField] private Color highlightColor = new(0f, 0f, 0f, 0.62f);
        [SerializeField, Range(0.02f, 0.5f)] private float highlightRadius = 0.16f;
        [SerializeField, Min(0f)] private float highlightSoftness = 0.08f;
        [SerializeField] private int highlightSortingOrder = 9000;

        private Canvas highlightCanvas;
        private Image highlightImage;
        private Material runtimeHighlightMaterial;
        private Coroutine typingRoutine;
        private Coroutine autoHideRoutine;
        private Coroutine finishRoutine;
        private int activeLineIndex = -1;
        private bool waitingForClick;
        private bool introFinished;
        private bool wrongLineShown;

        private void Awake()
        {
            if (gameplayController == null)
                gameplayController = FindFirstObjectByType<PuzzleGameplayController>();
            if (gameplayEvents == null)
                gameplayEvents = FindFirstObjectByType<PuzzleGameplayEvents>();
            if (bubble == null)
                bubble = gameObject;
            if (bubbleRoot == null)
                bubbleRoot = bubble.GetComponent<RectTransform>();
            if (bubbleAnimator == null)
                bubbleAnimator = bubble.GetComponent<Animator>();

            EnsureBubbleRendersAboveHighlight();

            CreateHighlightOverlay();
            if (bubbleRoot != null)
                bubbleRoot.localScale = Vector3.zero;
        }

        private void Start()
        {
            if (!IsTutorialLevel())
                return;

            if (lines == null || lines.Length < 3)
            {
                Debug.LogWarning("GameplayTutorialController needs at least three TutorialLine entries.", this);
                return;
            }

            SetControlsVisible(false);
            if (gameplayController != null)
                gameplayController.SetInputBlocked(true);
            ShowLine(0);
        }

        private void OnEnable()
        {
            if (gameplayEvents == null)
                gameplayEvents = FindFirstObjectByType<PuzzleGameplayEvents>();
            if (gameplayEvents != null)
            {
                gameplayEvents.ChainFailed -= HandleChainFailed;
                gameplayEvents.ChainFailed += HandleChainFailed;
                gameplayEvents.PlayerMoveStarted -= HandlePlayerMoveStarted;
                gameplayEvents.PlayerMoveStarted += HandlePlayerMoveStarted;
            }
        }

        private void OnDisable()
        {
            if (gameplayEvents != null)
            {
                gameplayEvents.ChainFailed -= HandleChainFailed;
                gameplayEvents.PlayerMoveStarted -= HandlePlayerMoveStarted;
            }
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);
            if (autoHideRoutine != null)
                StopCoroutine(autoHideRoutine);
            if (finishRoutine != null)
                StopCoroutine(finishRoutine);
            if (runtimeHighlightMaterial != null)
                Destroy(runtimeHighlightMaterial);
            if (highlightCanvas != null)
                Destroy(highlightCanvas.gameObject);
        }

        private void Update()
        {
            if (!waitingForClick || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            waitingForClick = false;
            if (activeLineIndex < 1 && !wrongLineShown)
            {
                ShowLine(activeLineIndex + 1);
                return;
            }

            BeginFinishTutorialMessage();
        }

        private void HandleChainFailed(GameplayProgressSnapshot snapshot)
        {
            if (!IsTutorialLevel() || wrongLineShown)
                return;

            wrongLineShown = true;
            if (autoHideRoutine != null)
                StopCoroutine(autoHideRoutine);
            if (gameplayController != null)
                gameplayController.SetInputBlocked(false);
            ShowLine(2);
        }

        private void HandlePlayerMoveStarted(Vector2Int direction)
        {
            if (!IsTutorialLevel() || !introFinished)
                return;

            // Once the player understands the controls, remove the helper panels
            // as soon as the first real movement begins.
            SetControlsVisible(false);
        }

        private void ShowLine(int index)
        {
            if (lines == null || index < 0 || index >= lines.Length)
                return;

            activeLineIndex = index;
            waitingForClick = false;
            if (bubbleAnimator != null && !string.IsNullOrWhiteSpace(showTrigger))
            {
                bubbleAnimator.Rebind();
                bubbleAnimator.Update(0f);
                bubbleAnimator.ResetTrigger(showTrigger);
                bubbleAnimator.SetTrigger(showTrigger);
            }
            else if (bubbleRoot != null)
            {
                bubbleRoot.localScale = Vector3.one;
            }

            var line = lines[index];
            if (carlImage != null)
                carlImage.sprite = line.carlSprite;
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);
            typingRoutine = StartCoroutine(TypeLine(line.message ?? string.Empty));

            if (index == 0)
                ShowHighlight(ledGoalTarget != null ? ledGoalTarget.transform : null, null);
            else if (index == 1)
                ShowHighlight(null, rulesTarget);
            else
                HideHighlight();
        }

        private IEnumerator TypeLine(string message)
        {
            if (dialogueText == null)
                yield break;

            dialogueText.text = string.Empty;
            foreach (var character in message)
            {
                dialogueText.text += character;
                if (typingInterval > 0f)
                    yield return new WaitForSecondsRealtime(typingInterval);
            }

            typingRoutine = null;
            waitingForClick = true;

            // The normal instruction and the mistake message close automatically
            // after the player has had enough time to read them.
            if ((activeLineIndex == 1 && !wrongLineShown) ||
                (activeLineIndex == 2 && wrongLineShown))
                autoHideRoutine = StartCoroutine(AutoHideInstruction());
        }

        private IEnumerator AutoHideInstruction()
        {
            yield return new WaitForSecondsRealtime(3f);
            var canAutoHide =
                (activeLineIndex == 1 && !wrongLineShown && !introFinished) ||
                (activeLineIndex == 2 && wrongLineShown);
            if (canAutoHide)
                BeginFinishTutorialMessage();
            autoHideRoutine = null;
        }

        private void BeginFinishTutorialMessage()
        {
            var closingWrongLine = wrongLineShown && activeLineIndex == 2;
            if (finishRoutine != null || (introFinished && !closingWrongLine))
                return;

            waitingForClick = false;
            if (autoHideRoutine != null)
            {
                StopCoroutine(autoHideRoutine);
                autoHideRoutine = null;
            }
            finishRoutine = StartCoroutine(FinishTutorialMessage());
        }

        private IEnumerator FinishTutorialMessage()
        {
            if (bubbleAnimator != null && !string.IsNullOrWhiteSpace(hideTrigger))
            {
                bubbleAnimator.ResetTrigger(hideTrigger);
                bubbleAnimator.SetTrigger(hideTrigger);
                yield return new WaitForSecondsRealtime(hideAnimationDuration);
            }
            else if (bubbleRoot != null)
            {
                bubbleRoot.localScale = Vector3.zero;
            }

            HideHighlight();

            if (wrongLineShown)
            {
                introFinished = true;
                SetControlsVisible(true);
                if (gameplayController != null)
                    gameplayController.SetInputBlocked(false);
                finishRoutine = null;
                yield break;
            }

            introFinished = true;
            SetControlsVisible(true);
            if (gameplayController != null)
                gameplayController.SetInputBlocked(false);
            finishRoutine = null;
        }

        private void SetControlsVisible(bool visible)
        {
            if (movementUi != null)
                movementUi.SetActive(visible);
            if (restartUi != null)
                restartUi.SetActive(visible);
        }

        private bool IsTutorialLevel()
        {
            if (gameplayController == null || gameplayController.Level == null)
                return false;
            return !onlyLevelOne || string.Equals(
                gameplayController.Level.LevelId,
                tutorialLevelId,
                StringComparison.OrdinalIgnoreCase);
        }

        private void CreateHighlightOverlay()
        {
            var overlayObject = new GameObject("Tutorial Highlight", typeof(RectTransform), typeof(Canvas));
            overlayObject.hideFlags = HideFlags.HideAndDontSave;
            var canvas = overlayObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = highlightSortingOrder;
            highlightCanvas = canvas;

            var imageObject = new GameObject("Tutorial Vignette", typeof(RectTransform), typeof(Image));
            imageObject.hideFlags = HideFlags.HideAndDontSave;
            imageObject.transform.SetParent(overlayObject.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            highlightImage = imageObject.GetComponent<Image>();
            highlightImage.raycastTarget = false;

            if (radialHighlightMaterial != null)
            {
                runtimeHighlightMaterial = new Material(radialHighlightMaterial);
                highlightImage.material = runtimeHighlightMaterial;
            }
            else
            {
                var shader = Shader.Find("Game Jam/Radial Level Transition");
                if (shader != null)
                {
                    runtimeHighlightMaterial = new Material(shader);
                    highlightImage.material = runtimeHighlightMaterial;
                }
            }

            highlightImage.enabled = false;
        }

        private void ShowHighlight(Transform worldTarget, RectTransform uiTarget)
        {
            if (highlightImage == null || runtimeHighlightMaterial == null)
                return;

            var viewport = new Vector2(0.5f, 0.5f);
            if (worldTarget != null && Camera.main != null)
            {
                viewport = Camera.main.WorldToViewportPoint(worldTarget.position);
            }
            else if (uiTarget != null)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(null, uiTarget.position);
                viewport = new Vector2(screen.x / Screen.width, screen.y / Screen.height);
            }

            runtimeHighlightMaterial.SetVector("_Center", new Vector4(viewport.x, viewport.y, 0f, 0f));
            runtimeHighlightMaterial.SetFloat("_Radius", highlightRadius);
            runtimeHighlightMaterial.SetFloat("_Softness", highlightSoftness);
            runtimeHighlightMaterial.SetColor("_Color", highlightColor);
            highlightImage.enabled = true;
        }

        private void HideHighlight()
        {
            if (highlightImage != null)
                highlightImage.enabled = false;
        }

        private void EnsureBubbleRendersAboveHighlight()
        {
            if (bubble == null)
                return;

            var bubbleCanvas = bubble.GetComponent<Canvas>();
            if (bubbleCanvas == null)
                bubbleCanvas = bubble.AddComponent<Canvas>();

            bubbleCanvas.overrideSorting = true;
            bubbleCanvas.sortingOrder = bubbleSortingOrder;
        }
    }
}
