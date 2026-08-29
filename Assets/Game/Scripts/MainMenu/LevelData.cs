using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public string levelName; // Misalnya "Hutan Gelap" atau "Level 1"
    public int levelNumber; // Angka urutan level (1, 2, 3)
    public Sprite levelIcon; // Gambar preview level yang muncul di menu
    public string sceneToLoad; // NAMA SCENE persis seperti yang ada di Build Settings!
    // Kamu juga bisa menambahkan hal lain nanti, misal:
    // public int requiredScoreToUnlock;
}