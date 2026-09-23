using System.Collections;
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

        [Tooltip("Jika true, raycast baru akan memproses hover SETELAH mouse benar-benar digerakkan oleh player saat raycast diaktifkan.")]
        [SerializeField] private bool requireMouseMovementOnEnable = true;

        [Tooltip("Jarak minimal pergerakan mouse (dalam piksel) untuk memicu raycast pertama kali setelah diaktifkan.")]
        [SerializeField] private float mouseMovementThreshold = 3f;

        [Tooltip("Jeda waktu (detik) sebelum raycast benar-benar aktif setelah EnableRaycast() dipanggil")]
        [SerializeField] private float enableDelay = 0.2f;

        [Tooltip("Jeda waktu (detik) sebelum raycast benar-benar mati setelah DisableRaycast() dipanggil")]
        [SerializeField] private float disableDelay = 0.1f;

        [Header("Camera & Raycast Settings")]
        [Tooltip("Camera acuan (jika kosong akan otomatis mengambil Camera di GameObject ini)")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Layer khusus untuk tombol 3D (pilih layer 3dButton)")]
        [SerializeField] private LayerMask buttonLayer;

        [Tooltip("Jarak maksimum raycast dapat mendeteksi tombol 3D")]
        [SerializeField] private float maxDistance = 15f;

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

        [Header("Hover Stability")]
        [Tooltip("Jarak gerak mouse sebelum hover dilepas setelah raycast kehilangan collider karena kamera ikut bergerak")]
        [SerializeField, Min(1f)] private float hoverReleaseMouseDistance = 18f;

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

        // Protection States
        private Coroutine stateChangeCoroutine;
        private bool isWaitingForMouseMovement = false;
        private Vector3 mousePosOnEnable;
        private bool isCameraTransitionActive;
        private Vector3 lastValidHoverMousePosition;

        // Batas atas untuk deltaTime yang dipakai UpdateCameraMotion, supaya
        // spike deltaTime di frame pertama (sisa loading/scene transition)
        // tidak bikin Slerp/Lerp overshoot dan terlihat seperti jitter/lompat.
        private const float MaxMotionDeltaTime = 0.05f;

        public bool IsRaycastActive => isRaycastActive;
        public bool IsCameraTransitionActive => isCameraTransitionActive;

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
                // Simpan rotasi awal kamera & tetapkan FOV awal.
                // Catatan: ini masih bisa "basi" kalau kamera direposisikan
                // oleh sistem lain (CameraRig, level select controller, dll)
                // setelah Awake() ini berjalan. Nilai ini akan di-refresh lagi
                // di EnableRaycastRoutine() sebelum raycast benar-benar aktif.
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

        private void Start()
        {
            if (isRaycastActive && requireMouseMovementOnEnable)
            {
                isWaitingForMouseMovement = true;
                mousePosOnEnable = Input.mousePosition;
            }
        }

        private void Update()
        {
            // CATATAN: Update() di sini HANYA menghitung/menentukan target
            // rotasi & FOV (state), TIDAK menulis ke transform kamera secara
            // langsung. Penulisan aktual dipindah ke LateUpdate() (lihat di
            // bawah) supaya script lain yang juga menggerakkan kamera
            // (misalnya animasi intro/pan CameraRig) sudah selesai jalan
            // duluan di frame yang sama, sebelum kita menimpa rotasinya.
            // Ini mencegah dua sistem "rebutan" kontrol atas transform kamera.

            // Jika raycast mati atau kamera null, biarkan target kembali ke default & keluar
            if (!isRaycastActive || targetCamera == null)
            {
                targetRotation = defaultRotation;
                targetFov = defaultFov;
                return;
            }

            // Proteksi: Tahan raycast sampai mouse benar-benar digerakkan oleh player
            if (isWaitingForMouseMovement)
            {
                float mouseDelta = Vector3.Distance(Input.mousePosition, mousePosOnEnable);
                if (mouseDelta >= mouseMovementThreshold)
                {
                    isWaitingForMouseMovement = false; // Player menggerakkan mouse, izinkan raycast berjalan normal
                }
                else
                {
                    return; // Jangan jalankan raycast dulu agar tidak 'kaget'
                }
            }

            // 1. Raycast realtime dari posisi mouse
            lastRay = targetCamera.ScreenPointToRay(Input.mousePosition);

            // 2. Tembakkan Raycast
            if (Physics.Raycast(lastRay, out RaycastHit hit, maxDistance, buttonLayer))
            {
                if (hit.transform.TryGetComponent<LevelButton>(out var button))
                {
                    lastHitPoint = hit.point;
                    isHittingButton = true;

                    Transform focusTarget = GetFocusTarget(button);

                    // A. Hover Enter / Stay
                    if (currentHoveredButton != button)
                    {
                        ClearCurrentHover(force: true);
                        currentHoveredButton = button;
                        currentHoveredButton.SetHovered(true);

                        if (menuSpotlight != null)
                        {
                            bool isUnlocked = LevelUnlockProgress.IsUnlocked(button.LevelIndex);
                            menuSpotlight.FocusOnTarget(focusTarget, isUnlocked);
                        }
                    }

                    // Keep an anchor from the latest valid hit. When camera
                    // focus moves the object away from the cursor by itself,
                    // a temporary ray miss must not cancel the hover.
                    lastValidHoverMousePosition = Input.mousePosition;

                    // B. Atur Target Rotasi & FOV Kamera
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

                    return;
                }
            }

            // 3. Jika Raycast Miss (Tidak Kena Tombol)
            isHittingButton = false;

            // Camera rotation/FOV changes screen-space projection. Without
            // hysteresis, the ray alternates hit/miss and makes the first
            // focus visibly shake. Release only after genuine mouse movement.
            if (currentHoveredButton != null &&
                Vector3.Distance(Input.mousePosition, lastValidHoverMousePosition) < hoverReleaseMouseDistance)
            {
                return;
            }

            ClearCurrentHover();
        }

        private void LateUpdate()
        {
            if (isCameraTransitionActive)
            {
                UpdateFieldOfView();
                return;
            }

            // Diterapkan paling akhir di frame ini secara sengaja, supaya
            // menang atas script lain yang mungkin masih menggerakkan
            // kamera (mis. animasi intro CameraRig) di Update() mereka.
            UpdateCameraMotion();
        }

        #region Public Raycast Controls

        /// <summary>
        /// Mengaktifkan pendeteksian raycast.
        /// </summary>
        public void EnableRaycast()
        {
            if (stateChangeCoroutine != null) StopCoroutine(stateChangeCoroutine);
            stateChangeCoroutine = StartCoroutine(EnableRaycastRoutine());
        }

        /// <summary>
        /// Mematikan pendeteksian raycast.
        /// </summary>
        public void DisableRaycast()
        {
            if (stateChangeCoroutine != null) StopCoroutine(stateChangeCoroutine);
            stateChangeCoroutine = StartCoroutine(DisableRaycastRoutine());
        }

        /// <summary>
        /// Mengatur aktif/tidaknya raycast secara dinamis.
        /// </summary>
        public void SetRaycastActive(bool active)
        {
            if (active) EnableRaycast();
            else DisableRaycast();
        }

        private IEnumerator EnableRaycastRoutine()
        {
            if (enableDelay > 0f)
            {
                yield return new WaitForSeconds(enableDelay);
            }

            // CameraMover may need longer than the inspector delay to settle.
            // Waiting for the real completion avoids two scripts writing the
            // camera rotation in the same frame during the first hover.
            while (isCameraTransitionActive)
            {
                yield return null;
            }

            // FIX: re-capture rotasi default DI SINI (bukan hanya di Awake()).
            // Ini memastikan defaultRotation mengikuti posisi kamera yang
            // sebenarnya saat level select benar-benar mulai aktif, bukan
            // rotasi kamera saat scene baru load (yang mungkin sudah berubah
            // karena CameraRig/controller lain memindahkan kamera setelah Awake()).
            //
            // PENTING: kalau kamu punya script terpisah yang menganimasikan
            // kamera masuk ke posisi level-select (intro pan), idealnya
            // EnableRaycast() dipanggil dari CALLBACK selesainya animasi itu
            // (bukan cuma delay timer di sini) supaya dua sistem ini tidak
            // pernah aktif menulis rotasi kamera secara bersamaan.
            if (targetCamera != null)
            {
                defaultRotation = targetCamera.transform.rotation;
                targetRotation = defaultRotation;
                targetFov = defaultFov;
            }

            isRaycastActive = true;

            // Kunci deteksi sampai mouse digerakkan
            if (requireMouseMovementOnEnable)
            {
                isWaitingForMouseMovement = true;
                mousePosOnEnable = Input.mousePosition;
            }

            stateChangeCoroutine = null;
        }

        private IEnumerator DisableRaycastRoutine()
        {
            if (disableDelay > 0f)
            {
                yield return new WaitForSeconds(disableDelay);
            }

            isRaycastActive = false;
            isHittingButton = false;
            isWaitingForMouseMovement = false;
            ClearCurrentHover(force: true);
            stateChangeCoroutine = null;
        }

        #endregion

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

            Debug.LogWarning($"[Raycast3DButtonDetector] focusTargets[{index}] kosong/di luar range untuk '{button.name}' (LevelIndex={index}). Fallback ke posisi tombol itu sendiri.", button);
            return button.transform;
        }

        private Vector3 GetTargetCenterPoint(Transform target)
        {
            if (target == null) return transform.position;

            Renderer renderer = target.GetComponentInChildren<Renderer>();

            // FIX: kalau renderer belum pernah "hidup" (misalnya object baru
            // di-enable/instantiate dan bounds-nya masih kosong/belum valid),
            // jangan pakai renderer.bounds.center karena bisa mengarah ke titik
            // yang salah pada frame-frame awal. Fallback ke posisi transform.
            if (renderer != null && renderer.bounds.size != Vector3.zero)
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

                if (menuSpotlight != null)
                {
                    menuSpotlight.ResetFocus();
                }
            }

            targetRotation = defaultRotation;
            targetFov = defaultFov;
        }

        private void UpdateCameraMotion()
        {
            if (targetCamera == null) return;

            // FIX: clamp deltaTime supaya spike di frame pertama (sisa loading
            // scene/menu) tidak menyebabkan Slerp/Lerp overshoot yang terlihat
            // seperti kamera "lompat"/jitter ke arah raycast, bukan ke target.
            float dt = Mathf.Min(Time.deltaTime, MaxMotionDeltaTime);

            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetRotation,
                dt * rotationSpeed
            );

            UpdateFieldOfView(dt);
        }

        private void UpdateFieldOfView()
        {
            UpdateFieldOfView(Mathf.Min(Time.deltaTime, MaxMotionDeltaTime));
        }

        private void UpdateFieldOfView(float dt)
        {
            if (targetCamera == null) return;

            targetCamera.fieldOfView = Mathf.Lerp(
                targetCamera.fieldOfView,
                targetFov,
                dt * fovSpeed
            );
        }

        private void OnDrawGizmos()
        {
            if (!showPreviewGizmos) return;

            Camera cam = targetCamera != null ? targetCamera : GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (Application.isPlaying)
            {
                if (!isRaycastActive || isWaitingForMouseMovement) return;

                if (isHittingButton)
                {
                    Gizmos.color = hitColor;
                    Gizmos.DrawLine(lastRay.origin, lastHitPoint);
                    Gizmos.DrawWireSphere(lastHitPoint, 0.25f);
                }
                else
                {
                    Gizmos.color = missColor;
                    Gizmos.DrawRay(lastRay.origin, lastRay.direction * maxDistance);
                }
            }
            else
            {
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

        /// <summary>
        /// Gives exclusive camera ownership to an external menu transition.
        /// </summary>
        public void BeginCameraTransition()
        {
            isCameraTransitionActive = true;
            isRaycastActive = false;
            isHittingButton = false;
            isWaitingForMouseMovement = false;
            ClearCurrentHover(force: true);
            targetFov = defaultFov;
        }

        /// <summary>
        /// Returns camera ownership to hover focus using the final transition pose.
        /// </summary>
        public void CompleteCameraTransition(Quaternion finalRotation)
        {
            defaultRotation = finalRotation;
            targetRotation = finalRotation;
            targetFov = defaultFov;
            isCameraTransitionActive = false;
        }
    }
}
