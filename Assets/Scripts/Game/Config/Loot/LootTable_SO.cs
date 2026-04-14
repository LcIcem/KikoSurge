using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掉落表配置：定义每种敌人类型的掉落物品列表
/// </summary>
[CreateAssetMenu(fileName = "LootTable_SO", menuName = "KikoSurge/掉落表/敌人掉落表")]
public class LootTable_SO : ScriptableObject
{
    [Tooltip("适用的敌人类型")]
    public EnemyType EnemyType;

    [Tooltip("掉落物品列表")]
    public List<LootItemDropEntry> Entries = new();

    [Tooltip("掉落物散落半径")]
    public float DropSpreadRadius = 1f;
}
