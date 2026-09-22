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

        [Header("Camera Hover Motion Settings")]
        [Tooltip("FOV awal kamera saat tidak ada tombol yang di-hover")]
        [SerializeField] private float defaultFov = 48f;

        [Tooltip("FOV kamera saat tombol 3D sedang di-hover")]
        [SerializeField] private float hoverFov = 25f;

        [Tooltip("Kecepatan rotasi kamera menghadap ke tombol atau kembali ke asal")]
        [SerializeField] private float rotationSpeed = 5f;

        [Tooltip("Kecepatan transisi perubahan FOV kamera")]
        [SerializeField] private float fovSpeed = 5f;

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

        // Data rotasi & FOV
        private Quaternion defaultRotation;
        private Quaternion targetRotation;
        private float targetFov;

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

                    // B. Atur Target Rotasi Menghadap Tombol & Set Target FOV
                    Vector3 directionToButton = button.transform.position - targetCamera.transform.position;
                    if (directionToButton != Vector3.zero)
                    {
                        targetRotation = Quaternion.LookRotation(directionToButton);
                    }
                    targetFov = hoverFov;

                    // C. Input Klik Kiri Mouse
                    if (Input.GetMouseButtonDown(0))
                    {
                        currentHoveredButton.OnButtonClicked();
                    }

                    UpdateCameraMotion();
                    return; // Keluar agar tidak memanggil ClearCurrentHover
                }
            }

            // D. Jika raycast tidak mengenai tombol 3D
            isHittingButton = false;
            ClearCurrentHover();

            // Lakukan pergerakan rotasi & FOV kembali ke default
            UpdateCameraMotion();
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

            // Kembalikan target rotasi & FOV ke kondisi default
            targetRotation = defaultRotation;
            targetFov = defaultFov;
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