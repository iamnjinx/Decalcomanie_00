using Njinx.UI;
using TMPro;
using UnityEngine;

public class SettingUI : BaseUI
{
    [SerializeField] private ButtonUI closeButton;

    [Header("Display")]
    [SerializeField] private ButtonUI displayButton;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Resolution")]
    [SerializeField] private TextMeshProUGUI resolutionText;


    [Header("Volume")]
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private SliderUI MasterVolumeSlider;

    [SerializeField] private TextMeshProUGUI effectVolumeText;
    [SerializeField] private SliderUI EffectVolumeSlider;

    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private SliderUI MusicVolumeSlider;

    [Header("In Game Buttons")]
    [SerializeField] private GameObject gameButtonsContainer;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private ButtonUI stageButton;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ButtonUI titleButton;

    [Header("Reset Save")]
    [SerializeField] private GameObject titleButtonsContainer;
    [SerializeField] private TextMeshProUGUI resetSaveText;
    public ButtonUI resetSaveButton;

    [Header("Reset Warning")]
    public BaseUI resetWarning;
    public TextMeshProUGUI resetWarningText;
    public ButtonUI resetWarningYesButton;
    public ButtonUI resetWarningNoButton;

    protected override void Start()
    {
        base.Start();

        EffectVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        MusicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        MasterVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);

        EffectVolumeSlider.onValueChanged.AddListener(_ => GameManager.Instance.SaveSettings());
        MusicVolumeSlider.onValueChanged.AddListener(_ => GameManager.Instance.SaveSettings());
        MasterVolumeSlider.onValueChanged.AddListener(_ => GameManager.Instance.SaveSettings());

        EffectVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        MusicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);
        MasterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);

        closeButton.OnSingleClick += () => SettingManager.Instance.CloseSetting();

        titleButton.OnSingleClick += () =>
        {
            SettingManager.Instance.CloseSetting();
            GameManager.Instance.LoadTitleScene();
        };

        stageButton.OnSingleClick += () =>
        {
            SettingManager.Instance.CloseSetting();
            GameManager.Instance.LoadSelectScene();
        };

        resolutionDropdown.onValueChanged.AddListener(i => SettingManager.Instance.SetResolution(i));

        displayButton.OnSingleClick += () =>
        {
            SettingManager.Instance.ChangeDisplaySetting();
            UpdateDisplayText();
        };

        UpdateSettingLocalization();
    }

    public void SetGameButtons(bool is_true)
    {
        gameButtonsContainer.SetActive(is_true);
        titleButtonsContainer.SetActive(!is_true);
    }

    public void UpdateSettingLocalization()
    {
        LocalizedData data = GameManager.Instance.CurrentLocalizedData;

        UpdateDisplayText();

        resolutionText.text = data.resolutionText;
        masterVolumeText.text = data.masterVolumeText;
        effectVolumeText.text = data.sfxVolumeText;
        musicVolumeText.text = data.musicVolumeText;

        stageText.text = data.backToStageText;
        titleText.text = data.titleText;
        resetSaveText.text = data.resetSaveText;

        resetWarningText.text = data.resetWarningText;
    }

    public void UpdateDisplayText()
    {
        displayText.text = GameManager.Instance.IsFullscreen ? GameManager.Instance.CurrentLocalizedData.fullScreenText : GameManager.Instance.CurrentLocalizedData.windowedText;
    }
}
