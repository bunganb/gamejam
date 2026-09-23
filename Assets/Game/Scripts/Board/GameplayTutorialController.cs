using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Inspector-driven level tutorial with smooth highlight animations and initial delay.
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

        [Header("Highlight Overlay")]
        [SerializeField] private Renderer ledGoalTarget;
        [SerializeField] private RectTransform rulesTarget;
        [SerializeField] private Material radialHighlightMaterial;
        [SerializeField] private Color highlightColor = new(0.06f, 0.04f, 0.14f, 0.82f); // Preset Spooky
        [SerializeField, Range(0.02f, 0.5f)] private float highlightRadius = 0.16f;
        [SerializeField, Min(0f)] private float highlightSoftness = 0.08f;
        [SerializeField] private int highlightSortingOrder = 9000;
        
        [Header("Highlight Animation Timings")]
        [SerializeField, Min(0f)] private float initialHighlightDelay = 0.5f; // Delay sebelum animasi pertama dimulai
        [SerializeField, Min(0.1f)] private float highlightTransitionDuration = 0.55f; // Durasi meluncur/animasi

        private Canvas highlightCanvas;
        private Image highlightImage;
        private Material runtimeHighlightMaterial;
        private Coroutine typingRoutine;
        private Coroutine autoHideRoutine;
        private Coroutine finishRoutine;
        private Coroutine highlightRoutine;

        private int activeLineIndex = -1;
        private bool waitingForClick;
        private bool introFinished;
        private bool wrongLineShown;

        // State animasi highlight
        private Vector2 currentHighlightCenter = new(0.5f, 0.5f);
        private float currentHighlightRadius = 1f;
        private Color currentHighlightColor;
        private bool isHighlightActive;

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
            if (highlightRoutine != null)
                StopCoroutine(highlightRoutine);
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

            // Target World
            Transform wTarget = line.worldHighlightTarget != null 
                ? line.worldHighlightTarget 
                : (index == 0 && ledGoalTarget != null ? ledGoalTarget.transform : null);

            // Target UI
            RectTransform uTarget = line.uiHighlightTarget;
            if (uTarget == null && index == 1)
            {
                if (movementUi != null)
                    uTarget = movementUi.GetComponent<RectTransform>();
                else
                    uTarget = rulesTarget;
            }

            ShowHighlight(wTarget, uTarget);
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
            var canvas = overlayObject.GetComponent<Canvas>();

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
                canvas.planeDistance = 1.1f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = highlightSortingOrder;
            highlightCanvas = canvas;

            var imageObject = new GameObject("Tutorial Vignette", typeof(RectTransform), typeof(Image));
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

        // =========================================================================
        // ANIMASI HIGHLIGHT DENGAN INITIAL DELAY
        // =========================================================================

        private void ShowHighlight(Transform worldTarget, RectTransform uiTarget)
        {
            if (highlightImage == null || runtimeHighlightMaterial == null)
                return;

            Vector2 targetViewport = Vector2.zero;
            var mainCam = Camera.main;

            if (worldTarget != null && mainCam != null)
            {
                targetViewport = mainCam.WorldToViewportPoint(worldTarget.position);
            }
            else if (uiTarget != null)
            {
                Canvas parentCanvas = uiTarget.GetComponentInParent<Canvas>();
                Camera uiCam = null;

                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    uiCam = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : mainCam;
                }

                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, uiTarget.position);
                targetViewport = new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);
            }
            else
            {
                HideHighlight();
                return;
            }

            if (highlightRoutine != null)
                StopCoroutine(highlightRoutine);

            highlightRoutine = StartCoroutine(AnimateHighlightRoutine(targetViewport));
        }

        private IEnumerator AnimateHighlightRoutine(Vector2 targetCenter)
        {
            Vector2 startCenter;
            float startRadius;
            Color startColor;

            if (!isHighlightActive)
            {
                // INTRO: Layar awalnya bening transparan di tengah (0.5, 0.5)
                currentHighlightCenter = new Vector2(0.5f, 0.5f);
                currentHighlightRadius = 1.0f;
                currentHighlightColor = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
                ApplyHighlightProperties(currentHighlightCenter, currentHighlightRadius, currentHighlightColor);

                highlightImage.enabled = true;
                isHighlightActive = true;

                // Menunggu delay awal (0.5 detik) agar pemain siap
                if (initialHighlightDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(initialHighlightDelay);
                }

                startCenter = currentHighlightCenter;
                startRadius = currentHighlightRadius;
                startColor = currentHighlightColor;
            }
            else
            {
                // TRANSISI: Meluncur dari posisi sebelumnya ke posisi baru
                startCenter = currentHighlightCenter;
                startRadius = currentHighlightRadius;
                startColor = currentHighlightColor;
            }

            Vector2 endCenter = targetCenter;
            float endRadius = highlightRadius;
            Color endColor = highlightColor;

            float elapsed = 0f;
            while (elapsed < highlightTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / highlightTransitionDuration);
                
                // Kurva pergerakan halus (SmoothStep)
                float smoothT = t * t * (3f - 2f * t);

                currentHighlightCenter = Vector2.Lerp(startCenter, endCenter, smoothT);
                currentHighlightRadius = Mathf.Lerp(startRadius, endRadius, smoothT);
                currentHighlightColor = Color.Lerp(startColor, endColor, smoothT);

                ApplyHighlightProperties(currentHighlightCenter, currentHighlightRadius, currentHighlightColor);
                yield return null;
            }

            currentHighlightCenter = endCenter;
            currentHighlightRadius = endRadius;
            currentHighlightColor = endColor;
            ApplyHighlightProperties(currentHighlightCenter, currentHighlightRadius, currentHighlightColor);

            highlightRoutine = null;
        }

        private void HideHighlight()
        {
            if (!isHighlightActive || highlightImage == null)
                return;

            if (highlightRoutine != null)
                StopCoroutine(highlightRoutine);

            highlightRoutine = StartCoroutine(AnimateHideHighlightRoutine());
        }

        private IEnumerator AnimateHideHighlightRoutine()
        {
            Vector2 startCenter = currentHighlightCenter;
            float startRadius = currentHighlightRadius;
            Color startColor = currentHighlightColor;

            Vector2 endCenter = startCenter;
            float endRadius = 1.0f;
            Color endColor = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);

            float elapsed = 0f;
            while (elapsed < highlightTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / highlightTransitionDuration);
                float smoothT = t * t * (3f - 2f * t);

                currentHighlightCenter = Vector2.Lerp(startCenter, endCenter, smoothT);
                currentHighlightRadius = Mathf.Lerp(startRadius, endRadius, smoothT);
                currentHighlightColor = Color.Lerp(startColor, endColor, smoothT);

                ApplyHighlightProperties(currentHighlightCenter, currentHighlightRadius, currentHighlightColor);
                yield return null;
            }

            highlightImage.enabled = false;
            isHighlightActive = false;
            highlightRoutine = null;
        }

        private void ApplyHighlightProperties(Vector2 center, float radius, Color color)
        {
            if (runtimeHighlightMaterial == null) return;
            runtimeHighlightMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0f, 0f));
            runtimeHighlightMaterial.SetFloat("_Radius", radius);
            runtimeHighlightMaterial.SetFloat("_Softness", highlightSoftness);
            runtimeHighlightMaterial.SetColor("_Color", color);
        }

        private void EnsureBubbleRendersAboveHighlight()
        {
            if (bubble == null)
                return;

            var bubbleCanvas = bubble.GetComponent<Canvas>();
            if (bubbleCanvas == null)
                bubbleCanvas = bubble.AddComponent<Canvas>();

            if (bubble.GetComponent<GraphicRaycaster>() == null)
                bubble.AddComponent<GraphicRaycaster>();

            bubbleCanvas.overrideSorting = true;
            bubbleCanvas.sortingOrder = bubbleSortingOrder;
        }
    }
}