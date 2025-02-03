using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public AudioClip hoverSoundClip; // 滑鼠進入的音效
    public AudioClip clickSoundClip; // 滑鼠點擊的音效
    public AudioSource audioSource; // 播放音效的組件

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (audioSource == null || hoverSoundClip == null)
        {
            Debug.Log("AudioSource or HoverSoundClip is missing!");
            return;
        }
        if (!audioSource.isActiveAndEnabled)
        {
            Debug.Log("AudioSource is disabled or inactive!");
            return;
        }
        Debug.Log("Enter play");
        audioSource.PlayOneShot(hoverSoundClip);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioSource == null || clickSoundClip == null)
        {
            Debug.Log("AudioSource or ClickSoundClip is missing!");
            return;
        }
        if (!audioSource.isActiveAndEnabled)
        {
            Debug.Log("AudioSource is disabled or inactive!");
            return;
        }
        audioSource.PlayOneShot(clickSoundClip);
    }
}
