using UnityEngine;

/// <summary>
/// 掉落表条目：定义单个物品的掉落概率和数量范围
/// </summary>
[System.Serializable]
public class LootItemDropEntry
{
    [Tooltip("物品定义")]
    public LootItemDefBase ItemDef;

    [Tooltip("掉落概率（0-1），1=100%掉落")]
    [Range(0f, 1f)]
    public float DropChance = 0.1f;

    [Tooltip("最少掉落数量")]
    public int MinQuantity = 1;

    [Tooltip("最多掉落数量")]
    public int MaxQuantity = 1;
}
