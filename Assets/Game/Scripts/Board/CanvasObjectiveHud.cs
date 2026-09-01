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

        [Header("Shake Effect Settings")]
        [SerializeField] private float shakeDuration = 0.4f;
        [SerializeField] private float shakeAmount = 10f;

        // Menyimpan jumlah note yang sudah selesai
        private int previousMatchedTotal = -1;

        // Coroutine shake yang sedang berjalan
        private Coroutine shakeCoroutine;

        private void Start()
        {
            if (gameplayController != null &&
                gameplayController.ProgressTracker != null)
            {
                previousMatchedTotal =
                    gameplayController.ProgressTracker.MatchedTotal;
            }
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
            }
            else if (currentIndex < previousMatchedTotal)
            {
                // ======================================
                // PUZZLE DI-RESET
                // ======================================

                // Jangan shake saat reset.
                // Hanya sinkronkan nilai sebelumnya.
                previousMatchedTotal = currentIndex;
            }
            else if (currentIndex > previousMatchedTotal)
            {
                // ======================================
                // NOTE BERHASIL DISELESAIKAN
                // ======================================

                Debug.Log(
                    $"Progress berubah: " +
                    $"{previousMatchedTotal} -> {currentIndex}"
                );

                PlayCenterBoxShake();

                previousMatchedTotal = currentIndex;
            }

            // ==========================================
            // UPDATE 3 SLOT
            // ==========================================

            if (chainSlots.Length >= 3)
            {
                // Slot kiri - box
                SetSlotData(
                    chainSlots[0],
                    allNotes,
                    currentIndex - 1
                );

                // Slot tengah - box (1)
                SetSlotData(
                    chainSlots[1],
                    allNotes,
                    currentIndex
                );

                // Slot kanan - box (2)
                SetSlotData(
                    chainSlots[2],
                    allNotes,
                    currentIndex + 1
                );
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

        // ==========================================
        // START SHAKE BOX TENGAH
        // ==========================================

        private void PlayCenterBoxShake()
        {
            if (chainSlots.Length < 2 ||
                chainSlots[1].noteImage == null)
            {
                return;
            }

            RectTransform target =
                chainSlots[1].noteImage.rectTransform;

            // Kalau shake masih berjalan,
            // hentikan lalu mulai dari awal.
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);

                target.anchoredPosition =
                    target.anchoredPosition;
            }

            shakeCoroutine =
                StartCoroutine(
                    PlayShakeEffect(target)
                );
        }

        // ==========================================
        // CAMERA STYLE POSITION SHAKE
        // ==========================================

        private IEnumerator PlayShakeEffect(
            RectTransform target)
        {
            Debug.Log(
                $"SHAKE START: {target.name}"
            );

            float elapsedTime = 0f;

            // Simpan posisi tepat saat shake dimulai
            Vector2 originalPosition =
                target.anchoredPosition;

            while (elapsedTime < shakeDuration)
            {
                elapsedTime += Time.deltaTime;

                // Progress 0 -> 1
                float normalizedTime =
                    elapsedTime / shakeDuration;

                // Shake semakin kecil mendekati akhir
                float damping =
                    1f - normalizedTime;

                // Random position
                float offsetX =
                    Random.Range(
                        -shakeAmount,
                        shakeAmount
                    ) * damping;

                float offsetY =
                    Random.Range(
                        -shakeAmount,
                        shakeAmount
                    ) * damping;

                target.anchoredPosition =
                    originalPosition +
                    new Vector2(
                        offsetX,
                        offsetY
                    );

                yield return null;
            }

            // Kembalikan ke posisi awal
            target.anchoredPosition =
                originalPosition;

            shakeCoroutine = null;

            Debug.Log(
                $"SHAKE END: {target.name}"
            );
        }

        // ==========================================
        // SAFETY RESET
        // ==========================================

        private void OnDisable()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            if (chainSlots.Length >= 2 &&
                chainSlots[1].noteImage != null)
            {
                RectTransform target =
                    chainSlots[1].noteImage.rectTransform;

                // Posisi tetap mengikuti posisi UI saat ini.
                // Tidak ada perubahan scale / rotation.
            }
        }
    }
}