using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public TextMeshProUGUI buttonText;
    public GameObject arrowContainer; 
    public Image leftArrow;
    public Image rightArrow;

    [Header("Colors (Gunakan #)")]
    public string normalColorHex = "#197F05"; 
    public string textHoverColorHex = "#FFFFFF"; 
    public string arrowHoverColorHex = "#95271D"; 

    [Header("Font Size")]
    public float normalFontSize = 47f; // Ukuran normal
    public float hoverFontSize = 50f;  // Ukuran saat di-hover

    private Color normalColor;
    private Color textHoverColor;
    private Color arrowHoverColor;

    void Start()
    {
        // Mengubah kode Hex menjadi warna yang bisa dibaca Unity
        ColorUtility.TryParseHtmlString(normalColorHex, out normalColor);
        ColorUtility.TryParseHtmlString(textHoverColorHex, out textHoverColor);
        ColorUtility.TryParseHtmlString(arrowHoverColorHex, out arrowHoverColor);

        // Kondisi awal saat game dimulai
        buttonText.color = normalColor;
        buttonText.fontSize = normalFontSize; // Set ukuran font awal
        
        if (arrowContainer != null)
        {
            arrowContainer.SetActive(false); 
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Saat kursor masuk (hover)
        buttonText.color = textHoverColor;
        buttonText.fontSize = hoverFontSize; // Perbesar font saat hover

        if (arrowContainer != null) arrowContainer.SetActive(true);
        if (leftArrow != null) leftArrow.color = arrowHoverColor;
        if (rightArrow != null) rightArrow.color = arrowHoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Saat kursor keluar
        buttonText.color = normalColor;
        buttonText.fontSize = normalFontSize; // Kembalikan ukuran font seperti semula

        if (arrowContainer != null) arrowContainer.SetActive(false);
    }
}