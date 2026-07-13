using Njinx.UI;
using TMPro;
using UnityEngine;

public class SettingUI : BaseUI
{
    [SerializeField] private ButtonUI closeButton;

    [SerializeField] private SliderUI EffectVolumeSlider;
    [SerializeField] private SliderUI MusicVolumeSlider;

    [SerializeField] private ButtonUI titleButton;
    [SerializeField] private ButtonUI stageButton;
    [SerializeField] private ButtonUI displayButton;
    [SerializeField] private TextMeshProUGUI displayText;

    [SerializeField] private TMP_Dropdown resolutionDropdown;

    protected override void Start()
    {
        base.Start();

        EffectVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        MusicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        EffectVolumeSlider.onValueChanged.AddListener(_ => GameManager.Instance.SaveSettings());
        MusicVolumeSlider.onValueChanged.AddListener(_ => GameManager.Instance.SaveSettings());

        EffectVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        MusicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);

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
    }

    public void SetGameButtons(bool is_true)
    {
        titleButton.gameObject.SetActive(is_true);
        stageButton.gameObject.SetActive(is_true);
    }

    public void UpdateDisplayText()
    {
        displayText.text = GameManager.Instance.IsFullscreen ? "Full Screen" : "Windowed";
    }
}
