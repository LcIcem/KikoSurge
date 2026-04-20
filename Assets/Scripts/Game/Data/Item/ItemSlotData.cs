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

    /// <summary>
    /// 武器当前弹药（仅武器槽位使用）
    /// </summary>
    public int ammo;

    /// <summary>
    /// 默认构造函数（LitJson 反序列化需要）
    /// </summary>
    public ItemSlotData()
    {
        itemId = 0;
        quantity = 0;
    }

    public ItemSlotData(int itemId, int quantity)
    {
        this.itemId = itemId;
        this.quantity = quantity;
    }

    public ItemSlotData(int itemId, int quantity, int ammo)
    {
        this.itemId = itemId;
        this.quantity = quantity;
        this.ammo = ammo;
    }

    /// <summary>
    /// 是否为空格子
    /// </summary>
    public bool IsEmpty => itemId == 0 || quantity <= 0;
}
