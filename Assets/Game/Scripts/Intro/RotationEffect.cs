using UnityEngine;

public class RotationEffect : MonoBehaviour
{
    [Header("Rotation Angle Settings")]
    [Tooltip("Sudut rotasi minimal pada sumbu Z (misal: -15)")]
    [SerializeField] private float minAngleZ = -15f;

    [Tooltip("Sudut rotasi maksimal pada sumbu Z (misal: 15)")]
    [SerializeField] private float maxAngleZ = 15f;

    [Header("Speed Settings")]
    [Tooltip("Kecepatan ayunan (makin tinggi makin cepat)")]
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Motion Polish")]
    [Tooltip("Centang untuk ayunan pendulum halus (melambat di ujung). Uncheck untuk ayunan patah-patah linear.")]
    [SerializeField] private bool smoothRotation = true;

    private RectTransform targetRectTransform;

    private void Awake()
    {
        // Mengambil komponen RectTransform (komponen utama untuk objek UI)
        targetRectTransform = GetComponent<RectTransform>();

        if (targetRectTransform == null)
        {
            Debug.LogError($"[RotationEffect] Tidak ditemukan komponen RectTransform pada {gameObject.name}! Skrip ini hanya untuk objek UI.");
        }
    }

    private void Update()
    {
        if (targetRectTransform == null) return;

        // Ambil rotasi saat ini untuk mempertahankan nilai X dan Y
        Vector3 currentAngles = targetRectTransform.localEulerAngles;
        float angleZ;

        if (smoothRotation)
        {
            // Efek ayunan pendulum halus menggunakan Sinus (melambat di ujung)
            // Mathf.Sin memberikan nilai -1 hingga 1.
            float t = (Mathf.Sin(Time.time * rotationSpeed) + 1f) / 2f; // Diubah menjadi 0 hingga 1
            angleZ = Mathf.Lerp(minAngleZ, maxAngleZ, t);
        }
        else
        {
            // Efek ayunan patah-patah/linear (kecepatan konstan)
            float length = maxAngleZ - minAngleZ;
            float t = Mathf.PingPong(Time.time * rotationSpeed, length);
            angleZ = minAngleZ + t;
        }

        // Terapkan rotasi baru hanya pada sumbu Z
        targetRectTransform.localEulerAngles = new Vector3(currentAngles.x, currentAngles.y, angleZ);
    }
}