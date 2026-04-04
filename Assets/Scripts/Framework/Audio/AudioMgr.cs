using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频管理器，统一管理 BGM 和 SFX
/// </summary>
public class AudioMgr : MonoSingleton<AudioMgr>
{
    // BGM 播放器
    private AudioSource bgmSource;
    // SFX 池
    private List<AudioSource> sfxSources = new List<AudioSource>();
    private const int SFXPoolSize = Constants.SFX_Pool_Size;
    // 音量
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    #region 初始化
    private void Awake()
    {
        // 初始化 BGM 播放器
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // 初始化 SFX 池
        for (int i = 0; i < SFXPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            sfxSources.Add(src);
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
        bgmSource.clip = null; // 占位
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBGM() => bgmSource.Stop();

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseBGM() => bgmSource.Pause();

    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeBGM() => bgmSource.UnPause();

    /// <summary>
    /// 静音背景音乐
    /// </summary>
    public void MuteBGM() => bgmSource.mute = true;

    /// <summary>
    /// 取消背景音乐静音
    /// </summary>
    public void UnmuteBGM() => bgmSource.mute = false;
    #endregion

    #region SFX
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="audioId">音频 ID（由 Addressables 加载）</param>
    public void PlaySFX(string audioId)
    {
        var src = sfxSources.Find(s => !s.isPlaying);
        if (src == null) src = sfxSources[0]; // 全部占用则复用第一个
        // TODO：从 Addressables 加载
        src.volume = sfxVolume;
        src.Play();
    }

    /// <summary>
    /// 停止所有音效
    /// </summary>
    public void StopSFX()
    {
        foreach (var s in sfxSources) s.Stop();
    }

    /// <summary>
    /// 暂停所有音效
    /// </summary>
    public void PauseSFX()
    {
        foreach (var s in sfxSources) s.Pause();
    }

    /// <summary>
    /// 恢复所有音效
    /// </summary>
    public void ResumeSFX()
    {
        foreach (var s in sfxSources) s.UnPause();
    }

    /// <summary>
    /// 静音音效
    /// </summary>
    public void MuteSFX()
    {
        foreach (var s in sfxSources) s.mute = true;
    }

    /// <summary>
    /// 取消音效静音
    /// </summary>
    public void UnmuteSFX()
    {
        foreach (var s in sfxSources) s.mute = false;
    }
    #endregion

    #region 音量
    /// <summary>
    /// 设置 BGM 音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    /// <summary>
    /// 设置 SFX 音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        foreach (var s in sfxSources)
            s.volume = sfxVolume;
    }

    /// <summary>
    /// 获取 BGM 音量
    /// </summary>
    public float GetBGMVolume() => bgmVolume;

    /// <summary>
    /// 获取 SFX 音量
    /// </summary>
    public float GetSFXVolume() => sfxVolume;
    #endregion
}