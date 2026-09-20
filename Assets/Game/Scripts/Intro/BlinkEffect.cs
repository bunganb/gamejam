using UnityEngine;
using UnityEngine.UI;

public class BlinkEffect : MonoBehaviour
{
    [Header("Blink Speed & Alpha Settings")]
    [Tooltip("Kecepatan berkedip (makin tinggi makin cepat)")]
    [SerializeField] private float blinkSpeed = 3f;

    [Tooltip("Transparansi minimal (0 = benar-benar hilang, 1 = tampak jelas)")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.1f;

    [Tooltip("Transparansi maksimal")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;

    [Header("Mode Kedip")]
    [Tooltip("Centang untuk efek memudar halus (Fade). Uncheck untuk kedip patah-patah (On/Off).")]
    [SerializeField] private bool smoothBlink = true;

    private Graphic targetGraphic;

    private void Awake()
    {
        // Mengambil komponen Graphic (bisa berupa Image, RawImage, atau Text/TMP)
        targetGraphic = GetComponent<Graphic>();

        if (targetGraphic == null)
        {
            Debug.LogError($"[BlinkEffect] Tidak ditemukan komponen Image atau Text pada {gameObject.name}!");
        }
    }

    private void Update()
    {
        if (targetGraphic == null) return;

        Color currentColor = targetGraphic.color;
        float newAlpha;

        if (smoothBlink)
        {
            // Efek gelombang halus menggunakan Sinus
            float lerpVal = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
            newAlpha = Mathf.Lerp(minAlpha, maxAlpha, lerpVal);
        }
        else
        {
            // Efek sakelar patah-patah (On / Off)
            newAlpha = (Mathf.PingPong(Time.time * blinkSpeed, 1f) > 0.5f) ? maxAlpha : minAlpha;
        }

        currentColor.a = newAlpha;
        targetGraphic.color = currentColor;
    }
}