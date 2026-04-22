using System.Collections;
using System.Collections.Generic;
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

    public void ChangeChapterDisplay(int chapterIndex, int maxChapterIndex)
    {
        selectionRT.DOAnchorPosX(-chapterIndex * 1920f, 0.2f).SetEase(Ease.InOutQuad);

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
    }
}
