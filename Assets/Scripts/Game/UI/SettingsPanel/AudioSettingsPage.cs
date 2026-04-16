using LcIcemFramework.Managers;
using LcIcemFramework.Managers.Audio;
using LcIcemFramework.Managers.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音频设置页面
/// </summary>
public class AudioSettingsPage : ISettingsPage
{
    // 控件名称常量
    private const string TOG_BGM = "tog_BGM";
    private const string SLI_BGM = "sli_BGM";
    private const string TOG_SFX = "tog_SFX";
    private const string SLI_SFX = "sli_SFX";

    private BasePanel _ownerPanel;

    public void Init(BasePanel ownerPanel)
    {
        _ownerPanel = ownerPanel;
    }

    public void OnEnter()
    {
        ApplyToAudioManager();
        RefreshUI();
    }

    public void OnExit()
    {
    }

    public void OnTogValueChanged(string togName, bool value)
    {
        switch (togName)
        {
            case TOG_BGM:
                if (value) AudioManager.Instance.UnmuteBGM();
                else AudioManager.Instance.MuteBGM();
                GameDataManager.Instance.GetSettingsData().bgmMuted = !value;
                GameDataManager.Instance.SaveSettings();
                break;
            case TOG_SFX:
                if (value) AudioManager.Instance.UnmuteSFX();
                else AudioManager.Instance.MuteSFX();
                GameDataManager.Instance.GetSettingsData().sfxMuted = !value;
                GameDataManager.Instance.SaveSettings();
                break;
        }
    }

    public void OnSliderValueChanged(string sliderName, float value)
    {
        switch (sliderName)
        {
            case SLI_BGM:
                AudioManager.Instance.SetBGMVolume(value);
                GameDataManager.Instance.GetSettingsData().bgmVolume = value;
                GameDataManager.Instance.SaveSettings();
                break;
            case SLI_SFX:
                AudioManager.Instance.SetSFXVolume(value);
                GameDataManager.Instance.GetSettingsData().sfxVolume = value;
                GameDataManager.Instance.SaveSettings();
                break;
        }
    }

    private void ApplyToAudioManager()
    {
        var settings = GameDataManager.Instance.GetSettingsData();
        if (settings == null) return;
        AudioManager.Instance.SetBGMVolume(settings.bgmVolume);
        AudioManager.Instance.SetSFXVolume(settings.sfxVolume);
        if (settings.bgmMuted) AudioManager.Instance.MuteBGM();
        else AudioManager.Instance.UnmuteBGM();
        if (settings.sfxMuted) AudioManager.Instance.MuteSFX();
        else AudioManager.Instance.UnmuteSFX();
    }

    private void RefreshUI()
    {
        if (_ownerPanel == null) return;

        var bgmToggle = _ownerPanel.GetControl<Toggle>(TOG_BGM);
        var bgmSlider = _ownerPanel.GetControl<Slider>(SLI_BGM);
        var sfxToggle = _ownerPanel.GetControl<Toggle>(TOG_SFX);
        var sfxSlider = _ownerPanel.GetControl<Slider>(SLI_SFX);

        if (bgmToggle != null)
            bgmToggle.isOn = !AudioManager.Instance.IsBgmMuted();
        if (bgmSlider != null)
            bgmSlider.value = AudioManager.Instance.GetBGMVolume();

        if (sfxToggle != null)
            sfxToggle.isOn = !AudioManager.Instance.IsSfxMuted();
        if (sfxSlider != null)
            sfxSlider.value = AudioManager.Instance.GetSFXVolume();
    }
}
