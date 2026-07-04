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

    public GameObject questionMakrkIcon;
    public Image screenshot;

    public BaseUI[] starIcons = new BaseUI[3];

    public void SetStagePanel(int stageID, bool isCleared, bool obtainedStar, bool achievedMinMoves, bool isOpened = false, Sprite ss = null)
    {
        stageNameText.text = $"STAGE {stageID/10+1}-{stageID%10+1}";
        panelImage.color = GameManager.Instance.GameData.chapterColors[stageID / 5];

        //Debug.Log($"Setting Stage Panel: StageID={stageID}, Cleared={isCleared}, ObtainedStar={obtainedStar}, AchievedMinMoves={achievedMinMoves}");

        SetIcon(starIcons[0], isCleared);
        SetIcon(starIcons[1], isCleared && obtainedStar);
        SetIcon(starIcons[2], isCleared && achievedMinMoves);

        SetOpened(ss, isOpened);
    }

    private void SetIcon(BaseUI icon, bool show)
    {
        icon.is_shown = !show; // bypass ShowUI/HideUI guard
        if (show) icon.ShowUI();
        else icon.HideUI();
    }

    private void SetOpened(Sprite ss, bool isOpened = true)
    {
        screenshot.sprite = ss;
        screenshot.gameObject.SetActive(isOpened);
        questionMakrkIcon.SetActive(!isOpened);
    }
}
