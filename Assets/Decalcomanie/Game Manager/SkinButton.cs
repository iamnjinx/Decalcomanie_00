using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinButton : ButtonUI
{
    public Image SkinImage;
    public Image DinoImage;
    public TextMeshProUGUI starCostText;

    public BaseUI selectedFlagUI;

    public DinoSkinSO dinoSkinSO;

    public void SetDinoSkin(int skinID)
    {
        SkinImage.sprite = dinoSkinSO.dinoSkins[skinID];
        starCostText.text = dinoSkinSO.unlockThresholds[skinID].ToString();
    }

    public void SetSelectedFlagActive(bool isSelected)
    {
        if (selectedFlagUI != null)
        {
            selectedFlagUI.SetUI(isSelected);
        }
    }

    public void SetUnlocked(bool isUnlocked)
    {
        SkinImage.color = isUnlocked ? Color.white : Color.black;
        DinoImage.color = isUnlocked ? Color.white : Color.black;

        interactable = isUnlocked;
    }
}
