using UnityEngine;

/// <summary>
/// 药水配置
/// </summary>
[CreateAssetMenu(fileName = "Item_Potion", menuName = "KikoSurge/物品/药水")]
public class PotionItemConfig : ItemConfig
{
    [Header("即时效果")]
    public PotionInstantEffectType instantEffectType = PotionInstantEffectType.None;
    public float instantEffectValue = 0f;

    [Header("限时效果")]
    public PotionTimedEffectType timedEffectType = PotionTimedEffectType.None;
    public float timedEffectDuration = 0f;
    public float timedEffectValue = 0f;
}

/// <summary>
/// 药水即时效果类型
/// </summary>
public enum PotionInstantEffectType
{
    None,
    Heal,      // 恢复生命
}

/// <summary>
/// 药水限时效果类型
/// </summary>
public enum PotionTimedEffectType
{
    None,
    Shield,    // 护盾（限时）
    SpeedBoost, // 加速（限时）
}
