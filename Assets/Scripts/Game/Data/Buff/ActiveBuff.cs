using System;

/// <summary>
/// 激活中的Buff数据结构
/// </summary>
[Serializable]
public class ActiveBuff
{
    /// <summary>唯一标识符</summary>
    public int buffId;

    /// <summary>Buff类型</summary>
    public BuffType type;

    /// <summary>剩余持续时间（秒）</summary>
    public float remainingTime;

    /// <summary>总持续时间（用于刷新时重置）</summary>
    public float totalDuration;

    /// <summary>Buff数值
    /// <para>Shield: 防御力加成值</para>
    /// <para>SpeedBoost: 速度倍率（1.5f = +50%）</para>
    /// <para>Burn: 每秒伤害值</para>
    /// <para>Freeze: 减速倍率（0.5f = 50%速度）</para>
    /// </summary>
    public float value;

    /// <summary>Tick间隔（秒），0表示非DOT Buff</summary>
    public float tickInterval;

    /// <summary>当前Tick计时器</summary>
    public float tickTimer;

    /// <summary>来源ID（itemId 或 weaponId）</summary>
    public string sourceId;

    /// <summary>目标ID（敌人GameObject的InstanceID）</summary>
    public string targetId;

    /// <summary>当前叠加层数</summary>
    public int stackCount;

    /// <summary>最大叠加层数（1表示不可叠加）</summary>
    public int maxStacks;

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => remainingTime <= 0f;

    /// <summary>
    /// 是否为DOT类Buff（需要Tick）
    /// </summary>
    public bool IsDot => tickInterval > 0f;

    /// <summary>
    /// 创建Buff实例
    /// </summary>
    public static ActiveBuff Create(BuffType type, float duration, float value,
        string sourceId, float tickInterval = 0f, int maxStacks = 1, string targetId = null)
    {
        return new ActiveBuff
        {
            buffId = GenerateId(),
            type = type,
            remainingTime = duration,
            totalDuration = duration,
            value = value,
            tickInterval = tickInterval,
            tickTimer = 0f,
            sourceId = sourceId,
            targetId = targetId,
            stackCount = 1,
            maxStacks = maxStacks
        };
    }

    private static int _idCounter = 0;
    public static int GenerateId() => ++_idCounter;

    /// <summary>
    /// 刷新Buff持续时间（如果可叠加则增加层数）
    /// </summary>
    public void Refresh()
    {
        if (stackCount < maxStacks)
        {
            stackCount++;
        }
        remainingTime = totalDuration;
        tickTimer = 0f;
    }

    /// <summary>
    /// Tick推进（返回是否触发了一次Tick）
    /// </summary>
    public bool Tick(float deltaTime)
    {
        remainingTime -= deltaTime;

        if (IsDot)
        {
            tickTimer += deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                return true;
            }
        }
        return false;
    }
}
