using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频管理器，统一管理 BGM 和 SFX
/// </summary>
public class AudioMgr : MonoSingleton<AudioMgr>
{
    // BGM 播放器
    private AudioSource _bgmSource;
    // SFX 池
    private List<AudioSource> _sfxSources = new List<AudioSource>();
    private const int SFX_POOL_SIZE = Constants.SFX_POOL_SIZE;
    // 音量
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;

    #region 初始化
    private void Awake()
    {
        // 初始化 BGM 播放器
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;

        // 初始化 SFX 池
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
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="audioId">音频 ID（由 Addressables 加载）</param>
    public void PlayBGM(string audioId)
    {
        // TODO：从 Addressables 加载 AudioClip
        _bgmSource.clip = null; // 占位
        _bgmSource.volume = _bgmVolume;
        _bgmSource.Play();
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBGM() => _bgmSource.Stop();

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseBGM() => _bgmSource.Pause();

    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeBGM() => _bgmSource.UnPause();

    /// <summary>
    /// 静音背景音乐
    /// </summary>
    public void MuteBGM() => _bgmSource.mute = true;

    /// <summary>
    /// 取消背景音乐静音
    /// </summary>
    public void UnmuteBGM() => _bgmSource.mute = false;
    #endregion

    #region SFX
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="audioId">音频 ID（由 Addressables 加载）</param>
    public void PlaySFX(string audioId)
    {
        var src = _sfxSources.Find(s => !s.isPlaying);
        if (src == null) src = _sfxSources[0]; // 全部占用则复用第一个
        // TODO：从 Addressables 加载
        src.volume = _sfxVolume;
        src.Play();
    }

    /// <summary>
    /// 停止所有音效
    /// </summary>
    public void StopSFX()
    {
        foreach (var s in _sfxSources) s.Stop();
    }

    /// <summary>
    /// 暂停所有音效
    /// </summary>
    public void PauseSFX()
    {
        foreach (var s in _sfxSources) s.Pause();
    }

    /// <summary>
    /// 恢复所有音效
    /// </summary>
    public void ResumeSFX()
    {
        foreach (var s in _sfxSources) s.UnPause();
    }

    /// <summary>
    /// 静音音效
    /// </summary>
    public void MuteSFX()
    {
        foreach (var s in _sfxSources) s.mute = true;
    }

    /// <summary>
    /// 取消音效静音
    /// </summary>
    public void UnmuteSFX()
    {
        foreach (var s in _sfxSources) s.mute = false;
    }
    #endregion

    #region 音量
    /// <summary>
    /// 设置 BGM 音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        _bgmSource.volume = _bgmVolume;
    }

    /// <summary>
    /// 设置 SFX 音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        foreach (var s in _sfxSources)
            s.volume = _sfxVolume;
    }

    /// <summary>
    /// 获取 BGM 音量
    /// </summary>
    public float GetBGMVolume() => _bgmVolume;

    /// <summary>
    /// 获取 SFX 音量
    /// </summary>
    public float GetSFXVolume() => _sfxVolume;
    #endregion
}