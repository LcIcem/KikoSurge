using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有掉落物品定义的聚合配置
/// </summary>
[CreateAssetMenu(fileName = "AllLootItems_SO", menuName = "KikoSurge/掉落物品/所有掉落物品集中配置")]
public class AllLootItems_SO : ScriptableObject
{
    [Tooltip("所有可掉落的物品配置")]
    public List<LootItemDefBase> lootItemDefs;
}