using System;

/// <summary>
/// 药水限时效果信息（用于UI显示）
/// </summary>
[Serializable]
public class PotionEffectInfo
{
    /// <summary>
    /// 效果类型
    /// </summary>
    public BuffType type;

    /// <summary>
    /// 效果数值
    /// </summary>
    public float value;

    /// <summary>
    /// 剩余时间（秒）
    /// </summary>
    public float remainingTime;
}
