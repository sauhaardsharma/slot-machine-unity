using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to any Button to get hover and click audio automatically.
/// No manual wiring needed per button — just attach and forget.
/// </summary>
public class UIAudioHook : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance?.PlayHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance?.PlayClick();
    }
}