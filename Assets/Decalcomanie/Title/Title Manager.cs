using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    [SerializeField] TitleUI titleUI;

    void Start()
    {
        for (int i = 0; i < titleUI.selectionButtons.Length; i++)
        {
            if (i < titleUI.selectionButtons.Length)
            {
                TitleSelectionButtons buttons = titleUI.selectionButtons[i];
                buttons.selectionButton.OnSingleClick += MoveToSelectScene;
                buttons.settingButton.OnSingleClick += OpenSetting;
                buttons.languageButton.OnSingleClick += ChangeLanguage;
                buttons.quitButton.OnSingleClick += QuitGame;
            }
        }

        titleUI.creditButton.OnSingleClick += ShowCredit;

        StartCoroutine(InitLanguageNextFrame());
    }

    IEnumerator InitLanguageNextFrame()
    {
        yield return null;
        titleUI.SetLanguage(GameManager.Instance.CurrentLanguage);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SettingManager.Instance.CloseSetting();
        }
    }

    public void MoveToSelectScene()
    {
        if (!SaveManager.Instance.Exists(GameProgressData.SaveKey))
            GameManager.Instance.LoadIntroScene();
        else
            GameManager.Instance.LoadSelectScene();
    }

    public void OpenSetting()
    {
        SettingManager.Instance.OpenSetting(showGameButtons: false);
    }

    public void ChangeLanguage()
    {
        GameLanguage newLanguage = GameManager.Instance.ToggleLanguage();
        titleUI.SetLanguage(newLanguage);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ShowCredit()
    {
        GameManager.Instance.LoadCreditScene();
    }
}
