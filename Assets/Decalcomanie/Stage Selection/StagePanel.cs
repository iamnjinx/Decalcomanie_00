using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;

public class StagePanel: MonoBehaviour
{
    public TextMeshProUGUI stageNameText; 
    public ButtonUI button;

    public void SetStagePanel(int stageID)
    {
        stageNameText.text = $"stage {stageID}";
    }
}
