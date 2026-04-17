using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有掉落表的聚合配置
/// </summary>
[CreateAssetMenu(fileName = "LootTable_Registry", menuName = "KikoSurge/掉落/总配置")]
public class LootTableRegistry : ScriptableObject
{
    [Tooltip("所有敌人掉落表")]
    public List<LootTableConfig> lootTables = new();
}
