using System;

/// <summary>
/// 单局游戏内的修饰器数据（永久数值加成）
/// <para>用于存储整局有效的静态加成，如遗物奖励、升级强化等</para>
/// <para>临时效果由独立的 Buff 系统处理</para>
/// </summary>
[Serializable]
public class ModifierData
{
    /// <summary>
    /// 修饰器ID（对应 ModifierDefinition_SO 的 ID）
    /// </summary>
    public int modifierId;

    /// <summary>
    /// 修饰器名称
    /// </summary>
    public string modifierName;

    /// <summary>
    /// 影响的属性类型
    /// </summary>
    public ModifierType type;

    /// <summary>
    /// 加成值（正数加，负数减）
    /// </summary>
    public float value;

    public ModifierData() { }

    public ModifierData(int modifierId, string modifierName, ModifierType type, float value)
    {
        this.modifierId = modifierId;
        this.modifierName = modifierName;
        this.type = type;
        this.value = value;
    }
}

/// <summary>
/// 修饰器影响的属性类型
/// </summary>
[Serializable]
public enum ModifierType
{
    /// <summary>最大生命值</summary>
    MaxHealth,
    /// <summary>攻击力</summary>
    Attack,
    /// <summary>防御力</summary>
    Defense,
    /// <summary>移动速度</summary>
    MoveSpeed,
    /// <summary>冲刺速度</summary>
    DashSpeed,
    /// <summary>冲刺持续时间</summary>
    DashDuration,
    /// <summary>无敌持续时间</summary>
    InvincibleDuration,
    /// <summary>受伤持续时间</summary>
    HurtDuration,
}
