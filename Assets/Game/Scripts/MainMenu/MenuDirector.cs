using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline; 

public class MenuDirector : MonoBehaviour
{
    public PlayableDirector director; 
    
    // Tempat untuk memasukkan file timeline kamu
    public TimelineAsset timelineSetting;
    public TimelineAsset timelineCredit;
    public TimelineAsset timelinePlay; // <-- Tambahan slot untuk Timeline Play

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
        if (director != null && timelineCredit != null)
        {
            director.playableAsset = timelineCredit; 
            director.time = 0; 
            director.Play();
            director.playableGraph.GetRootPlayable(0).SetSpeed(1); 
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
            director.Play(); 
            director.time = director.duration; 
            director.playableGraph.GetRootPlayable(0).SetSpeed(-1); 
        }
    }

    public void TutupCredit()
    {
        if (director != null && timelineCredit != null)
        {
            director.playableAsset = timelineCredit;
            director.Play(); 
            director.time = director.duration; 
            director.playableGraph.GetRootPlayable(0).SetSpeed(-1); 
        }
    }

    // <-- Fungsionalitas baru untuk Tutup Play -->
    public void TutupPlay()
    {
        if (director != null && timelinePlay != null)
        {
            director.playableAsset = timelinePlay;
            director.Play(); 
            director.time = director.duration; 
            director.playableGraph.GetRootPlayable(0).SetSpeed(-1); 
        }
    }
}