using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework;

/// <summary>
/// Item 实例工厂：创建运行时 item 实例
/// </summary>
public class ItemFactory : Singleton<ItemFactory>
{
    protected override void Init()
    {
        // 懒初始化，无需额外操作
    }

    /// <summary>
    /// 创建场景掉落物（通过 LootEntryConfig 配置）
    /// 内部处理概率抽取和数量计算
    /// </summary>
    /// <param name="entry">掉落条目配置</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成的 LootItem 实例，如果概率未通过则返回 null</returns>
    public LootItem CreateLootItem(LootEntryConfig entry, Vector2 position)
    {
        if (entry == null || entry.lootItemPrefab == null || entry.itemConfig == null)
        {
            LogError($"LootEntryConfig 配置不完整");
            return null;
        }

        // 概率抽取
        if (Random.value > entry.dropChance)
            return null;

        // 计算掉落数量
        int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);

        // 从对象池获取
        var lootItem = ManagerHub.Pool.Get<LootItem>(
            entry.lootItemPrefab.name, position, Quaternion.identity);

        if (lootItem == null)
        {
            LogError($"LootItem 获取失败: {entry.lootItemPrefab.name}");
            return null;
        }

        // 初始化（传入 ItemConfig）
        lootItem.Initialize(entry.itemConfig, quantity);
        return lootItem;
    }

    /// <summary>
    /// 释放掉落物回对象池
    /// </summary>
    public void Release(LootItem lootItem)
    {
        if (lootItem == null) return;
        ManagerHub.Pool.Release(lootItem.gameObject);
    }

    private void Log(string msg) => Debug.Log($"[ItemFactory] {msg}");
    private void LogError(string msg) => Debug.LogError($"[ItemFactory] {msg}");
}
