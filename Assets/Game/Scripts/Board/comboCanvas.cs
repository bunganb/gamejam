using UnityEngine;

public class comboCanvas : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // Menyamakan rotasi canvas/group dengan rotasi kamera utama
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}