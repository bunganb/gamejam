using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameJam.Gameplay
{
    public class CanvasObjectiveHud : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PuzzleGameplayController gameplayController;

        [System.Serializable]
        public struct ChainSlotUI
        {
            [Tooltip("Drag komponen Image dari box ke sini")]
            public Image noteImage;
        }

        [Header("Sliding Window UI")]
        [SerializeField] private ChainSlotUI[] chainSlots = new ChainSlotUI[3];

        [Header("Completion UI")]
        [SerializeField] private TMP_Text completionText;

        [Header("Color Settings")]
        [SerializeField] private Color redColor = new Color(1f, 0.26f, 0.26f);
        [SerializeField] private Color blueColor = new Color(0.26f, 0.53f, 1f);
        [SerializeField] private Color yellowColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color emptyColor = Color.white;

        [Header("Goal Beat Pulse")]
        [SerializeField, Min(0f)] private float goalPulseScale = 1.12f;
        [SerializeField, Min(0.05f)] private float goalPulseDuration = 0.52f;

        [Header("Goal Horizontal Scroll")]
        [SerializeField, Min(0.05f)] private float goalScrollDuration = 0.24f;
        [SerializeField, Min(0f)] private float goalScrollDistance = 0f;

        // Menyimpan jumlah note yang sudah selesai
        private int previousMatchedTotal = -1;

        private Coroutine scrollCoroutine;
        private RectTransform[] slotTransforms;
        private Vector2[] slotBasePositions;
        private Vector3 centerBaseScale = Vector3.one;

        private void Start()
        {
            if (gameplayController != null &&
                gameplayController.ProgressTracker != null)
            {
                previousMatchedTotal =
                    gameplayController.ProgressTracker.MatchedTotal;
            }

            CacheSlotLayout();
        }

        private void Update()
        {
            UpdateGoalChainUI();
        }

        private void UpdateGoalChainUI()
        {
            if (gameplayController == null ||
                gameplayController.ProgressTracker == null ||
                gameplayController.Level == null)
            {
                return;
            }

            var tracker = gameplayController.ProgressTracker;
            int currentIndex = tracker.MatchedTotal;

            // ==========================================
            // KUMPULKAN SEMUA NOTE
            // ==========================================

            List<BeatColor> allNotes = new List<BeatColor>();

            foreach (var row in gameplayController.Level.ObjectiveRows)
            {
                for (int i = 0; i < row.NoteCount; i++)
                {
                    allNotes.Add(row.Notes[i]);
                }
            }

            // ==========================================
            // UPDATE COMPLETION TEXT
            // ==========================================

            if (completionText != null)
            {
                completionText.text =
                    $"{currentIndex}/{tracker.TotalNotes}";
            }

            // ==========================================
            // DETEKSI PERUBAHAN PROGRESS
            // ==========================================

            if (previousMatchedTotal == -1)
            {
                previousMatchedTotal = currentIndex;
                ApplyGoalChain(currentIndex);
            }
            else if (currentIndex < previousMatchedTotal)
            {
                previousMatchedTotal = currentIndex;
                StopGoalAnimation(true);
                ApplyGoalChain(currentIndex);
            }
            else if (currentIndex > previousMatchedTotal)
            {
                previousMatchedTotal = currentIndex;
                StartGoalScroll(currentIndex);
            }

            // ==========================================
            // UPDATE 3 SLOT
            // ==========================================

            if (scrollCoroutine == null)
            {
                ApplyGoalChain(currentIndex, allNotes);
            }

            UpdateGoalBeatPulse();
        }

        private void CacheSlotLayout()
        {
            if (chainSlots == null || chainSlots.Length < 3 ||
                chainSlots[0].noteImage == null || chainSlots[1].noteImage == null ||
                chainSlots[2].noteImage == null)
            {
                return;
            }

            slotTransforms = new RectTransform[3];
            slotBasePositions = new Vector2[3];
            for (var index = 0; index < 3; index++)
            {
                slotTransforms[index] = chainSlots[index].noteImage.rectTransform;
                slotBasePositions[index] = slotTransforms[index].anchoredPosition;
            }

            centerBaseScale = slotTransforms[1].localScale;
        }

        private void ApplyGoalChain(int currentIndex, List<BeatColor> notes = null)
        {
            if (notes == null)
            {
                notes = CollectNotes();
            }

            if (chainSlots.Length < 3)
            {
                return;
            }

            SetSlotData(chainSlots[0], notes, currentIndex - 1);
            SetSlotData(chainSlots[1], notes, currentIndex);
            SetSlotData(chainSlots[2], notes, currentIndex + 1);
        }

        private List<BeatColor> CollectNotes()
        {
            var notes = new List<BeatColor>();
            foreach (var row in gameplayController.Level.ObjectiveRows)
            {
                for (var index = 0; index < row.NoteCount; index++)
                {
                    notes.Add(row.Notes[index]);
                }
            }

            return notes;
        }

        private void StartGoalScroll(int targetIndex)
        {
            if (slotTransforms == null)
            {
                ApplyGoalChain(targetIndex);
                return;
            }

            StopGoalAnimation(true);
            scrollCoroutine = StartCoroutine(ScrollGoalChain(targetIndex));
        }

        private IEnumerator ScrollGoalChain(int targetIndex)
        {
            var distance = goalScrollDistance;
            if (distance <= 0f)
            {
                distance = Mathf.Abs(slotBasePositions[2].x - slotBasePositions[1].x);
            }

            if (distance <= 0f)
            {
                distance = 64f;
            }

            var elapsed = 0f;
            while (elapsed < goalScrollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / goalScrollDuration);
                var eased = 1f - Mathf.Pow(1f - normalized, 3f);
                var offset = Vector2.left * (distance * eased);
                for (var index = 0; index < slotTransforms.Length; index++)
                {
                    slotTransforms[index].anchoredPosition = slotBasePositions[index] + offset;
                }

                yield return null;
            }

            RestoreSlotPositions();
            ApplyGoalChain(targetIndex);
            scrollCoroutine = null;
        }

        private void UpdateGoalBeatPulse()
        {
            if (slotTransforms == null || slotTransforms.Length < 2 ||
                gameplayController.ProgressTracker.IsComplete)
            {
                if (slotTransforms != null && slotTransforms.Length > 1)
                {
                    slotTransforms[1].localScale = centerBaseScale;
                }

                return;
            }

            var phase = Mathf.Repeat(Time.unscaledTime, goalPulseDuration) / goalPulseDuration;
            var pulse = 0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            var scale = Mathf.Lerp(1f, goalPulseScale, pulse);
            slotTransforms[1].localScale = centerBaseScale * scale;
        }

        private void StopGoalAnimation(bool restorePosition)
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }

            if (restorePosition)
            {
                RestoreSlotPositions();
            }
        }

        private void RestoreSlotPositions()
        {
            if (slotTransforms == null || slotBasePositions == null)
            {
                return;
            }

            for (var index = 0; index < slotTransforms.Length; index++)
            {
                slotTransforms[index].anchoredPosition = slotBasePositions[index];
            }
        }

        // ==========================================
        // SET DATA SLOT
        // ==========================================

        private void SetSlotData(
            ChainSlotUI slot,
            List<BeatColor> notes,
            int targetIndex)
        {
            if (slot.noteImage == null)
            {
                return;
            }

            if (targetIndex >= 0 &&
                targetIndex < notes.Count)
            {
                slot.noteImage.color =
                    GetBeatColor(notes[targetIndex]);
            }
            else
            {
                slot.noteImage.color =
                    emptyColor;
            }
        }

        // ==========================================
        // GET COLOR
        // ==========================================

        private Color GetBeatColor(BeatColor color)
        {
            return color switch
            {
                BeatColor.Magenta => redColor,
                BeatColor.Blue => blueColor,
                _ => yellowColor
            };
        }

        private void OnDisable()
        {
            StopGoalAnimation(true);
            if (slotTransforms != null && slotTransforms.Length > 1)
            {
                slotTransforms[1].localScale = centerBaseScale;
            }
        }
    }
}
