using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinButton : ButtonUI
{
    public TextMeshProUGUI skinNameText;
    public Image SkinImage;
    public Image DinoImage;
    public TextMeshProUGUI starCostText;

    public Image[] panelUIs = new Image[3];
    public Color[] unlockedColors = new Color[3];

    public DinoSkinSO dinoSkinSO;

    private int currentSkinID = -1;

    public void SetDinoSkin(int skinID)
    {
        currentSkinID = skinID;

        SkinImage.sprite = dinoSkinSO.dinoSkins[skinID];
        starCostText.text = dinoSkinSO.unlockThresholds[skinID].ToString();

        UpdateSkinLocalization();
    }

    // 현재 언어의 스킨 이름 / 폰트를 적용합니다.
    public void UpdateSkinLocalization()
    {
        if (currentSkinID < 0) return;

        LocalizedData data = GameManager.Instance.CurrentLocalizedData;
        if (data == null) return;

        if (skinNameText != null)
        {
            skinNameText.text = data.GetSkinName(currentSkinID);
            skinNameText.font = data.fontAsset;
        }

        if (starCostText != null) starCostText.font = data.fontAsset;
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
