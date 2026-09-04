using UnityEngine;
using UnityEngine.UI; // Wajib ditambahkan untuk memanipulasi UI

public class SmoothBlink : MonoBehaviour
{
    [Header("Komponen UI (Text / Image)")]
    public Graphic uiElement;

    [Header("Pengaturan Kedip")]
    public float speed = 2f; // Semakin besar, semakin cepat kedipnya
    public float minAlpha = 0.2f; // Batas transparansi paling pudar (0 = hilang total)
    public float maxAlpha = 1.0f; // Batas transparansi paling jelas (1 = solid)

    void Update()
    {
        if (uiElement != null)
        {
            // Ambil warna saat ini
            Color currentColor = uiElement.color;
            
            // Gunakan Mathf.PingPong untuk menaik-turunkan nilai alpha secara mulus
            float blinkValue = Mathf.PingPong(Time.time * speed, 1f);
            
            // Terapkan nilai batas min dan max menggunakan Lerp
            currentColor.a = Mathf.Lerp(minAlpha, maxAlpha, blinkValue);
            
            // Masukkan kembali warnanya ke UI
            uiElement.color = currentColor;
        }
    }
}