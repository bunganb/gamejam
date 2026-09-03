using UnityEngine;
using UnityEngine.UI;

public class ScrollInfinite : MonoBehaviour
{
    public RawImage img;

    public float x;
    public float y;


    void Update()
    {
        img.uvRect = new Rect(img.uvRect.position + new Vector2(x,y) * Time.deltaTime, img.uvRect.size);
    }
}
