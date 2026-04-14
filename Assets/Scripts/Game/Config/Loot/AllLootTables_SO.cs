using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有掉落表的聚合配置
/// </summary>
[CreateAssetMenu(fileName = "AllLootTables_SO", menuName = "KikoSurge/掉落表/所有掉落表集中配置")]
public class AllLootTables_SO : ScriptableObject
{
    [Tooltip("所有敌人掉落表")]
    public List<LootTable_SO> lootTables = new();
}
