/// <summary>
/// Buff类型枚举（统一管理药水buff和武器命中buff）
/// </summary>
public enum BuffType
{
    // ========== 药水buff（限时） ==========
    /// <summary>护盾：增加防御力</summary>
    Shield,
    /// <summary>加速：增加移动速度</summary>
    SpeedBoost,

    // ========== 武器命中buff（限时） ==========
    /// <summary>燃烧：DOT持续伤害</summary>
    Burn,
    /// <summary>冰冻：减速效果</summary>
    Freeze,
}
