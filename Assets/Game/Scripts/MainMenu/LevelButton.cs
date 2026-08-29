using UnityEngine;
using UnityEngine.SceneManagement;
using GameJam.Gameplay; // Tambahkan namespace dari LevelDefinition

public class LevelButton : MonoBehaviour
{
    // Slot untuk menaruh data ScriptableObject yang baru
    public LevelDefinition myLevelData; 

    // Fungsi ini yang akan dipanggil saat tombol/nisan diklik
    public void LoadThisLevel()
    {
        // Mengecek apakah data sudah diisi dan LevelId tidak kosong
        if(myLevelData != null && !string.IsNullOrEmpty(myLevelData.LevelId))
        {
            Debug.Log("Membuka scene: " + myLevelData.LevelId);
            SceneManager.LoadScene(myLevelData.LevelId); // Menggunakan LevelId sebagai nama scene
        }
        else
        {
            Debug.LogWarning("Data level belum dimasukkan atau nama LevelId kosong!");
        }
    }
}