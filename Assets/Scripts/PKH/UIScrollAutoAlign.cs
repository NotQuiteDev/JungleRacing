using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIScrollAutoAlign : MonoBehaviour
{
    private ScrollRect scrollRect;
    private RectTransform viewport;
    private RectTransform content;

    public float transitionDuration = 0.2f;

    private TransitionHelper transitionHelper = new TransitionHelper();

    [SerializeField] private Button[] buttonList;
    [SerializeField] private Button scrollBarButton;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();

        content = scrollRect.content;
        viewport = scrollRect.viewport;

        ButtonNavigationSetting();
    }

    public void OnSelect(ScrollButton button)
    {
        //HandleOnSelectChange(button);
    }

    private void ButtonNavigationSetting()
    {
        for(int i=0; i< buttonList.Length; i++)
        {
            Navigation nav = buttonList[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            if(i > 0) nav.selectOnLeft = buttonList[i - 1];
            if(i < buttonList.Length - 1) nav.selectOnRight = buttonList[i + 1];
            nav.selectOnDown = scrollBarButton;
            buttonList[i].navigation = nav;
        }
    }

    private void Update()
    {
        if(transitionHelper.InProgress == true)
        {
            transitionHelper.Update();
            content.transform.localPosition = transitionHelper.PosCurrent;
        }
    }

    public void HandleOnSelectChange(GameObject obj)
    {
        float viewportLeftX = GetBorderLeftXLocal(viewport.gameObject);
        float viewportRightX = GetBorderRightXLocal(viewport.gameObject);

        float targetLeft = GetBorderLeftXLocal(obj);
        float targetLeftXViewportOffset = targetLeft - viewportLeftX;

        float targetRight = GetBorderRightXLocal(obj);
        float targetRightXViewportOffset = targetRight - viewportRightX;

        float leftDiff = targetLeftXViewportOffset - viewportLeftX;
        //if(leftDiff < 0f) 

        float rightDiff = targetRightXViewportOffset - viewportRightX;
    }

    private float GetBorderLeftXLocal(GameObject obj)
    {
        Vector3 pos = obj.transform.localPosition / 100f;

        return pos.x;
    }

    private float GetBorderRightXLocal(GameObject obj)
    {
        Vector2 recSize = obj.GetComponent<RectTransform>().rect.size * 0.01f;
        Vector3 pos = obj.transform.localPosition / 100f;
        pos.x -= recSize.x;

        return pos.x;
    }

    private float GetLeftXRelative(GameObject obj)
    {
        float contentX = content.transform.localPosition.x / 100f;
        float targetLeftLocal = GetBorderLeftXLocal(obj);
        float targetRelativeLocal = targetLeftLocal + contentX;

        return targetRelativeLocal;
    }

    private float GetRightXRelative(GameObject obj)
    {
        float contentX = content.transform.localPosition.x / 100f;
        float targetRightLocal = GetBorderRightXLocal(obj);
        float targetRelativeLocal = targetRightLocal + contentX;

        return targetRelativeLocal;
    }

    private void MoveContentObjectByAmount(float amount)
    {
        Vector2 posCurrent = content.transform.localPosition;
        Vector2 posTo = posCurrent;
        posTo.x += amount;

        transitionHelper.TransitionPositionFromTo(posCurrent, posTo, transitionDuration);
    }

    private HorizontalLayoutGroup GetHorizontalLayoutGroup()
    {
        HorizontalLayoutGroup horizontalLayoutGroup = content.GetComponent<HorizontalLayoutGroup>();

        return horizontalLayoutGroup;
    }

    private class TransitionHelper
    {
        private float duration = 0f;
        private float timeElapsed = 0f;
        private float progress = 0f;

        private bool inprogress = false;

        private Vector2 posCurrent;
        private Vector2 posFrom;
        private Vector2 posTo;

        public bool InProgress => inprogress;

        public Vector2 PosCurrent => posCurrent;

        public void Update()
        {
            Tick();

            CalculatePosition();
        }

        public void Clear()
        {
            duration = 0f;
            timeElapsed = 0f;
            progress = 0f;

            inprogress = false;
        }

        public void TransitionPositionFromTo(Vector2 posFrom, Vector2 posTo, float duration)
        {
            Clear();

            this.posFrom = posFrom;
            this.posTo = posTo;
            this.duration = duration;

            inprogress = true;
        }

        private void CalculatePosition()
        {
            posCurrent.x = Mathf.Lerp(posFrom.x, posTo.x, progress);
            posCurrent.y = Mathf.Lerp(posFrom.y, posTo.y, progress);
        }

        private void Tick()
        {
            if (inprogress == false) return;

            timeElapsed += Time.deltaTime; ;
            progress += timeElapsed / duration;

            if (progress >= 1f)
            {
                progress = 1f;
                TransitionComplete();
            }
        }

        private void TransitionComplete()
        {
            inprogress = false;
        }
    }
}