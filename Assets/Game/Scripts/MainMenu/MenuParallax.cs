using UnityEngine;
using UnityEngine.InputSystem; // Penting: Tambahkan library ini

public class MenuParallaxUI : MonoBehaviour
{
    public float offsetMultiplier = 50f;
    public float smoothTime = 0.3f;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector2 velocity;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        // Pastikan mouse terdeteksi agar tidak error (misal jika dimainkan di HP)
        if (Mouse.current == null) return;

        // Mengambil posisi mouse menggunakan New Input System
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Membuat offset dari -0.5 sampai 0.5 
        Vector2 offset = new Vector2(
            (mousePos.x / Screen.width) - 0.5f,
            (mousePos.y / Screen.height) - 0.5f
        );

        // Menentukan posisi target
        Vector2 targetPosition = startPosition + (offset * offsetMultiplier);

        // Menggerakkan UI
        rectTransform.anchoredPosition = Vector2.SmoothDamp(
            rectTransform.anchoredPosition, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );
    }
}