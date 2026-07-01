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
        
        titleUI.resetButton.OnSingleClick += ResetProgress;
        titleUI.creditButton.OnSingleClick += ShowCredit;

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
        SettingManager.Instance.OpenSetting();
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

    public void ResetProgress()
    {
        SaveManager.Instance.Delete(GameProgressData.SaveKey);
    }
}
