using System.Collections;
using GameJam.Gameplay;
using UnityEngine;

public class ComboTierManager : MonoBehaviour
{
    [Header("Combo Milestones (Masukkan objek 1, 2, 3)")]
    public GameObject comboTier1; // Untuk combo 3
    public GameObject comboTier2; // Untuk combo 6
    public GameObject comboTier3; // Untuk combo 9
    
    [Header("Fail Indicator")]
    public GameObject failImage;  // Untuk objek 'fail'

    [Header("Pengaturan")]
    public float failDisplayTime = 1f; // Berapa lama tulisan fail muncul di layar
    [SerializeField, Min(0.01f)] private float comboIntroDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float comboOutroDuration = 0.16f;
    [SerializeField, Min(1f)] private float comboIdleScale = 1.06f;
    [SerializeField, Min(0.01f)] private float comboIdleDuration = 0.5f;

    [Header("Gameplay Events")]
    [SerializeField] private PuzzleGameplayEvents gameplayEvents;
    [Header("Combo SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip combo1Clip;
    [SerializeField] private AudioClip combo2Clip;
    [SerializeField] private AudioClip combo3Clip;
    [SerializeField] private AudioClip crowdBooClip;

    private int currentCombo = 0;
    private bool perfectChainUnlocked;
    private GameObject activeComboTier;
    private Coroutine comboAnimation;
    private Vector3 comboTier1Scale;
    private Vector3 comboTier2Scale;
    private Vector3 comboTier3Scale;

    public void ConfigureAudio(AudioSource source, AudioClip combo1, AudioClip combo2,
        AudioClip combo3, AudioClip crowdBoo)
    {
        sfxSource = source;
        combo1Clip = combo1;
        combo2Clip = combo2;
        combo3Clip = combo3;
        crowdBooClip = crowdBoo;
        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    private void OnEnable()
    {
        if (gameplayEvents == null)
        {
            gameplayEvents = FindFirstObjectByType<PuzzleGameplayEvents>();
        }

        if (gameplayEvents == null)
        {
            Debug.LogWarning("ComboTierManager tidak menemukan PuzzleGameplayEvents.", this);
            return;
        }

        gameplayEvents.ChainAdvanced += HandleChainAdvanced;
        gameplayEvents.ChainFailed += HandleChainFailed;
        gameplayEvents.ChainReset += HandleChainReset;
        gameplayEvents.ChainCompleted += HandleChainCompleted;
    }

    private void OnDisable()
    {
        if (gameplayEvents == null)
        {
            return;
        }

        gameplayEvents.ChainAdvanced -= HandleChainAdvanced;
        gameplayEvents.ChainFailed -= HandleChainFailed;
        gameplayEvents.ChainReset -= HandleChainReset;
        gameplayEvents.ChainCompleted -= HandleChainCompleted;
    }

    private void Start()
    {
        // Matikan semua gambar saat game baru dimulai
        ResetSemuaGambar();
    }

    private void Awake()
    {
        comboTier1Scale = GetScale(comboTier1);
        comboTier2Scale = GetScale(comboTier2);
        comboTier3Scale = GetScale(comboTier3);
    }

    private void HandleChainAdvanced(GameplayProgressSnapshot snapshot)
    {
        HitBeat();
    }

    private void HandleChainFailed(GameplayProgressSnapshot snapshot)
    {
        PlaySfx(crowdBooClip);
        MissBeat();
    }

    private void HandleChainReset()
    {
        currentCombo = 0;
        perfectChainUnlocked = false;
        ResetComboTiers();
    }

    private void HandleChainCompleted(GameplayProgressSnapshot snapshot)
    {
        // Tier 3 is the Perfect Chain banner: reveal it only after Full Groove is reached.
        perfectChainUnlocked = true;
        UpdateTampilanCombo();
    }

    // Panggil fungsi ini setiap kali pemain BERHASIL mengenai nada
    public void HitBeat()
    {
        currentCombo++;
        
        // Pastikan gambar fail langsung hilang kalau kita berhasil hit lagi
        if (failImage != null) failImage.SetActive(false);

        UpdateTampilanCombo();
    }

    // Panggil fungsi ini setiap kali pemain GAGAL mengenai nada
    public void MissBeat()
    {
        currentCombo = 0;
        perfectChainUnlocked = false;
        
        // Sembunyikan gambar combo 1, 2, 3
        ResetComboTiers();
        
        // Munculkan gambar Fail
        if (failImage != null)
        {
            failImage.SetActive(true);
            
            // Hilangkan gambar fail otomatis setelah beberapa detik
            CancelInvoke("SembunyikanFail"); 
            Invoke("SembunyikanFail", failDisplayTime); 
        }
    }

    private void UpdateTampilanCombo()
    {
        GameObject targetTier = null;

        // Logika 9 kali berturut-turut
        if (perfectChainUnlocked)
        {
            targetTier = comboTier3;
        }
        // Logika 6 kali berturut-turut
        else if (currentCombo >= 6)
        {
            targetTier = comboTier2;
        }
        // Logika 3 kali berturut-turut
        else if (currentCombo >= 3)
        {
            targetTier = comboTier1;
        }

        if (targetTier != activeComboTier)
        {
            if (comboAnimation != null) StopCoroutine(comboAnimation);
            comboAnimation = StartCoroutine(ChangeComboTier(targetTier));
            PlayTierSfx(targetTier);
        }
    }

    private void PlayTierSfx(GameObject tier)
    {
        if (tier == null) return;
        if (tier == comboTier1) PlaySfx(combo1Clip);
        else if (tier == comboTier2) PlaySfx(combo2Clip);
        else if (tier == comboTier3) PlaySfx(combo3Clip);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }

    private IEnumerator ChangeComboTier(GameObject targetTier)
    {
        GameObject previousTier = activeComboTier;
        activeComboTier = targetTier;

        if (previousTier != null)
        {
            yield return AnimateOut(previousTier, GetCanvasGroup(previousTier), GetScale(previousTier));
            previousTier.SetActive(false);
        }

        if (targetTier != null)
        {
            CanvasGroup canvasGroup = GetCanvasGroup(targetTier);
            Vector3 baseScale = GetScale(targetTier);
            targetTier.SetActive(true);
            targetTier.transform.localScale = baseScale * 0.7f;
            canvasGroup.alpha = 1f;

            yield return AnimateIntro(targetTier, canvasGroup, baseScale);
            comboAnimation = StartCoroutine(IdleComboTier(targetTier, canvasGroup, baseScale));
        }
        else
        {
            comboAnimation = null;
        }
    }

    private IEnumerator AnimateIntro(GameObject tier, CanvasGroup canvasGroup, Vector3 baseScale)
    {
        float elapsed = 0f;
        while (elapsed < comboIntroDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / comboIntroDuration);
            progress = 1f - Mathf.Pow(1f - progress, 3f);
            tier.transform.localScale = Vector3.LerpUnclamped(baseScale * 0.7f, baseScale, progress);
            canvasGroup.alpha = 1f;
            yield return null;
        }

        tier.transform.localScale = baseScale;
    }

    private IEnumerator AnimateOut(GameObject tier, CanvasGroup canvasGroup, Vector3 baseScale)
    {
        float elapsed = 0f;
        Vector3 startScale = tier.transform.localScale;
        Vector3 endScale = baseScale * 1.08f;
        while (elapsed < comboOutroDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / comboOutroDuration);
            tier.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            canvasGroup.alpha = 1f - progress;
            yield return null;
        }

        tier.transform.localScale = baseScale;
        canvasGroup.alpha = 0f;
    }

    private IEnumerator IdleComboTier(GameObject tier, CanvasGroup canvasGroup, Vector3 baseScale)
    {
        while (activeComboTier == tier && tier.activeSelf)
        {
            float progress = (Mathf.Sin(Time.time * Mathf.PI * 2f / comboIdleDuration) + 1f) * 0.5f;
            tier.transform.localScale = Vector3.Lerp(baseScale, baseScale * comboIdleScale, progress);
            canvasGroup.alpha = 1f;
            yield return null;
        }
    }

    private void ResetComboTiers()
    {
        if (comboAnimation != null) StopCoroutine(comboAnimation);
        comboAnimation = null;
        activeComboTier = null;
        ResetTier(comboTier1, comboTier1Scale);
        ResetTier(comboTier2, comboTier2Scale);
        ResetTier(comboTier3, comboTier3Scale);
    }

    private void ResetTier(GameObject tier, Vector3 baseScale)
    {
        if (tier == null) return;
        tier.SetActive(false);
        tier.transform.localScale = baseScale;
        GetCanvasGroup(tier).alpha = 1f;
    }

    private Vector3 GetScale(GameObject tier)
    {
        return tier == null ? Vector3.one : tier.transform.localScale;
    }

    private CanvasGroup GetCanvasGroup(GameObject tier)
    {
        CanvasGroup canvasGroup = tier.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : tier.AddComponent<CanvasGroup>();
    }

    private void SembunyikanFail()
    {
        if (failImage != null) failImage.SetActive(false);
    }

    private void ResetSemuaGambar()
    {
        ResetComboTiers();
        if (failImage) failImage.SetActive(false);
    }
}
