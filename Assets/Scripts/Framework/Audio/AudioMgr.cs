using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频管理器，统一管理 BGM 和 SFX
/// </summary>
public class AudioMgr : MonoSingleton<AudioMgr>
{

    // AudioSource：背景音乐（持续播放一个）
    private AudioSource bgmSource;

    // AudioSource 池：同时播放多个 SFX
    private List<AudioSource> sfxSources = new List<AudioSource>();
    private const int SFXPoolSize = Constants.SFX_Pool_Size;

    // 音量配置
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

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

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="audioId"></param>
    public void PlayBGM(string audioId)
    {
        // TODO：实际项目中从 Addressables 加载 AudioClip
        // AudioClip clip = await AddressablesManager.Instance.LoadAsync<AudioClip>(audioId);
        bgmSource.clip = null; // 占位
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="audioId">音效id</param>
    public void PlaySFX(string audioId)
    {
        // 从池中找一个空闲的 AudioSource
        var src = sfxSources.Find(s => !s.isPlaying);
        // 如果全部占用 强制复用一个
        if (src == null) src = sfxSources[0];
        // TODO：从 Addressables 加载
        src.volume = sfxVolume;
        src.Play();
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    /// <param name="type">BGM 或 SFX</param>
    /// <param name="volume">音量大小</param>
    public void SetVolume(string type, float volume)
    {
        volume = Mathf.Clamp01(volume);
        switch (type)
        {
            case "BGM":
                bgmVolume = volume;
                bgmSource.volume = volume;
                break;
            case "SFX":
                sfxVolume = volume;
                break;
        }
    }
}