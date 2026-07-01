using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [Header("Logo")]
    public Sprite[] logoSprites;

    [Header("Selection")]
    public Image[] selectionImages;
    public Image selectionBackgroundImage;
    public Sprite[] selectionBackgroundSprites;
    public BaseUI[] selectionUIList;

    [Header("Buttons")]
    public TitleSelectionButtons[] selectionButtons = new TitleSelectionButtons[2];
    public ButtonUI creditButton;
    public ButtonUI creditCloseButton;

    public ButtonUI resetButton;

    public BaseUI creditUI;

    public BaseUI fadeUI;

    public void SetLanguage(GameLanguage language)
    {
        Sprite selectedBackgroundSprite = language == GameLanguage.English ? selectionBackgroundSprites[0] : selectionBackgroundSprites[1];
        selectionBackgroundImage.sprite = selectedBackgroundSprite;

        for (int i = 0; i < selectionUIList.Length; i++)
        {
            if (i == (int)language)
            {
                selectionUIList[i].ShowUI();
            }
            else
            {
                selectionUIList[i].HideUI();
            }
        } 
    }
}

[System.Serializable]
public class TitleSelectionButtons
{
    public ButtonUI selectionButton;
    public ButtonUI settingButton;
    public ButtonUI languageButton;
    public ButtonUI quitButton;
}
