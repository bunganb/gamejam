using UnityEngine;
using UnityEngine.EventSystems;

public class Artist3DHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Pengaturan 3D (Fake Depth)")]
    [SerializeField] private float maxRotationY = 45f; 
    [SerializeField] private float swingSpeed = 3f;    
    [SerializeField] private float depthScale = 1.15f; // Skala membesar untuk ilusi mendekat

    private bool isHovering = false;
    private Quaternion originalRotation;
    private Vector3 originalScale;

    private void Start()
    {
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;

    public void OnPointerExit(PointerEventData eventData) => isHovering = false;

    private void Update()
    {
        if (isHovering)
        {
            // Mentok ayunan di 45 derajat (kiri-kanan)
            float angleY = Mathf.Sin(Time.time * swingSpeed) * maxRotationY;
            transform.localRotation = Quaternion.Euler(0f, angleY, 0f);
            
            // Efek membesar (ilusi maju ke arah kamera)
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale * depthScale, Time.deltaTime * 10f);
        }
        else
        {
            // Reset mulus saat kursor keluar
            transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRotation, Time.deltaTime * 10f);
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);
        }
    }
}