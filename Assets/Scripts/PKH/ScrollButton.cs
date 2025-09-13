using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollButton : MonoBehaviour, ISelectHandler
{
    private AutoScrollView autoScrollView;

    public int buttonIndex;

    private void Awake()
    {
        autoScrollView = GetComponentInParent<AutoScrollView>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        autoScrollView.ChangeButton(buttonIndex);
    }
}
