using Njinx.UI;
using UnityEngine;

public class SettingUI : BaseUI
{
    [SerializeField] private ButtonUI closeButton;

    [SerializeField] private SliderUI EffectVolumeSlider;
    [SerializeField] private SliderUI MusicVolumeSlider;

    [SerializeField] private ButtonUI titleButton;

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
    }
}
