using UnityEngine;
using UnityEngine.EventSystems;

public class ChipButton : MonoBehaviour, IPointerClickHandler
{
    public int value = 10;
    public BetPanel panel;

    public void OnPointerClick(PointerEventData eventData)
    {
        panel.AddChip(value);
    }
}
