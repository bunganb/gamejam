using System.Collections;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>Low-volume crowd layer that loops from the full-song transition until the next level.</summary>
    [DisallowMultipleComponent]
    public sealed class WinCrowdChant : MonoBehaviour
    {
        [SerializeField] private PrototypeMusicDirector musicDirector;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private AudioSource chantSource;
        [SerializeField, Range(0f, 1f)] private float chantVolume = .14f;
        [SerializeField, Min(0f)] private float fadeInDuration = .25f;

        private Coroutine fadeRoutine;

        public void ConfigureReferences(
            PrototypeMusicDirector director,
            LevelLoader loader,
            PuzzleGameplayEvents events,
            AudioSource source)
        {
            Unsubscribe();
            musicDirector = director;
            levelLoader = loader;
            gameplayEvents = events;
            chantSource = source;
            if (chantSource != null)
            {
                chantSource.playOnAwake = false;
                chantSource.loop = true;
                chantSource.spatialBlend = 0f;
                chantSource.volume = chantVolume;
            }

            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            Unsubscribe();
            StopChant();
        }

        private void Subscribe()
        {
            if (musicDirector != null)
            {
                musicDirector.FullSongTransitionStarted -= HandleFullSongTransitionStarted;
                musicDirector.FullSongTransitionStarted += HandleFullSongTransitionStarted;
            }

            if (levelLoader != null)
            {
                levelLoader.LevelChanged -= HandleLevelChanged;
                levelLoader.LevelChanged += HandleLevelChanged;
            }

            if (gameplayEvents != null)
            {
                gameplayEvents.ChainReset -= StopChant;
                gameplayEvents.ChainReset += StopChant;
            }
        }

        private void Unsubscribe()
        {
            if (musicDirector != null)
                musicDirector.FullSongTransitionStarted -= HandleFullSongTransitionStarted;
            if (levelLoader != null)
                levelLoader.LevelChanged -= HandleLevelChanged;
            if (gameplayEvents != null)
                gameplayEvents.ChainReset -= StopChant;
        }

        private void HandleLevelChanged(int index, LevelDefinition level) => StopChant();

        private void HandleFullSongTransitionStarted(float duration)
        {
            if (chantSource == null || chantSource.clip == null)
                return;

            StopChant();
            chantSource.loop = true;
            chantSource.volume = fadeInDuration > 0f ? 0f : chantVolume;
            chantSource.Play();
            if (fadeInDuration > 0f)
                fadeRoutine = StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            var elapsed = 0f;
            while (elapsed < fadeInDuration && chantSource != null && chantSource.isPlaying)
            {
                elapsed += Time.unscaledDeltaTime;
                chantSource.volume = chantVolume * Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / fadeInDuration));
                yield return null;
            }

            if (chantSource != null)
                chantSource.volume = chantVolume;
            fadeRoutine = null;
        }

        private void StopChant()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (chantSource != null)
                chantSource.Stop();
        }
    }
}
