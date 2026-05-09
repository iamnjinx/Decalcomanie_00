using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    [SerializeField] TitleUI titleUI;

    void Start()
    {
        titleUI.selectionButton.OnSingleClick += MoveToSelectScene;
        titleUI.settingButton.OnSingleClick += OpenSetting;
        titleUI.resetButton.OnSingleClick += ResetProgress;
        titleUI.languageButton.OnSingleClick += ChangeLanguage;
        titleUI.quitButton.OnSingleClick += QuitGame;
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
