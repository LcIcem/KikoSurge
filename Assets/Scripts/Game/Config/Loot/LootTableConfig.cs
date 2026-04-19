using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掉落表配置
/// </summary>
[CreateAssetMenu(fileName = "LootTable", menuName = "KikoSurge/掉落/掉落表")]
public class LootTableConfig : ScriptableObject
{
    [Tooltip("掉落组（按类型分组，每组可控制最多掉落数量）")]
    public List<LootGroup> groups = new();

    [Tooltip("掉落物散落半径")]
    public float DropSpreadRadius = 1f;

    /// <summary>
    /// 掉落组
    /// </summary>
    [Serializable]
    public class LootGroup
    {
        [Tooltip("分组名称（用于显示）")]
        public string groupName = "默认";

        [Tooltip("掉落条目")]
        public List<LootEntry> entries = new();

        [Tooltip("最多掉落数量")]
        public int maxPick = 1;
    }

    /// <summary>
    /// 掉落条目
    /// </summary>
    [Serializable]
    public class LootEntry
    {
        [Tooltip("物品定义")]
        [SerializeField] private ItemConfig _itemConfig;

        public ItemConfig itemConfig => _itemConfig;

        [Tooltip("掉落概率 0~1")]
        [Range(0f, 1f)]
        public float dropChance = 0.1f;

        [Tooltip("最小数量")]
        public int minQuantity = 1;

        [Tooltip("最大数量")]
        public int maxQuantity = 1;
    }
}
