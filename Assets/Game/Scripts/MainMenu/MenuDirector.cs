using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline; 
using TMPro;

public class MenuDirector : MonoBehaviour
{
    public PlayableDirector director; 
    
    // Tempat untuk memasukkan file timeline kamu
    public TimelineAsset timelineSetting;
    public TimelineAsset timelineCredit;
    public TimelineAsset timelinePlay; // <-- Tambahan slot untuk Timeline Play
    [SerializeField] private GameObject creditsRoot;

    // --- FUNGSI UNTUK MEMBUKA (ANIMASI MAJU) ---

    public void BukaSetting()
    {
        if (director != null && timelineSetting != null)
        {
            director.playableAsset = timelineSetting; 
            director.time = 0; 
            director.Play();
            director.playableGraph.GetRootPlayable(0).SetSpeed(1); 
        }
    }

    public void BukaCredit()
    {
        EnsureCreditsVisible();
        if (director != null && timelineCredit != null)
        {
            director.playableAsset = timelineCredit; 
            director.time = 0; 
            director.Play();
            director.playableGraph.GetRootPlayable(0).SetSpeed(1); 
        }
    }

    private void EnsureCreditsVisible()
    {
        if (creditsRoot == null)
        {
            // GameObject.Find does not return inactive objects. Credits are
            // intentionally hidden before the timeline opens, so search the
            // loaded scene including inactive objects.
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var candidate in allObjects)
            {
                if (candidate == null || candidate.name != "Credits" || !candidate.scene.IsValid())
                {
                    continue;
                }

                var parent = candidate.transform.parent;
                if (parent != null && parent.name == "MainMenu" && candidate.scene == gameObject.scene)
                {
                    creditsRoot = candidate;
                    break;
                }
            }
        }

        if (creditsRoot == null)
        {
            Debug.LogWarning("Credits root was not found.", this);
            return;
        }

        creditsRoot.SetActive(true);
        foreach (var group in creditsRoot.GetComponentsInChildren<CanvasGroup>(true))
        {
            group.alpha = 1f;
        }

        var texts = creditsRoot.GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            text.enabled = true;
            text.maxVisibleCharacters = int.MaxValue;
            text.ForceMeshUpdate();
        }
    }

    // <-- Fungsionalitas baru untuk Tombol Play -->
    public void BukaPlay()
    {
        if (director != null && timelinePlay != null)
        {
            director.playableAsset = timelinePlay; 
            director.time = 0; 
            director.Play();
            director.playableGraph.GetRootPlayable(0).SetSpeed(1); 
        }
    }

    // --- FUNGSI UNTUK MENUTUP (ANIMASI MUNDUR) ---

    public void TutupSetting()
    {
        if (director != null && timelineSetting != null)
        {
            director.playableAsset = timelineSetting;
            director.time = director.duration;
            director.Play();
            director.playableGraph.GetRootPlayable(0).SetSpeed(-1); 
        }
    }

    public void TutupCredit()
    {
        if (director != null && timelineCredit != null)
        {
            director.playableAsset = timelineCredit;
            director.time = director.duration;
            director.Play();
            director.playableGraph.GetRootPlayable(0).SetSpeed(-1); 
        }
    }

    // <-- Fungsionalitas baru untuk Tutup Play -->
    public void TutupPlay()
    {
        if (director != null && timelinePlay != null)
        {
            director.playableAsset = timelinePlay;
            director.time = director.duration;
            director.Play();
            director.playableGraph.GetRootPlayable(0).SetSpeed(-1); 
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
