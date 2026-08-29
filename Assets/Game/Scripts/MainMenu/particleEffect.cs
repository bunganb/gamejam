using System.Collections;
using UnityEngine;

public class particleEffect : MonoBehaviour
{
    [Header("Masukkan GameObject Partikel di sini")]
    [SerializeField] private GameObject particleObject;

    // Panggil fungsi ini dari Button OnClick() atau script lain
    public void TriggerParticle()
    {
        if (particleObject != null)
        {
            // Memulai Coroutine
            StartCoroutine(ToggleParticleRoutine());
        }
        else
        {
            Debug.LogWarning("Target partikel belum dimasukkan ke Inspector!");
        }
    }

    private IEnumerator ToggleParticleRoutine()
    {
        // 1. Mengaktifkan partikel
        particleObject.SetActive(true);

        // 2. Menunggu selama 1 detik
        yield return new WaitForSeconds(2.5f);

        // 3. Mematikan partikel kembali
        particleObject.SetActive(false);
    }
}