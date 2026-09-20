using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisclaimerTimer : MonoBehaviour
{
    [Header("Scene & Timer Settings")]
    [Tooltip("Durasi waktu tampil (dalam detik)")]
    [SerializeField] private float displayDuration = 3f;

    [Tooltip("Nama scene tujuan (sesuai di Build Settings)")]
    [SerializeField] private string nextSceneName = "MainMenu";

    [Header("UX Polish")]
    [Tooltip("Centang jika pemain boleh skip dengan klik/tekan tombol apa saja")]
    [SerializeField] private bool allowSkip = true;

    private bool isTransitioning = false;

    private void Start()
    {
        StartCoroutine(TimerRoutine());
    }

    private void Update()
    {
        // Fitur Skip: Jika pemain menekan tombol/klik sebelum timer habis
        if (allowSkip && !isTransitioning && Input.anyKeyDown)
        {
            StopAllCoroutines();
            LoadNextScene();
        }
    }

    private IEnumerator TimerRoutine()
    {
        yield return new WaitForSeconds(displayDuration);
        
        if (!isTransitioning)
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        isTransitioning = true;
        SceneManager.LoadScene(nextSceneName);
    }
}