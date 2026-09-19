using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Draws the same three-note goal window used by the HUD into the LED DJ shader.
    /// The texture is intentionally small and is rebuilt only on progress changes or
    /// at a low pulse tick rate, keeping the display cheap on low-end hardware.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class LedGoalChainDisplay : MonoBehaviour
    {
        private static readonly int MainTextId = Shader.PropertyToID("_MainText");
        private static readonly int LedColorId = Shader.PropertyToID("_LED_color");
        private static readonly int LedSizeId = Shader.PropertyToID("_LED_size");

        [SerializeField] private PuzzleGameplayController gameplayController;
        [SerializeField, Min(32)] private int textureWidth = 256;
        [SerializeField, Min(16)] private int textureHeight = 64;
        [SerializeField, Min(0.01f)] private float pulseTickInterval = 0.05f;
        [SerializeField, Min(0.05f)] private float slideDuration = 0.28f;
        [SerializeField, Min(0f)] private float heartbeatScale = 0.14f;
        [SerializeField, Min(0.05f)] private float heartbeatDuration = 0.52f;
        [SerializeField, Range(0f, 1f)] private float ledDotSize = 0.67f;
        [SerializeField] private Color backgroundColor = new(0.002f, 0.004f, 0.012f, 0f);
        [SerializeField] private Color previousColor = new(0.18f, 0.2f, 0.24f, 1f);
        [SerializeField] private Color redColor = new(1f, 0.17f, 0.78f, 1f);
        [SerializeField] private Color blueColor = new(0.18f, 0.62f, 1f, 1f);
        [SerializeField] private Color yellowColor = new(1f, 0.8f, 0f, 1f);
        [SerializeField] private Color arrowColor = new(0.52f, 0.58f, 0.68f, 1f);
        [SerializeField] private TMP_FontAsset completionFont;

        private Renderer targetRenderer;
        private Texture2D displayTexture;
        private MaterialPropertyBlock propertyBlock;
        private Color32[] pixels;
        private Canvas overlayCanvas;
        private RawImage overlayImage;
        private TMP_Text completionText;
        private int lastMatchedTotal = int.MinValue;
        private int lastTotalNotes = int.MinValue;
        private float nextPulseUpdate;
        private int slideFromIndex;
        private float slideStartTime;
        private bool sliding;

        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            textureWidth = Mathf.Clamp(textureWidth, 32, 1024);
            textureHeight = Mathf.Clamp(textureHeight, 16, 512);
            displayTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Goal Chain LED",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };
            pixels = new Color32[textureWidth * textureHeight];
            CreateWorldSpaceOverlay();
            ApplyTexture();
        }

        private void Start()
        {
            if (gameplayController == null)
                gameplayController = FindFirstObjectByType<PuzzleGameplayController>();
            Rebuild(true);
        }

        private void Update()
        {
            if (gameplayController == null || gameplayController.ProgressTracker == null || gameplayController.Level == null)
                return;

            var matched = gameplayController.ProgressTracker.MatchedTotal;
            var total = gameplayController.ProgressTracker.TotalNotes;
            if (matched != lastMatchedTotal || total != lastTotalNotes)
            {
                if (lastMatchedTotal != int.MinValue && matched > lastMatchedTotal)
                {
                    slideFromIndex = lastMatchedTotal;
                    slideStartTime = Time.unscaledTime;
                    sliding = true;
                }
                else
                {
                    sliding = false;
                }

                lastMatchedTotal = matched;
                lastTotalNotes = total;
            }

            var slideProgress = sliding
                ? Mathf.Clamp01((Time.unscaledTime - slideStartTime) / Mathf.Max(0.05f, slideDuration))
                : 1f;
            if (sliding && slideProgress >= 1f)
                sliding = false;

            if (sliding || Time.unscaledTime >= nextPulseUpdate)
                RebuildWindow(slideProgress);

            UpdateCompletionDisplay(gameplayController.ProgressTracker.IsComplete);
        }

        private void Rebuild(bool force)
        {
            if (gameplayController == null || gameplayController.ProgressTracker == null || gameplayController.Level == null)
                return;

            if (!force && Time.unscaledTime < nextPulseUpdate)
                return;
            RebuildWindow(1f);
        }

        private void RebuildWindow(float slideProgress)
        {
            if (gameplayController == null || gameplayController.ProgressTracker == null || gameplayController.Level == null)
                return;

            nextPulseUpdate = Time.unscaledTime + Mathf.Max(0.01f, pulseTickInterval);
            Array.Fill(pixels, ToColor32(backgroundColor));
            var notes = CollectNotes();
            var eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(slideProgress), 3f);
            if (sliding)
            {
                DrawWindow(notes, slideFromIndex, -textureWidth * eased);
                DrawWindow(notes, lastMatchedTotal, textureWidth * (1f - eased));
            }
            else
            {
                DrawWindow(notes, lastMatchedTotal, 0f);
            }

            ApplyTexture();
        }

        private void DrawWindow(List<BeatColor> notes, int matched, float offset)
        {
            var centers = new[]
            {
                textureWidth * 0.2f + offset,
                textureWidth * 0.5f + offset,
                textureWidth * 0.8f + offset
            };
            var baseRadius = Mathf.Max(5f, textureHeight * 0.25f);
            var heartbeat = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.05f, heartbeatDuration));
            for (var i = 0; i < centers.Length; i++)
            {
                var noteIndex = matched + i - 1;
                var valid = noteIndex >= 0 && noteIndex < notes.Count;
                var color = valid ? GetColor(notes[noteIndex]) : previousColor * 0.3f;
                var active = i == 1 && valid && !gameplayController.ProgressTracker.IsComplete;
                var radius = baseRadius * (active ? 1f + heartbeat * heartbeatScale : 1f);
                if (active)
                    color = Color.Lerp(color * 0.8f, color, heartbeat);
                DrawCircle(centers[i], textureHeight * 0.5f, radius, color);
            }
            DrawArrow(textureWidth * 0.35f + offset, textureHeight * 0.5f, arrowColor);
            DrawArrow(textureWidth * 0.65f + offset, textureHeight * 0.5f, arrowColor);
        }

        private List<BeatColor> CollectNotes()
        {
            var notes = new List<BeatColor>();
            foreach (var row in gameplayController.Level.ObjectiveRows)
                for (var i = 0; i < row.NoteCount; i++)
                    notes.Add(row.Notes[i]);
            return notes;
        }

        private Color GetColor(BeatColor beatColor)
        {
            return beatColor switch
            {
                BeatColor.Magenta => redColor,
                BeatColor.Blue => blueColor,
                _ => yellowColor
            };
        }

        private void DrawCircle(float centerX, float centerY, float radius, Color color)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radius));
            var maxX = Mathf.Min(textureWidth - 1, Mathf.CeilToInt(centerX + radius));
            var minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radius));
            var maxY = Mathf.Min(textureHeight - 1, Mathf.CeilToInt(centerY + radius));
            var radiusSquared = radius * radius;
            var edgeRadius = Mathf.Max(1f, radius - 1f);
            var edgeSquared = edgeRadius * edgeRadius;
            for (var y = minY; y <= maxY; y++)
                for (var x = minX; x <= maxX; x++)
                {
                    var distance = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                    if (distance <= radiusSquared)
                        pixels[y * textureWidth + x] = ToColor32(color);
                    if (distance <= radiusSquared && distance > edgeSquared)
                        pixels[y * textureWidth + x] = ToColor32(Color.Lerp(color, Color.white, 0.35f));
                }
        }

        private void DrawArrow(float centerX, float centerY, Color color)
        {
            var start = Mathf.RoundToInt(centerX - textureWidth * 0.035f);
            var end = Mathf.RoundToInt(centerX + textureWidth * 0.035f);
            var y = Mathf.RoundToInt(centerY);
            for (var x = start; x <= end; x++) SetPixel(x, y, color);
            for (var i = 0; i < 5; i++)
            {
                SetPixel(end - i, y - i, color);
                SetPixel(end - i, y + i, color);
            }
        }

        private void SetPixel(int x, int y, Color color)
        {
            if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                pixels[y * textureWidth + x] = ToColor32(color);
        }

        private static Color32 ToColor32(Color color)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.a) * 255f));
        }

        private void ApplyTexture()
        {
            if (displayTexture == null || targetRenderer == null)
                return;
            displayTexture.SetPixels32(pixels);
            displayTexture.Apply(false, false);
            if (overlayImage != null)
            {
                overlayImage.texture = displayTexture;
                overlayImage.color = Color.white;
            }
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(MainTextId, displayTexture);
            propertyBlock.SetColor(LedColorId, Color.white);
            propertyBlock.SetFloat(LedSizeId, ledDotSize);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void CreateWorldSpaceOverlay()
        {
            var overlay = new GameObject("LED Goal Chain Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            overlay.hideFlags = HideFlags.HideAndDontSave;
            // The LED mesh has a deliberately large, non-uniform import scale.
            // Keep the overlay unparented so that scale does not multiply into a
            // full-screen canvas.
            overlay.transform.SetParent(null, true);
            var bounds = targetRenderer != null ? targetRenderer.bounds : new Bounds(transform.position, new Vector3(5f, 2f, 0.1f));
            overlay.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.012f);
            overlay.transform.rotation = Quaternion.identity;
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(500f, 190f);
            var scale = Mathf.Max(0.001f, bounds.size.x / 500f);
            overlay.transform.localScale = Vector3.one * scale;
            overlayCanvas = canvas;

            var imageObject = new GameObject("Goal Chain Display", typeof(RectTransform), typeof(RawImage));
            imageObject.hideFlags = HideFlags.HideAndDontSave;
            imageObject.transform.SetParent(overlay.transform, false);
            var imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            overlayImage = imageObject.GetComponent<RawImage>();
            overlayImage.raycastTarget = false;

            var textObject = new GameObject("Beat Master Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.hideFlags = HideFlags.HideAndDontSave;
            textObject.transform.SetParent(overlay.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(460f, 58f);
            completionText = textObject.GetComponent<TextMeshProUGUI>();
            completionText.text = "BEAT MASTER!";
            completionText.font = completionFont != null ? completionFont : TMP_Settings.defaultFontAsset;
            completionText.alignment = TextAlignmentOptions.Center;
            completionText.fontSize = 32f;
            completionText.fontStyle = FontStyles.Bold;
            completionText.color = new Color(1f, 0.86f, 0.18f, 1f);
            completionText.outlineWidth = 0.22f;
            completionText.outlineColor = new Color(0.05f, 0.01f, 0.08f, 1f);
            completionText.raycastTarget = false;
            textObject.SetActive(false);
        }

        private void UpdateCompletionDisplay(bool complete)
        {
            if (completionText == null)
                return;

            if (!complete)
            {
                if (overlayImage != null)
                    overlayImage.enabled = true;
                if (completionText.gameObject.activeSelf)
                    completionText.gameObject.SetActive(false);
                return;
            }

            if (overlayImage != null)
                overlayImage.enabled = false;
            if (!completionText.gameObject.activeSelf)
                completionText.gameObject.SetActive(true);

            var heartbeat = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.05f, heartbeatDuration));
            var scale = 1f + heartbeat * heartbeatScale * 0.75f;
            completionText.transform.localScale = Vector3.one * scale;
        }

        private void OnDestroy()
        {
            if (overlayCanvas != null)
                Destroy(overlayCanvas.gameObject);
            if (displayTexture != null)
                Destroy(displayTexture);
        }
    }
}
