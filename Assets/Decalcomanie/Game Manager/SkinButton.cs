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

    public Image[] panelUIs = new Image[3];
    public Color[] unlockedColors = new Color[3];

    public DinoSkinSO dinoSkinSO;

    public void SetDinoSkin(int skinID)
    {
        SkinImage.sprite = dinoSkinSO.dinoSkins[skinID];
        starCostText.text = dinoSkinSO.unlockThresholds[skinID].ToString();
    }

    public void SetSelectedFlagActive(bool isSelected)
    {
        panelUIs[0].color = isSelected ? unlockedColors[0] : Color.white;
        panelUIs[2].color = isSelected ? unlockedColors[2] : Color.white;
    }

    public void SetUnlocked(bool isUnlocked)
    {
        SkinImage.color = isUnlocked ? Color.white : Color.black;
        DinoImage.color = isUnlocked ? Color.white : Color.black;

        panelUIs[1].color = isUnlocked ? unlockedColors[1] : Color.white;

        interactable = isUnlocked;
    }
}
