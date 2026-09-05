using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;

public class SkinManager : MonoBehaviour
{
    public ButtonUI ExitButton;
    public TextMeshProUGUI curStarCountText;

    public SkinButton[] SkinButtons;
    public DinoSkinSO dinoSkinSO;

    void Start()
    {
        ExitButton.OnSingleClick += () => ExitSkinScene();

        for (int i = 0; i < SkinButtons.Length; i++)
        {
            int skinID = i;
            SkinButtons[i].OnSingleClick += () => SelectSkin(skinID);
            SkinButtons[i].SetDinoSkin(i);
        }

        RefreshSkinButtons();
        var progress = GameProgressData.Load();
        curStarCountText.text = progress.blueStarCount.ToString();
    }

    public bool IsSkinUnlocked(int skinID, int blueStarCount)
    {
        if (skinID < 0) return false;
        return blueStarCount >= dinoSkinSO.unlockThresholds[skinID];
    }

    public void SelectSkin(int skinID)
    {
        var progress = GameProgressData.Load();
        if (!IsSkinUnlocked(skinID, progress.blueStarCount)) return;

        // 이미 선택된 스킨을 다시 누르면 선택 해제(-1).
        SetDinoSkin(skinID == progress.skinID ? -1 : skinID);
        RefreshSkinButtons();
    }

    public void RefreshSkinButtons()
    {
        var progress = GameProgressData.Load();

        for (int i = 0; i < SkinButtons.Length; i++)
        {
            bool unlocked = IsSkinUnlocked(i, progress.blueStarCount);
            bool isSelected = i == progress.skinID;
            SkinButtons[i].SetUnlocked(unlocked);
            SkinButtons[i].SetSelectedFlagActive(isSelected);
        }
    }

    public void SetDinoSkin(int skinID)
    {
        var progress = GameProgressData.Load();
        progress.skinID = skinID;
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);

        if (skinID >= 0 && skinID < SkinButtons.Length)
            SkinButtons[skinID].SetSelectedFlagActive(true);
    }

    public void ExitSkinScene()
    {
        // 스테이지 선택 씬으로 이동.
        GameManager.Instance.LoadTitleScene();
    }
}
