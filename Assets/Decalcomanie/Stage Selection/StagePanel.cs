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

    public void SetStagePanel(int stageID)
    {
        stageNameText.text = $"stage {stageID}";
        panelImage.color = GameManager.Instance.GameData.chapterColors[(stageID+9) / 10];
    }
}
