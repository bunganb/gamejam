using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;

    public void Configure(AudioSource source, AudioClip hover, AudioClip click)
    {
        audioSource = source;
        hoverClip = hover;
        clickClip = click;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        Play(hoverClip, hoverVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        Play(clickClip, clickVolume);
    }

    private bool IsInteractable()
    {
        var button = GetComponent<Button>();
        return button == null || button.interactable;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
