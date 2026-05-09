using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    public Sprite[] logoSprites;

    public Image[] selectionImages;

    public Sprite[] selectionSpritesEnglish;
    public Sprite[] selectionSpritesKorean;

    public ButtonUI selectionButton;
    public ButtonUI settingButton;
    public ButtonUI languageButton;
    public ButtonUI quitButton;
    public ButtonUI creditButton;
    public ButtonUI creditCloseButton;

    public ButtonUI resetButton;

    public BaseUI creditUI;

    public BaseUI fadeUI;

    public void SetLanguage(GameLanguage language)
    {
        Sprite[] selectedSprites = language == GameLanguage.English ? selectionSpritesEnglish : selectionSpritesKorean;

        for (int i = 0; i < selectionImages.Length; i++)
        {
            if (i < selectedSprites.Length)
            {
                selectionImages[i].sprite = selectedSprites[i];
                selectionImages[i].SetNativeSize();
            }
        }
    }
}
