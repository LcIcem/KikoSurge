using UnityEngine;

/// <summary>
/// 伤害计算结果
/// </summary>
public struct DamageResult
{
    /// <summary>最终造成的伤害（已应用暴击和防御减伤）</summary>
    public float finalDamage;

    /// <summary>是否为暴击</summary>
    public bool isCrit;

    /// <summary>本次计算的暴击率</summary>
    public float critRate;

    /// <summary>本次使用的暴击伤害倍率</summary>
    public float critMultiplier;

    /// <summary>伤害来源标签</summary>
    public DamageSource source;

    /// <summary>原始伤害（暴击加成前的伤害）</summary>
    public float rawDamage;

    /// <summary>防御减伤量</summary>
    public float defenseReduction;

    /// <summary>世界坐标（用于飘字显示）</summary>
    public Vector3 worldPosition;
}

/// <summary>
/// 伤害来源
/// </summary>
public enum DamageSource
{
    PlayerBullet,
    EnemyAttack,
}
