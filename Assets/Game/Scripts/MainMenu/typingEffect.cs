using System.Collections;
using UnityEngine;
using TMPro; 
using UnityEngine.EventSystems;

public class typingEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Komponen Teks")]
    [SerializeField] private TextMeshProUGUI roleTextComponent; // Slot untuk teks "PROGRAMMER"
    [SerializeField] private TextMeshProUGUI nameTextComponent; // Slot untuk teks "NAMA"

    [Header("Pengaturan")]
    [SerializeField] private float typingSpeed = 0.05f; // Jeda waktu per karakter

    private Coroutine typingCoroutine;

    private void Start()
    {
        // Pastikan teks tampil utuh (tidak animasi) saat game baru dimulai
        ShowFullText();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Hentikan efek ngetik yang mungkin masih berjalan
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        // Mulai jalankan efek ngetik dari awal
        typingCoroutine = StartCoroutine(TypeBothTexts());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Hentikan coroutine dan langsung tampilkan teks utuh saat mouse keluar
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        ShowFullText();
    }

    private IEnumerator TypeBothTexts()
    {
        // Update mesh agar jumlah karakter terbaca akurat
        if (roleTextComponent != null) roleTextComponent.ForceMeshUpdate();
        if (nameTextComponent != null) nameTextComponent.ForceMeshUpdate();

        int roleTotal = roleTextComponent != null ? roleTextComponent.textInfo.characterCount : 0;
        int nameTotal = nameTextComponent != null ? nameTextComponent.textInfo.characterCount : 0;

        // Cari karakter terpanjang agar loop berjalan sampai teks terpanjang selesai
        int maxCharacters = Mathf.Max(roleTotal, nameTotal);

        // Sembunyikan kedua teks sebelum mulai animasi
        if (roleTextComponent != null) roleTextComponent.maxVisibleCharacters = 0;
        if (nameTextComponent != null) nameTextComponent.maxVisibleCharacters = 0;

        // Menampilkan karakter satu per satu
        for (int i = 0; i <= maxCharacters; i++)
        {
            if (roleTextComponent != null && i <= roleTotal)
                roleTextComponent.maxVisibleCharacters = i;

            if (nameTextComponent != null && i <= nameTotal)
                nameTextComponent.maxVisibleCharacters = i;

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void ShowFullText()
    {
        // Mengubah maxVisibleCharacters ke angka besar (99999) otomatis menampilkan seluruh teks
        if (roleTextComponent != null) roleTextComponent.maxVisibleCharacters = 99999;
        if (nameTextComponent != null) nameTextComponent.maxVisibleCharacters = 99999;
    }
}