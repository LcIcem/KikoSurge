using UnityEngine;

/// <summary>
/// 掉落条目配置（独立 SO）
/// 定义单个物品的掉落概率和数量范围
/// </summary>
[CreateAssetMenu(fileName = "Loot_Entry", menuName = "KikoSurge/掉落/掉落条目")]
public class LootEntryConfig : ScriptableObject
{
    [Tooltip("掉落物预设体（纯视觉）")]
    public LootItem lootItemPrefab;

    [Tooltip("物品定义")]
    public ItemConfig itemConfig;

    [Tooltip("掉落概率 0~1")]
    [Range(0f, 1f)]
    public float dropChance = 0.1f;

    [Tooltip("最小数量")]
    public int minQuantity = 1;

    [Tooltip("最大数量")]
    public int maxQuantity = 1;
}
