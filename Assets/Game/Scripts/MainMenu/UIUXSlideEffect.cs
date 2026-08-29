using UnityEngine;
using UnityEngine.EventSystems;

public class UIUXBouncyEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Pengaturan Bouncy UI")]
    [SerializeField] private float scaleMultiplier = 1.15f; 
    [SerializeField] private float moveDownDistance = 15f;  // Jarak turun ke bawah
    [SerializeField] private float smoothTime = 0.1f;       // Kecepatan membal

    private bool isHovering = false;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    
    private Vector3 scaleVelocity = Vector3.zero;
    private Vector3 posVelocity = Vector3.zero;

    private void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;
    public void OnPointerExit(PointerEventData eventData) => isHovering = false;

    private void Update()
    {
        Vector3 targetScale = isHovering ? originalScale * scaleMultiplier : originalScale;
        
        // Perhatikan tanda minus (-) pada moveDownDistance agar teks meluncur ke bawah
        Vector3 targetPos = isHovering ? originalPosition + new Vector3(0, -moveDownDistance, 0) : originalPosition;

        transform.localScale = Vector3.SmoothDamp(transform.localScale, targetScale, ref scaleVelocity, smoothTime);
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPos, ref posVelocity, smoothTime);
    }
}