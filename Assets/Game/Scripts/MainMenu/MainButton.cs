using System.Collections;
using UnityEngine;

public class MainButton : MonoBehaviour
{
    [SerializeField] GameObject settings;
    [SerializeField] private Animator Main;


    public void settingBack()
    {
        StartCoroutine(playButtonAnim(2.5f));
    }

    IEnumerator playButtonAnim(float timer)
    {
        yield return new WaitForSeconds(timer);
        Main.Play("button");
    }
}
