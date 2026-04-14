using UnityEngine;

/// <summary>
/// 掉落物品类型枚举
/// </summary>
public enum LootItemType
{
    Weapon,  // 武器
    Prop,    // 道具
    Gold,    // 金币
}

/// <summary>
/// 掉落物品定义基类（抽象）
/// </summary>
public abstract class LootItemDefBase : ScriptableObject
{
    [Header("标识")]
    [Tooltip("唯一ID")]
    public string ItemId;

    [Tooltip("物品名称")]
    public string ItemName;

    [Tooltip("物品类型")]
    public LootItemType ItemType;

    [Header("掉落配置")]
    [Tooltip("图标（用于掉落物显示）")]
    public Sprite Icon;

    [Tooltip("掉落权重（数值越高掉落概率越高）")]
    public float Weight = 1f;

    [Tooltip("最小掉落数量")]
    public int MinQuantity = 1;

    [Tooltip("最大掉落数量")]
    public int MaxQuantity = 1;
}
