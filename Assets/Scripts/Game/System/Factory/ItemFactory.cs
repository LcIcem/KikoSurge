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
    /// 创建场景掉落物
    /// </summary>
    /// <param name="itemConfig">物品配置</param>
    /// <param name="quantity">数量</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成的 LootItem 实例</returns>
    public LootItem CreateLootItem(ItemConfig itemConfig, int quantity, Vector2 position)
    {
        if (itemConfig == null)
        {
            LogError($"ItemConfig 为 null");
            return null;
        }

        // 从对象池获取（假设 LootItem prefab 名称为 "LootItem"）
        var lootItem = ManagerHub.Pool.Get<LootItem>("LootItem", position, Quaternion.identity);

        if (lootItem == null)
        {
            LogError($"LootItem 获取失败");
            return null;
        }

        lootItem.Initialize(itemConfig, quantity);
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
