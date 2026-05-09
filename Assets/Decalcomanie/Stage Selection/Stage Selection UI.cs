using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Njinx.UI;
using UnityEngine;

public class StageSelectionUI : MonoBehaviour
{
    public List<StagePanel> stagePanels;
    [SerializeField] RectTransform selectionRT;
    public ButtonUI LeftButton;
    public ButtonUI RightButton;

    public ButtonUI BackButton;

    public bool is_changingChapter = false;

    void Start()
    {
        BackButton.OnSingleClick += () => GameManager.Instance.LoadTitleScene();
    }

    public void SetStagePanel()
    {
        foreach(Transform child in selectionRT)
        {
            foreach(Transform grandChild in child)
            {
                StagePanel panel = grandChild.GetComponent<StagePanel>();
                if (panel != null)
                {
                    stagePanels.Add(panel);
                }
            }
        }
    }

    public async void ChangeChapterDisplay(int chapterIndex, int maxChapterIndex, bool is_instant = false)
    {
        is_changingChapter = true;
        LeftButton.HideUI();
        RightButton.HideUI();
        if (is_instant)
        {
            selectionRT.anchoredPosition = new Vector2(-chapterIndex * 1920f, selectionRT.anchoredPosition.y);
        }
        else
        {
            selectionRT.DOAnchorPosX(-chapterIndex * 1920f, 0.2f).SetEase(Ease.InOutQuad);
            await UniTask.Delay(200);
        }

        Debug.Log($"Chapter changed to {chapterIndex}");

        if (chapterIndex == 0)
        {
            LeftButton.SetUI(false);
        }
        else
        {
            LeftButton.SetUI(true);
        }

        if (chapterIndex == maxChapterIndex)
        {
            RightButton.SetUI(false);
        }
        else
        {
            RightButton.SetUI(true);
        }

        is_changingChapter = false;
    }
}
