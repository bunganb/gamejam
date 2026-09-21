using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace GameJam.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class LevelButton3D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Level Data & Settings")]
        [SerializeField] private GameLevelDefinition myLevelData;
        [SerializeField, Min(0)] private int levelIndex;
        [SerializeField] private string gameplaySceneName = "GameplayPrototype";
        [SerializeField] private string loadingSceneName = "Loading";

        [Header("3D Visual Feedback")]
        [SerializeField] private Renderer objectRenderer;
        [SerializeField] private Vector3 hoverScaleMultiplier = new Vector3(1.15f, 1.15f, 1.15f);
        [SerializeField] private Color hoverColor = Color.yellow;
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private Vector3 originalScale;
        private Color originalColor;
        private bool isUnlocked;
        private bool isHovered;

        public GameLevelDefinition Level => myLevelData;

        public void Configure(
            GameLevelDefinition level,
            int index,
            string sceneName = "GameplayPrototype",
            string loadingScene = "Loading")
        {
            myLevelData = level;
            levelIndex = index;
            gameplaySceneName = sceneName;
            loadingSceneName = loadingScene;

            RefreshLockState();
        }

        private void Awake()
        {
            originalScale = transform.localScale;

            if (objectRenderer == null)
            {
                objectRenderer = GetComponent<Renderer>();
            }

            if (objectRenderer != null)
            {
                originalColor = objectRenderer.material.color;
            }

            RefreshLockState();
        }

        private void OnEnable()
        {
            RefreshLockState();
        }

        public void RefreshLockState()
        {
            // Cek status unlock dari LevelUnlockProgress
            isUnlocked = LevelUnlockProgress.IsUnlocked(levelIndex);

            // Respon visual untuk status terkunci (Locked)
            if (objectRenderer != null)
            {
                if (!isUnlocked)
                {
                    objectRenderer.material.color = lockedColor;
                }
                else if (!isHovered)
                {
                    objectRenderer.material.color = originalColor;
                }
            }
        }

        #region EventSystem 3D Handlers (Hover & Click)

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isUnlocked) return; // Tidak ada efek hover jika level masih terkunci

            isHovered = true;
            transform.localScale = Vector3.Scale(originalScale, hoverScaleMultiplier);

            if (objectRenderer != null)
            {
                objectRenderer.material.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isUnlocked) return;

            isHovered = false;
            transform.localScale = originalScale;

            if (objectRenderer != null)
            {
                objectRenderer.material.color = originalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            LoadThisLevel();
        }

        #endregion

        public void LoadThisLevel()
        {
            if (!LevelUnlockProgress.IsUnlocked(levelIndex))
            {
                Debug.Log($"Level {levelIndex} ini belum terbuka.", this);
                return;
            }

            if (myLevelData == null)
            {
                Debug.LogWarning("Data level belum di-assign.", this);
                return;
            }

            if (!myLevelData.TryValidate(out var error))
            {
                Debug.LogWarning($"Data level belum valid: {error}", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogWarning("Nama scene gameplay belum diisi.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(loadingSceneName))
            {
                Debug.LogWarning("Nama scene loading belum diisi.", this);
                return;
            }

            Debug.Log($"Membuka {myLevelData.LevelId} melalui {loadingSceneName} menuju {gameplaySceneName}");
            LevelSelectionSession.Select(myLevelData);
            LoaderUtils.SetTargetScene(gameplaySceneName);
            SceneManager.LoadScene(loadingSceneName);
        }
    }
}