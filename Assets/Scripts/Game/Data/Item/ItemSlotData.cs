using System;

/// <summary>
/// 背包格子数据
/// <para>存储物品 ID 和堆叠数量，支持空格子判断</para>
/// </summary>
[Serializable]
public class ItemSlotData
{
    /// <summary>
    /// 物品配置 ID，0 表示空格子
    /// </summary>
    public int itemId;

    /// <summary>
    /// 堆叠数量
    /// </summary>
    public int quantity;

    public ItemSlotData(int itemId = 0, int quantity = 0)
    {
        this.itemId = itemId;
        this.quantity = quantity;
    }

    /// <summary>
    /// 是否为空格子
    /// </summary>
    public bool IsEmpty => itemId == 0 || quantity <= 0;
}
