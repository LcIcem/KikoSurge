using LcIcemFramework;
using LcIcemFramework.Data;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音频设置页面（零拖拽，通过 BasePanel.GetControl 访问控件）
/// </summary>
public class AudioSettingsPage : ISettingsPage
{
    private BasePanel _ownerPanel;

    public string PageKey => "panel_audio";

    public void Init(BasePanel ownerPanel)
    {
        _ownerPanel = ownerPanel;
    }

    public void OnEnter()
    {
        RefreshUI();
    }

    public void OnExit()
    {
        GameDataManager.Instance.SaveSettings();
    }

    public void OnTogValueChanged(string togName, bool value)
    {
        var settings = GameDataManager.Instance.GetSettingsData();
        switch (togName)
        {
            case "tog_BGM":
                if (value) AudioManager.Instance.UnmuteBGM();
                else AudioManager.Instance.MuteBGM();
                settings.bgmMuted = !value;
                break;
            case "tog_SFX":
                if (value) AudioManager.Instance.UnmuteSFX();
                else AudioManager.Instance.MuteSFX();
                settings.sfxMuted = !value;
                break;
        }
    }

    public void OnSliderValueChanged(string sliderName, float value)
    {
        var settings = GameDataManager.Instance.GetSettingsData();
        switch (sliderName)
        {
            case "sli_BGM":
                AudioManager.Instance.SetBGMVolume(value);
                settings.bgmVolume = value;
                break;
            case "sli_SFX":
                AudioManager.Instance.SetSFXVolume(value);
                settings.sfxVolume = value;
                break;
        }
    }

    public void OnClick(string btnName)
    {
    }

    private void RefreshUI()
    {
        var bgmToggle = _ownerPanel?.GetControl<Toggle>("tog_BGM");
        var bgmSlider = _ownerPanel?.GetControl<Slider>("sli_BGM");
        var sfxToggle = _ownerPanel?.GetControl<Toggle>("tog_SFX");
        var sfxSlider = _ownerPanel?.GetControl<Slider>("sli_SFX");

        if (bgmToggle != null) bgmToggle.isOn = !AudioManager.Instance.IsBgmMuted();
        if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.GetBGMVolume();
        if (sfxToggle != null) sfxToggle.isOn = !AudioManager.Instance.IsSfxMuted();
        if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.GetSFXVolume();
    }
}
