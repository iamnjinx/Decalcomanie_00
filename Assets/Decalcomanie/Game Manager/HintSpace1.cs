using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HintSpace1 : HintSpace
{
    [SerializeField] private TextMeshProUGUI hintNumText;
    
    public void SetHintNumText(int hintNum)
    {
        hintNumText.text = hintNum.ToString();
    }
}
