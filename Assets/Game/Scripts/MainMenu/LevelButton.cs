using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    // Slot untuk menaruh data KTP Level
    public LevelData myLevelData; 

    // Fungsi ini yang akan dipanggil saat nisan diklik
    public void LoadThisLevel()
    {
        if(myLevelData != null && !string.IsNullOrEmpty(myLevelData.sceneToLoad))
        {
            Debug.Log("Membuka scene: " + myLevelData.sceneToLoad);
            SceneManager.LoadScene(myLevelData.sceneToLoad);
        }
        else
        {
            Debug.LogWarning("Data level belum dimasukkan atau nama scene kosong!");
        }
    }
}