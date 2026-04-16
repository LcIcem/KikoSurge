using System;
using UnityEngine;

namespace LcIcemFramework.Data
{
/// <summary>
/// 游戏设置数据结构（JSON 持久化）
/// </summary>
[Serializable]
public class SettingsData
{
    public float bgmVolume = 0.5f;
    public float sfxVolume = 0.5f;
    public bool bgmMuted = false;
    public bool sfxMuted = false;

    // 键位覆盖 JSON
    public string keybindingsJson = "";
}
}
