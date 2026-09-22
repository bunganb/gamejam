using UnityEngine;

namespace GameJam.Gameplay
{
    [RequireComponent(typeof(Light))]
    public class LevelMenuSpotlight : MonoBehaviour
    {
        [Header("Default Light Settings")]
        [SerializeField] private Vector3 defaultRotation = new Vector3(124.5f, 0f, 0f);
        [SerializeField] private float defaultSpotAngle = 90f;
        [SerializeField] private Color defaultColor = Color.white; // #FFFFFF
        [SerializeField] private float defaultIntensity = 300f;

        [Header("Hover Settings")]
        [Tooltip("Sudut sorot saat disipitkan ke arah tombol")]
        [SerializeField] private float hoverSpotAngle = 65f;

        [Tooltip("Warna lampu untuk level yang terbuka (Unlocked)")]
        [SerializeField] private Color unlockedColor = new Color(0.976f, 1f, 0.322f, 1f); // #F9FF52

        [Tooltip("Warna lampu untuk level yang terkunci (Locked)")]
        [SerializeField] private Color lockedColor = new Color(0.451f, 0.851f, 1f, 1f); // #73D9FF

        [SerializeField] private float hoverIntensity = 450f;

        [Header("Animation Smoothness")]
        [Tooltip("Kecepatan transisi gerakan dan perubahan cahaya")]
        [SerializeField] private float transitionSpeed = 8f;

        private Light spotLight;
        private bool isLightOn = false;
        private bool isHovering = false;
        private Transform currentTarget;
        private bool isCurrentTargetUnlocked;

        private void Awake()
        {
            spotLight = GetComponent<Light>();
            
            // Pastikan tipe cahaya adalah Spot Light
            if (spotLight.type != LightType.Spot)
            {
                spotLight.type = LightType.Spot;
            }

            // Keadaan awal: Lampu MATI
            TurnOff();
        }

        private void Update()
        {
            if (!isLightOn) return;

            // Hitung Target Nilai berdasarkan Status Hover
            Quaternion targetRotation;
            float targetAngle;
            Color targetColor;
            float targetIntensity;

            if (isHovering && currentTarget != null)
            {
                // A. Kondisi Hover: Mengarah ke Titik Tengah Visual Mesh 3D Tombol
                Vector3 targetCenterPoint = GetTargetCenterPoint(currentTarget);
                Vector3 direction = targetCenterPoint - transform.position;

                if (direction.sqrMagnitude > 0.001f)
                {
                    targetRotation = Quaternion.LookRotation(direction);
                }
                else
                {
                    targetRotation = transform.rotation;
                }

                targetAngle = hoverSpotAngle;
                targetColor = isCurrentTargetUnlocked ? unlockedColor : lockedColor;
                targetIntensity = hoverIntensity;
            }
            else
            {
                // B. Kondisi Default (Tidak Hover)
                targetRotation = Quaternion.Euler(defaultRotation);
                targetAngle = defaultSpotAngle;
                targetColor = defaultColor;
                targetIntensity = defaultIntensity;
            }

            // Transisi Halus (Lerp / Slerp)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
            spotLight.spotAngle = Mathf.Lerp(spotLight.spotAngle, targetAngle, Time.deltaTime * transitionSpeed);
            spotLight.color = Color.Lerp(spotLight.color, targetColor, Time.deltaTime * transitionSpeed);
            spotLight.intensity = Mathf.Lerp(spotLight.intensity, targetIntensity, Time.deltaTime * transitionSpeed);
        }

        #region Helper Functions

        /// <summary>
        /// Mengambil titik tengah fisik/visual dari Mesh 3D agar Spotlight tidak menembak ke titik dasar/pivot lantai
        /// </summary>
        private Vector3 GetTargetCenterPoint(Transform target)
        {
            if (target == null) return transform.position;

            // Cari Renderer pada target atau anak-anaknya (child)
            Renderer renderer = target.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.center;
            }

            // Fallback jika tidak ditemukan Mesh Renderer
            return target.position;
        }

        #endregion

        #region Public Control Functions

        /// <summary>
        /// Mengaktifkan Spotlight
        /// </summary>
        public void TurnOn()
        {
            isLightOn = true;
            spotLight.enabled = true;
            ResetFocus(); // Atur ke tampilan default awal
        }

        /// <summary>
        /// Mematikan Spotlight
        /// </summary>
        public void TurnOff()
        {
            isLightOn = false;
            isHovering = false;
            currentTarget = null;
            spotLight.enabled = false;
        }

        /// <summary>
        /// Dipanggil saat kursor menunjuk (hover) ke salah satu tombol 3D
        /// </summary>
        public void FocusOnTarget(Transform buttonTransform, bool isUnlocked)
        {
            if (!isLightOn) return;

            currentTarget = buttonTransform;
            isCurrentTargetUnlocked = isUnlocked;
            isHovering = true;
        }

        /// <summary>
        /// Dipanggil saat kursor keluar dari tombol (kembali ke kondisi awal)
        /// </summary>
        public void ResetFocus()
        {
            isHovering = false;
            currentTarget = null;
        }

        #endregion
    }
}