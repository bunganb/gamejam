using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public class Raycast3DButtonDetector : MonoBehaviour
    {
        [Header("Camera & Raycast Settings")]
        [Tooltip("Camera acuan (jika kosong akan otomatis mengambil Camera di GameObject ini)")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Layer khusus untuk tombol 3D (pilih layer 3dButton)")]
        [SerializeField] private LayerMask buttonLayer;

        [Tooltip("Jarak maksimum raycast dapat mendeteksi tombol 3D")]
        [SerializeField] private float maxDistance = 15f;

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

        // Data lokal untuk menggambar Preview Gizmos di Scene View
        private Ray lastRay;
        private Vector3 lastHitPoint;
        private bool isHittingButton;

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
            if (targetCamera == null) return;

            // 1. Dapatkan Ray dari posisi mouse
            lastRay = targetCamera.ScreenPointToRay(Input.mousePosition);

            // 2. Tembakkan Raycast
            if (Physics.Raycast(lastRay, out RaycastHit hit, maxDistance, buttonLayer))
            {
                lastHitPoint = hit.point;

                if (hit.transform.TryGetComponent<LevelButton>(out var button))
                {
                    isHittingButton = true;

                    // A. Hover Enter / Stay
                    if (currentHoveredButton != button)
                    {
                        ClearCurrentHover();
                        currentHoveredButton = button;
                        currentHoveredButton.SetHovered(true);

                        // Fokuskan Spotlight ke tombol 3D yang sedang di-hover
                        if (menuSpotlight != null)
                        {
                            bool isUnlocked = LevelUnlockProgress.IsUnlocked(button.LevelIndex);
                            menuSpotlight.FocusOnTarget(button.transform, isUnlocked);
                        }
                    }

                    // B. Input Klik Kiri Mouse
                    if (Input.GetMouseButtonDown(0))
                    {
                        currentHoveredButton.OnButtonClicked();
                    }

                    return; // Keluar agar tidak memanggil ClearCurrentHover
                }
            }

            // C. Jika raycast tidak mengenai tombol 3D
            isHittingButton = false;
            ClearCurrentHover();
        }

        private void ClearCurrentHover()
        {
            if (currentHoveredButton != null)
            {
                currentHoveredButton.SetHovered(false);
                currentHoveredButton = null;

                // Reset Spotlight kembali ke posisi/warna default
                if (menuSpotlight != null)
                {
                    menuSpotlight.ResetFocus();
                }
            }
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
    }
}