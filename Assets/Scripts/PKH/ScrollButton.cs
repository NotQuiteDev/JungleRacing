using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScrollButton : MonoBehaviour, ISelectHandler
{
    private AutoScrollView autoScrollView;

    private Button button;

    public int buttonIndex;

    private void Awake()
    {
        autoScrollView = GetComponentInParent<AutoScrollView>();

        button = GetComponent<Button>();
        button.onClick.AddListener(PlayeScene);
    }

    public void OnSelect(BaseEventData eventData)
    {
        autoScrollView.ChangeButton(buttonIndex);
    }

    public void PlayeScene()
    {
        SceneManager.LoadScene(buttonIndex + 1);
    }
}
