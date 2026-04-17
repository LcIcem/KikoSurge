using UnityEngine;

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Weapon,
    Prop,
    Gold
}

/// <summary>
/// Item 配置基类（静态配置）
/// 所有可掉落/可使用物品的共同属性定义
/// </summary>
public abstract class ItemConfig : ScriptableObject
{
    [Header("标识")]
    [Tooltip("唯一ID，用于字典查找")]
    public string ItemId;

    [Tooltip("物品名称")]
    public string ItemName;

    [Tooltip("物品类型")]
    public ItemType ItemType;

    [Tooltip("图标（用于掉落物显示）")]
    public Sprite Icon;
}
