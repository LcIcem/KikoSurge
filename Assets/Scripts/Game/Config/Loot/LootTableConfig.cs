using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掉落表配置：定义每个敌人ID的掉落物品列表
/// </summary>
[CreateAssetMenu(fileName = "LootTable", menuName = "KikoSurge/掉落/敌人掉落表")]
public class LootTableConfig : ScriptableObject
{
    [Tooltip("对应的敌人ID")]
    public int EnemyId;

    [Tooltip("掉落物品列表")]
    public List<LootEntryConfig> Entries = new();

    [Tooltip("掉落物散落半径")]
    public float DropSpreadRadius = 1f;
}
