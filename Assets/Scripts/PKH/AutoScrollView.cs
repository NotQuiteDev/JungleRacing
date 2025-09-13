using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class AutoScrollView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ScrollRect scrollRect;
    private RectTransform content;

    private GameObject currentButton;
    private RectTransform currentButtonRect;

    public float[] buttonOffsets;

    private bool isLerp = false;
    public int curIndex = 0;
   

    private void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();

        content = scrollRect.content;
    }

    private void Update()
    {
        if (isLerp) MoveContent();
    }


    public void ChangeButton(int index)
    {
        curIndex = index;
        isLerp = true;
    }

    private void MoveContent()
    {
        Vector2 targetPos = new Vector2(buttonOffsets[curIndex], content.anchoredPosition.y);

        content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, targetPos, Time.deltaTime * 10f);

        if (Vector2.Distance(content.anchoredPosition, targetPos) < 0.1f)
        { 
            content.anchoredPosition = targetPos;                       
            isLerp = false;
        }
        else isLerp = true;
    }

    // 마우스 드래그 차단
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData) { }

}
