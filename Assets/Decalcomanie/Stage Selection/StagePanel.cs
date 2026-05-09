using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StagePanel: MonoBehaviour
{
    public Image panelImage;
    public TextMeshProUGUI stageNameText; 
    public ButtonUI button;

    public Image screenshot;

    public BaseUI[] starIcons = new BaseUI[3];

    public void SetStagePanel(int stageID, bool isCleared, bool obtainedStar, bool achievedMinMoves, bool isOpened = false, Sprite ss = null)
    {
        stageNameText.text = $"stage {stageID}";
        panelImage.color = GameManager.Instance.GameData.chapterColors[(stageID+9) / 10];

        Debug.Log($"Setting Stage Panel: StageID={stageID}, Cleared={isCleared}, ObtainedStar={obtainedStar}, AchievedMinMoves={achievedMinMoves}");

        SetIcon(starIcons[0], isCleared);
        SetIcon(starIcons[1], isCleared && obtainedStar);
        SetIcon(starIcons[2], isCleared && achievedMinMoves);

        if (isOpened)
        {
            SetOpened(ss);
        }
    }

    private void SetIcon(BaseUI icon, bool show)
    {
        icon.is_shown = !show; // bypass ShowUI/HideUI guard
        if (show) icon.ShowUI();
        else icon.HideUI();
    }

    private void SetOpened(Sprite ss)
    {
        screenshot.sprite = ss;
        screenshot.gameObject.SetActive(true);
    }
}
