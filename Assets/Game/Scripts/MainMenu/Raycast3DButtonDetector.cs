using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public class Raycast3DButtonDetector : MonoBehaviour
    {
        [Header("Raycast Active Control")]
        [Tooltip("Centang jika ingin raycast langsung aktif di awal. Jika tidak, panggil EnableRaycast() saat masuk menu pilih level.")]
        [SerializeField] private bool isRaycastActive = false;

        [Header("Camera & Raycast Settings")]
        [Tooltip("Camera acuan (jika kosong akan otomatis mengambil Camera di GameObject ini)")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Layer khusus untuk tombol 3D (pilih layer 3dButton)")]
        [SerializeField] private LayerMask buttonLayer;

        [Tooltip("Jarak maksimum raycast dapat mendeteksi tombol 3D")]
        [SerializeField] private float maxDistance = 15f;

        [Tooltip("Jeda waktu (detik) toleransi saat raycast miss akibat rotasi kamera (mencegah flicker/ngadat saat pertama kali hover)")]
        [SerializeField] private float unhoverDelay = 0.15f;

        [Header("Focus Target List")]
        [Tooltip("List GameObject acuan posisi Kamera & Spotlight saat tombol di-hover (urutan sesuai index level)")]
        [SerializeField] private List<GameObject> focusTargets = new List<GameObject>();

        [Header("Camera Hover Motion Settings")]
        [Tooltip("FOV awal kamera saat tidak ada tombol yang di-hover")]
        [SerializeField] private float defaultFov = 48f;

        [Tooltip("FOV kamera saat tombol 3D sedang di-hover")]
        [SerializeField] private float hoverFov = 25f;

        [Tooltip("Kecepatan rotasi kamera menghadap ke tombol atau kembali ke asal")]
        [SerializeField] private float rotationSpeed = 8f;

        [Tooltip("Kecepatan transisi perubahan FOV kamera")]
        [SerializeField] private float fovSpeed = 8f;

        [Header("Spotlight Reference")]
        [Tooltip("Drag GameObject Spot Light ke sini (bisa berupa GameObject atau komponen LevelMenuSpotlight)")]
        [SerializeField] private GameObject menuSpotlightObject;

        [Header("Preview / Debug Visualizer")]
        [Tooltip("Aktifkan untuk menampilkan garis raycast di Scene View")]
        [SerializeField] private bool showPreviewGizmos = true;
        [SerializeField] private Color hitColor = Color.green;
        [SerializeField] private Color missColor = Color.red;

        private LevelMenuSpotlight menuSpotlight;
        private LevelButton currentHoveredButton;

        // Data rotasi, FOV, & Timer
        private Quaternion defaultRotation;
        private Quaternion targetRotation;
        private float targetFov;
        private float unhoverTimer;

        // Data lokal untuk menggambar Preview Gizmos di Scene View
        private Ray lastRay;
        private Vector3 lastHitPoint;
        private bool isHittingButton;

        public bool IsRaycastActive => isRaycastActive;

        private void Awake()
        {
            // Auto detect Camera jika slot kosong
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
                if (targetCamera == null)
                {
                    targetCamera = Camera.main;
                }
            }

            if (targetCamera != null)
            {
                // Simpan rotasi awal kamera & tetapkan FOV awal
                defaultRotation = targetCamera.transform.rotation;
                targetRotation = defaultRotation;
                targetFov = defaultFov;
                targetCamera.fieldOfView = defaultFov;
            }

            // Auto fetch komponen LevelMenuSpotlight dari GameObject yang di-assign
            if (menuSpotlightObject != null)
            {
                menuSpotlight = menuSpotlightObject.GetComponent<LevelMenuSpotlight>();
                if (menuSpotlight == null)
                {
                    Debug.LogWarning($"[Raycast3DButtonDetector] GameObject '{menuSpotlightObject.name}' tidak memiliki komponen LevelMenuSpotlight!", this);
                }
            }
        }

        private void Update()
        {
            // Jika raycast sedang dimatikan atau kamera null, batalkan proses raycast
            if (!isRaycastActive || targetCamera == null)
            {
                // Tetap lakukan motion kamera agar jika baru diset mati, kamera kembali ke posisi default secara halus
                UpdateCameraMotion();
                return;
            }

            // 1. Dapatkan Ray dari posisi mouse
            lastRay = targetCamera.ScreenPointToRay(Input.mousePosition);

            // 2. Tembakkan Raycast
            if (Physics.Raycast(lastRay, out RaycastHit hit, maxDistance, buttonLayer))
            {
                lastHitPoint = hit.point;

                if (hit.transform.TryGetComponent<LevelButton>(out var button))
                {
                    isHittingButton = true;
                    unhoverTimer = 0f; // Reset timer toleransi unhover

                    // Dapatkan Transform target dari list berdasarkan LevelIndex
                    Transform focusTarget = GetFocusTarget(button);

                    // A. Hover Enter / Stay
                    if (currentHoveredButton != button)
                    {
                        ClearCurrentHover(force: true);
                        currentHoveredButton = button;
                        currentHoveredButton.SetHovered(true);

                        // Fokuskan Spotlight ke target GameObject yang sesuai
                        if (menuSpotlight != null)
                        {
                            bool isUnlocked = LevelUnlockProgress.IsUnlocked(button.LevelIndex);
                            menuSpotlight.FocusOnTarget(focusTarget, isUnlocked);
                        }
                    }

                    // B. Atur Target Rotasi Menghadap ke Center Point Focus Target & Set Target FOV
                    Vector3 targetCenterPoint = GetTargetCenterPoint(focusTarget);
                    Vector3 directionToTarget = targetCenterPoint - targetCamera.transform.position;

                    if (directionToTarget != Vector3.zero)
                    {
                        targetRotation = Quaternion.LookRotation(directionToTarget);
                    }
                    targetFov = hoverFov;

                    // C. Input Klik Kiri Mouse
                    if (Input.GetMouseButtonDown(0))
                    {
                        currentHoveredButton.OnButtonClicked();
                    }

                    UpdateCameraMotion();
                    return; // Keluar agar tidak mengeksekusi logika miss di bawah
                }
            }

            // D. Jika raycast tidak mengenai tombol 3D (Miss)
            isHittingButton = false;

            // Jika sedang meng-hover tombol, berikan toleransi waktu singkat sebelum melepaskannya
            if (currentHoveredButton != null)
            {
                unhoverTimer += Time.deltaTime;
                if (unhoverTimer < unhoverDelay)
                {
                    // Tetap lanjutkan pergerakan rotasi kamera menuju target selama jeda toleransi
                    UpdateCameraMotion();
                    return;
                }
            }

            // Lepas hover jika sudah melewati jeda toleransi
            ClearCurrentHover();

            // Pergerakan rotasi & FOV kembali ke default
            UpdateCameraMotion();
        }

        #region Public Raycast Controls

        /// <summary>
        /// Mengaktifkan pendeteksian raycast (panggil saat masuk menu Pilih Level).
        /// </summary>
        public void EnableRaycast()
        {
            isRaycastActive = true;
        }

        /// <summary>
        /// Mematikan pendeteksian raycast dan mereset status hover (panggil saat kembali ke Main Menu).
        /// </summary>
        public void DisableRaycast()
        {
            isRaycastActive = false;
            isHittingButton = false;
            ClearCurrentHover(force: true);
        }

        /// <summary>
        /// Mengatur aktif/tidaknya raycast secara dinamis.
        /// </summary>
        public void SetRaycastActive(bool active)
        {
            if (active) EnableRaycast();
            else DisableRaycast();
        }

        #endregion

        /// <summary>
        /// Mengambil target GameObject dari focusTargets berdasarkan level index.
        /// Menggunakan transform tombol sebagai fallback jika index tidak valid / list kosong.
        /// </summary>
        private Transform GetFocusTarget(LevelButton button)
        {
            int index = button.LevelIndex;

            if (focusTargets != null && index >= 0 && index < focusTargets.Count)
            {
                if (focusTargets[index] != null)
                {
                    return focusTargets[index].transform;
                }
            }

            // Fallback: Gunakan Transform tombol itu sendiri jika list kosong / index tidak tersedia
            return button.transform;
        }

        /// <summary>
        /// Mengambil titik tengah (center bounds) dari mesh visual target agar kamera/spotlight tidak menembak ke titik pivot bawah/lantai.
        /// </summary>
        private Vector3 GetTargetCenterPoint(Transform target)
        {
            if (target == null) return transform.position;

            Renderer renderer = target.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.center;
            }

            return target.position;
        }

        private void ClearCurrentHover(bool force = false)
        {
            if (currentHoveredButton != null || force)
            {
                if (currentHoveredButton != null)
                {
                    currentHoveredButton.SetHovered(false);
                    currentHoveredButton = null;
                }

                // Reset Spotlight kembali ke posisi/warna default
                if (menuSpotlight != null)
                {
                    menuSpotlight.ResetFocus();
                }
            }

            // Kembalikan target rotasi & FOV ke kondisi default
            targetRotation = defaultRotation;
            targetFov = defaultFov;
            unhoverTimer = 0f;
        }

        private void UpdateCameraMotion()
        {
            if (targetCamera == null) return;

            // Interpolasi rotasi kamera secara halus (Slerp)
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );

            // Interpolasi FOV kamera secara halus (Lerp)
            targetCamera.fieldOfView = Mathf.Lerp(
                targetCamera.fieldOfView,
                targetFov,
                Time.deltaTime * fovSpeed
            );
        }

        // =========================================================
        // PREVIEW RAYCAST DI SCENE VIEW
        // =========================================================
        private void OnDrawGizmos()
        {
            if (!showPreviewGizmos) return;

            Camera cam = targetCamera != null ? targetCamera : GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (Application.isPlaying)
            {
                if (!isRaycastActive) return; // Jangan gambar gizmos saat raycast sedang mati

                if (isHittingButton)
                {
                    // Preview SAAT KENA TOMBOL (Warna Hijau + Bola pada titik sentuh)
                    Gizmos.color = hitColor;
                    Gizmos.DrawLine(lastRay.origin, lastHitPoint);
                    Gizmos.DrawWireSphere(lastHitPoint, 0.25f);
                }
                else
                {
                    // Preview SAAT TIDAK KENA / DILUAR JARAK (Warna Merah)
                    Gizmos.color = missColor;
                    Gizmos.DrawRay(lastRay.origin, lastRay.direction * maxDistance);
                }
            }
            else
            {
                // Preview DI LUAR PLAY MODE (Warna Cyan lurus dari arah kamera)
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(cam.transform.position, cam.transform.forward * maxDistance);
            }
        }

        public void SetDefaultRotation(Quaternion newDefaultRotation)
        {
            defaultRotation = newDefaultRotation;
            if (currentHoveredButton == null)
            {
                targetRotation = defaultRotation;
            }
        }
    }
}