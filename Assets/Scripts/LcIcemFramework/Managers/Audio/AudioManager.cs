using System.Collections.Generic;
using UnityEngine;

using LcIcemFramework.Core;
using LcIcemFramework.Util.Const;

namespace LcIcemFramework.Managers.Audio
{

/// <summary>
/// 音频管理器，统一管理 BGM 和 SFX
/// </summary>
public class AudioManager : SingletonMono<AudioManager>
{
    private AudioSource _bgmSource;
    private List<AudioSource> _sfxSources = new List<AudioSource>();
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
    }

    #endregion

    #region BGM

    public async void PlayBGM(string audioId)
    {
        _bgmSource.clip = await ManagerHub.Addressables.LoadAsync<AudioClip>(audioId);
        _bgmSource.volume = _bgmVolume;
        _bgmSource.Play();
    }

    public void StopBGM() => _bgmSource.Stop();
    public void PauseBGM() => _bgmSource.Pause();
    public void ResumeBGM() => _bgmSource.UnPause();
    public void MuteBGM() => _bgmSource.mute = true;
    public void UnmuteBGM() => _bgmSource.mute = false;

    #endregion

    #region SFX

    public async void PlaySFX(string audioId)
    {
        var src = _sfxSources.Find(s => !s.isPlaying);
        if (src == null) src = _sfxSources[0];
        src.clip = await ManagerHub.Addressables.LoadAsync<AudioClip>(audioId);
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
}
}
