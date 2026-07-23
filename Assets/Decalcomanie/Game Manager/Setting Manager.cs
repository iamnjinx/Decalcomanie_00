using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [SerializeField] private SettingUI settingUI;

    public bool IsSettingOpen => settingUI.is_shown;

    (int, int)[] resolutions = new (int, int)[]
    {
        (3840, 2160),
        (2560, 1440),
        (1920, 1080),
        (1280, 720)
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        settingUI.resetSaveButton.OnSingleClick += () => settingUI.resetWarning.ShowUI();
        settingUI.resetWarningYesButton.OnSingleClick += ResetProgress;
        settingUI.resetWarningNoButton.OnSingleClick += () => settingUI.resetWarning.HideUI();
    }

    public void OpenSetting(bool showGameButtons = true)
    {
        settingUI.SetGameButtons(showGameButtons);
        settingUI.UpdateSettingLocalization();

        if (!settingUI.is_shown)
        {
            settingUI.ShowUI();
        }
    }

    public void CloseSetting()
    {
        settingUI.SetGameButtons(true);

        if (settingUI.is_shown) settingUI.HideUI();
    }

    public void SetSettingUI()
    {
        if(settingUI.is_shown)
        {
            CloseSetting();
        }
        else
        {
            OpenSetting();
        }
    }

    public void ChangeDisplaySetting()
    {
        GameManager.Instance.ToggleDisplayMode();
    }

    public void SetResolution(int i)
    {
        if (i < 0 || i >= resolutions.Length) return;

        var (width, height) = resolutions[i];
        var mode = GameManager.Instance.IsFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(width, height, mode);
    }

    public void ResetProgress()
    {
        SaveManager.Instance.Delete(GameProgressData.SaveKey);
        settingUI.resetWarning.HideUI();
    }
}
