using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LcIcemFramework.Core;
using Game.Event;

/// <summary>
/// 背包管理器
/// <para>统一管理所有类型背包的数据，提供物品操作接口，与 SessionData 同步</para>
/// </summary>
public class InventoryManager : SingletonMono<InventoryManager>
{
    #region 字段

    private SessionData _sessionData;

    #endregion

    #region 初始化

    protected override void Init()
    {
        _sessionData = null;
        Log("InventoryManager initialized.");
    }

    /// <summary>
    /// 绑定当前 Session 数据（由 SessionManager 在 StartSession/LoadSession 时调用）
    /// </summary>
    public void BindSessionData(SessionData sessionData)
    {
        _sessionData = sessionData;
        Log("Session data bound.");
    }

    #endregion

    #region 物品查询

    /// <summary>
    /// 获取指定类型背包的所有物品ID列表
    /// </summary>
    public List<int> GetInventory(ItemType type)
    {
        var list = GetInventoryList(type);
        return list != null ? new List<int>(list) : new List<int>();
    }

    /// <summary>
    /// 获取指定物品的数量
    /// </summary>
    public int GetItemCount(ItemType type, int itemId)
    {
        var list = GetInventoryList(type);
        return list?.Count(id => id == itemId) ?? 0;
    }

    /// <summary>
    /// 检查背包是否包含指定物品
    /// </summary>
    public bool ContainsItem(ItemType type, int itemId)
    {
        return GetItemCount(type, itemId) > 0;
    }

    #endregion

    #region 物品操作

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <param name="type">物品类型</param>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量（默认1）</param>
    /// <returns>实际添加的数量</returns>
    public int AddItem(ItemType type, int itemId, int quantity = 1)
    {
        if (_sessionData == null || quantity <= 0)
            return 0;

        var config = GameDataManager.Instance?.GetItemConfig(itemId);
        var list = GetInventoryList(type);
        if (list == null)
            return 0;

        // 检查堆叠限制
        int currentCount = GetItemCount(type, itemId);
        int maxStack = config?.MaxStack ?? 1;
        int canAdd = Mathf.Min(quantity, maxStack - currentCount);

        for (int i = 0; i < canAdd; i++)
        {
            list.Add(itemId);
        }

        if (canAdd > 0)
        {
            EventCenter.Instance.Publish(GameEventID.OnInventoryItemAdded,
                new InventoryChangeParams(type, itemId, canAdd, InventoryChangeType.Add));
            EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                new InventoryChangeParams(type, itemId, canAdd, InventoryChangeType.Add));
            Log($"Added {canAdd} item(s) of type {type} with id {itemId}.");
        }

        return canAdd;
    }

    /// <summary>
    /// 从背包移除物品
    /// </summary>
    /// <param name="type">物品类型</param>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量（默认1）</param>
    /// <returns>实际移除的数量</returns>
    public int RemoveItem(ItemType type, int itemId, int quantity = 1)
    {
        if (_sessionData == null || quantity <= 0)
            return 0;

        var list = GetInventoryList(type);
        if (list == null)
            return 0;

        int removed = 0;
        for (int i = 0; i < quantity && list.Remove(itemId); i++)
        {
            removed++;
        }

        if (removed > 0)
        {
            EventCenter.Instance.Publish(GameEventID.OnInventoryItemRemoved,
                new InventoryChangeParams(type, itemId, removed, InventoryChangeType.Remove));
            EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                new InventoryChangeParams(type, itemId, removed, InventoryChangeType.Remove));
            Log($"Removed {removed} item(s) of type {type} with id {itemId}.");
        }

        return removed;
    }

    /// <summary>
    /// 清空指定类型背包
    /// </summary>
    public void ClearInventory(ItemType type)
    {
        var list = GetInventoryList(type);
        if (list == null || list.Count == 0)
            return;

        list.Clear();
        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(type, 0, 0, InventoryChangeType.Clear));
        Log($"Cleared inventory of type {type}.");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 根据类型获取 SessionData 中对应的背包列表
    /// </summary>
    private List<int> GetInventoryList(ItemType type)
    {
        if (_sessionData == null)
            return null;

        return type switch
        {
            ItemType.Weapon => _sessionData.inventoryWeaponIds,
            ItemType.Ammo => _sessionData.inventoryAmmoIds,
            ItemType.Potion => _sessionData.inventoryPotionIds,
            ItemType.Armor => _sessionData.inventoryArmorIds,
            ItemType.Relic => _sessionData.inventoryRelicIds,
            ItemType.Currency => _sessionData.inventoryCurrencyIds,
            ItemType.Misc => _sessionData.inventoryMiscIds,
            _ => null
        };
    }

    #endregion

    #region 日志

    private void Log(string msg) => Debug.Log($"[InventoryManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[InventoryManager] {msg}");

    #endregion
}
