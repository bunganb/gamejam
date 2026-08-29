using UnityEngine;
using UnityEngine.EventSystems;

public class SoundPulseEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Pengaturan Audio Visualizer")]
    [SerializeField] private float beatSpeed = 20f;  // Seberapa cepat getarannya
    [SerializeField] private float beatSize = 0.05f; // Seberapa besar membesarnya
    [SerializeField] private float maxZRotation = 3f;// Batas rotasi Z (-3 hingga 3)

    private bool isHovering = false;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    private void Start()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
    }

    private void Update()
    {
        if (isHovering)
        {
            // 1. Efek Scale Jedag-jedug
            float scale = 1f + (Mathf.Sin(Time.time * beatSpeed) * beatSize);
            transform.localScale = originalScale * scale;

            // 2. Efek Rotasi Z (getar kiri-kanan -3 sampai 3)
            float zRot = Mathf.Sin(Time.time * beatSpeed) * maxZRotation;
            transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        }
    }
}