using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LevelTransitionFader : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private Transform focusTarget;
        [SerializeField] private Material radialMaterial;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float fadeInDuration = 0.65f;
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private int sortingOrder = 10000;
        [SerializeField, Range(0f, 0.15f)] private float bounceAmount = 0.045f;
        [SerializeField, Range(0f, 0.5f)] private float closedRadius = 0.12f;
        [SerializeField] private TMP_FontAsset finaleFont;
        [SerializeField, Min(0.01f)] private float finaleBlinkSpeed = 2.5f;
        [SerializeField, Range(0f, 1f)] private float finaleBlinkMinAlpha = 0.2f;

        private CanvasGroup canvasGroup;
        private Image transitionImage;
        private Material runtimeMaterial;
        private Renderer focusRenderer;
        private Coroutine fadeRoutine;
        private bool receivedFirstLevel;
        private bool finaleShown;
        private TextMeshProUGUI finaleText;

        public void ConfigureRadialMaterial(Material material)
        {
            radialMaterial = material;
        }

        public void ConfigureFinaleFont(TMP_FontAsset font)
        {
            finaleFont = font;
        }

        private void Awake()
        {
            ResolveReferences();
            CreateOverlay();
            SetRadius(0f);
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (levelLoader != null)
            {
                levelLoader.LevelChanged -= HandleLevelChanged;
                levelLoader.LevelChanged += HandleLevelChanged;
            }

            if (gameplayEvents != null)
            {
                gameplayEvents.WinPresentationReady -= HandleWinPresentationReady;
                gameplayEvents.WinPresentationReady += HandleWinPresentationReady;
            }
        }

    private void OnDisable()
        {
            if (levelLoader != null) levelLoader.LevelChanged -= HandleLevelChanged;
            if (gameplayEvents != null) gameplayEvents.WinPresentationReady -= HandleWinPresentationReady;
        }

        private void Update()
        {
            if (!finaleShown || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            SceneManager.LoadScene("MainMenu");
        }

        private void ResolveReferences()
        {
            if (levelLoader == null) levelLoader = GetComponent<LevelLoader>();
            if (gameplayEvents == null) gameplayEvents = FindAnyObjectByType<PuzzleGameplayEvents>();
            if (focusTarget == null)
            {
                focusTarget = ResolvePlayerAnchor();
            }
        }

        private void CreateOverlay()
        {
            var overlayObject = new GameObject("LevelTransitionFade");
            overlayObject.transform.SetParent(transform, false);

            var canvas = overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            overlayObject.AddComponent<GraphicRaycaster>();
            canvasGroup = overlayObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(overlayObject.transform, false);
            var image = imageObject.AddComponent<Image>();
            image.color = fadeColor;
            image.raycastTarget = false;
            transitionImage = image;

            if (radialMaterial != null)
            {
                image.material = radialMaterial;
            }
            else
            {
                var shader = Shader.Find("Game Jam/Radial Level Transition");
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader);
                    image.material = runtimeMaterial;
                }
            }

            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void HandleWinPresentationReady()
        {
            if (levelLoader == null)
            {
                return;
            }

            var isFinalLevel = levelLoader.CurrentGameLevel != null &&
                               levelLoader.CurrentGameLevel.NextLevel == null &&
                               levelLoader.CurrentLevelIndex + 1 >= levelLoader.LevelCount;
            if (isFinalLevel)
            {
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeToFinale());
                return;
            }

            StartFade(1f, fadeOutDuration);
        }

        private IEnumerator FadeToFinale()
        {
            yield return FadeTo(1f, fadeOutDuration);
            CreateFinaleText();
            fadeRoutine = null;
        }

        private void CreateFinaleText()
        {
            if (finaleShown)
            {
                return;
            }

            var finaleCanvasObject = new GameObject("FinaleTextCanvas");
            var canvas = finaleCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder + 1;

            var canvasGroup = finaleCanvasObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var textObject = new GameObject("ToBeContinuedText");
            textObject.transform.SetParent(finaleCanvasObject.transform, false);
            finaleText = textObject.AddComponent<TextMeshProUGUI>();
            finaleText.text = "TO BE CONTINUED\\n<size=70%>PRESS ESC TO RETURN</size>";
            finaleText.font = finaleFont;
            finaleText.fontSize = 42f;
            finaleText.alignment = TextAlignmentOptions.Center;
            finaleText.color = Color.white;
            finaleText.raycastTarget = false;

            var rect = finaleText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 90f);
            rect.sizeDelta = new Vector2(700f, 140f);

            finaleShown = true;
            StartCoroutine(BlinkFinaleText());
        }

        private IEnumerator BlinkFinaleText()
        {
            while (finaleText != null)
            {
                var color = finaleText.color;
                var value = Mathf.Lerp(finaleBlinkMinAlpha, 1f,
                    Mathf.PingPong(Time.unscaledTime * finaleBlinkSpeed, 1f));
                color.a = value;
                finaleText.color = color;
                yield return null;
            }
        }

        private void LateUpdate()
        {
            if (transitionImage == null || transitionImage.material == null)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            var worldPosition = focusTarget != null
                ? focusTarget.position
                : focusRenderer != null ? focusRenderer.bounds.center : transform.position;
            var viewport = mainCamera.WorldToViewportPoint(worldPosition);
            transitionImage.material.SetVector("_Center", new Vector4(viewport.x, viewport.y, 0f, 0f));
        }

        private void HandleLevelChanged(int levelIndex, LevelDefinition level)
        {
            receivedFirstLevel = true;
            StartFade(0f, fadeInDuration);
        }

        private void StartFade(float targetAlpha, float duration)
        {
            if (!receivedFirstLevel && targetAlpha <= 0f) return;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(targetAlpha, duration));
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (canvasGroup == null) yield break;

            var elapsed = 0f;
            var startRadius = GetRadius();
            var targetRadius = targetAlpha > 0.5f ? closedRadius : 2f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                progress = progress * progress * (3f - 2f * progress);
                canvasGroup.alpha = 1f;
                var radius = Mathf.Lerp(startRadius, targetRadius, progress);
                var bounce = Mathf.Sin(progress * Mathf.PI * 2f) * bounceAmount * (1f - progress);
                SetRadius(Mathf.Clamp(radius + bounce, 0f, 2f));
                yield return null;
            }

            canvasGroup.alpha = 1f;
            SetRadius(targetRadius);
            fadeRoutine = null;
        }

        private void SetRadius(float radius)
        {
            if (transitionImage != null && transitionImage.material != null)
            {
                transitionImage.material.SetFloat("_Radius", radius);
                transitionImage.material.SetColor("_Color", fadeColor);
                transitionImage.material.SetFloat("_Softness", 0.08f);
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - Mathf.Clamp01(radius);
            }
        }

        private float GetRadius()
        {
            return transitionImage != null && transitionImage.material != null
                ? transitionImage.material.GetFloat("_Radius")
                : 0f;
        }

        private Transform ResolvePlayerAnchor()
        {
            var playerRoot = levelLoader != null ? levelLoader.PlayerTransform : null;
            if (playerRoot == null)
            {
                return null;
            }

            Transform bestBone = null;
            var bestScore = int.MinValue;
            foreach (var candidate in playerRoot.GetComponentsInChildren<Transform>(true))
            {
                var name = candidate.name.ToLowerInvariant();
                var score = name switch
                {
                    "head" => 100,
                    "head_end" => 95,
                    "upperchest" => 90,
                    "chest" => 85,
                    "spine2" => 80,
                    "spine1" => 75,
                    "spine" => 70,
                    _ => name.Contains("head") ? 60 : name.Contains("chest") ? 50 : name.Contains("spine") ? 40 : 0
                };

                if (score > bestScore)
                {
                    bestScore = score;
                    bestBone = candidate;
                }
            }

            if (bestBone != null && bestScore > 0)
            {
                return bestBone;
            }

            focusRenderer = playerRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
            return null;
        }
    }
}
