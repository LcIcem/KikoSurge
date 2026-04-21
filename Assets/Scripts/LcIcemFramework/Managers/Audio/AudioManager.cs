using System.Collections.Generic;
using UnityEngine;

using LcIcemFramework.Core;
using LcIcemFramework.Util.Const;

namespace LcIcemFramework
{

/// <summary>
/// 音频管理器，统一管理 BGM 和 SFX
/// </summary>
public class AudioManager : SingletonMono<AudioManager>
{
    private AudioSource _bgmSource;
    private List<AudioSource> _sfxSources = new List<AudioSource>();
    private AudioSource _ambientSource;
    private const int SFX_POOL_SIZE = Constants.SFX_POOL_SIZE;
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;

    #region 初始化

    protected override void Init()
    {
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;

        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            _sfxSources.Add(src);
        }

        _ambientSource = gameObject.AddComponent<AudioSource>();
        _ambientSource.loop = true;
        _ambientSource.playOnAwake = false;
    }

    #endregion

    #region BGM

    public void PlayBGM(string audioId)
    {
        ManagerHub.Addressables.LoadAsync<AudioClip>(audioId, (clip) =>
        {
            if (clip == null)
                return;
            _bgmSource.clip = clip;
            _bgmSource.volume = _bgmVolume;
            _bgmSource.Play();
        });
    }

    public void StopBGM() => _bgmSource.Stop();
    public void PauseBGM() => _bgmSource.Pause();
    public void ResumeBGM() => _bgmSource.UnPause();
    public void MuteBGM() => _bgmSource.mute = true;
    public void UnmuteBGM() => _bgmSource.mute = false;

    #endregion

    #region Ambient

    public void PlayAmbient(AudioClip clip)
    {
        if (clip == null) return;
        _ambientSource.clip = clip;
        _ambientSource.volume = _sfxVolume;
        _ambientSource.Play();
    }

    public void StopAmbient() => _ambientSource.Stop();
    public void PauseAmbient() => _ambientSource.Pause();
    public void ResumeAmbient() => _ambientSource.UnPause();

    #endregion

    #region SFX

    public void PlaySFX(string audioId)
    {
        var src = _sfxSources.Find(s => !s.isPlaying);
        if (src == null) src = _sfxSources[0];

        ManagerHub.Addressables.LoadAsync<AudioClip>(audioId, (clip) =>
        {
            if (clip == null) return;
            src.clip = clip;
            src.volume = _sfxVolume;
            src.Play();
        });
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        var src = _sfxSources.Find(s => !s.isPlaying);
        if (src == null) src = _sfxSources[0];
        src.clip = clip;
        src.volume = _sfxVolume;
        src.Play();
    }

    public void StopSFX()  { foreach (var s in _sfxSources) s.Stop(); }
    public void PauseSFX() { foreach (var s in _sfxSources) s.Pause(); }
    public void ResumeSFX() { foreach (var s in _sfxSources) s.UnPause(); }
    public void MuteSFX()   { foreach (var s in _sfxSources) s.mute = true; }
    public void UnmuteSFX() { foreach (var s in _sfxSources) s.mute = false; }

    #endregion

    #region 音量

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        _bgmSource.volume = _bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        foreach (var s in _sfxSources) s.volume = _sfxVolume;
    }

    public float GetBGMVolume() => _bgmVolume;
    public float GetSFXVolume() => _sfxVolume;

    #endregion

    #region 静音状态查询

    public bool IsBgmMuted() => _bgmSource.mute;
    public bool IsSfxMuted() => _sfxSources.Count > 0 ? _sfxSources[0].mute : false;

    #endregion

    #region 启动同步

    /// <summary>
    /// 从 GameDataManager 加载并应用音频设置（启动时调用一次）
    /// </summary>
    public void ApplySettings()
    {
        var settings = GameDataManager.Instance?.GetSettingsData();
        if (settings == null) return;
        SetBGMVolume(settings.bgmVolume);
        SetSFXVolume(settings.sfxVolume);
        if (settings.bgmMuted) MuteBGM(); else UnmuteBGM();
        if (settings.sfxMuted) MuteSFX(); else UnmuteSFX();
    }

    #endregion
}
}
