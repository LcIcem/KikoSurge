using System;
using System.Collections.Generic;
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
        Log($"Session data bound. equippedWeaponSlots count={sessionData?.equippedWeaponSlots?.Count ?? -1}");
    }

    #endregion

    #region 物品查询

    /// <summary>
    /// 获取指定类型背包的所有格子数据
    /// </summary>
    public List<ItemSlotData> GetInventory(ItemType type)
    {
        var list = GetSlotList(type);
        return list ?? new List<ItemSlotData>();
    }

    /// <summary>
    /// 获取已装备武器格子列表
    /// </summary>
    public List<ItemSlotData> GetEquippedWeapons()
    {
        return _sessionData?.equippedWeaponSlots ?? new List<ItemSlotData>();
    }

    /// <summary>
    /// 获取指定物品的数量
    /// </summary>
    public int GetItemCount(ItemType type, int itemId)
    {
        if (itemId == 0) return 0;

        var slots = GetSlotList(type);
        if (slots == null) return 0;

        int total = 0;
        foreach (var slot in slots)
        {
            if (slot.itemId == itemId)
                total += slot.quantity;
        }
        return total;
    }

    /// <summary>
    /// 检查背包是否包含指定物品
    /// </summary>
    public bool ContainsItem(ItemType type, int itemId)
    {
        if (itemId == 0) return false;
        return GetItemCount(type, itemId) > 0;
    }

    #endregion

    #region 物品操作

    /// <summary>
    /// 添加物品到背包（自动堆叠或创建新格子）
    /// </summary>
    /// <param name="type">物品类型</param>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量</param>
    /// <returns>是否添加成功</returns>
    public bool AddItem(ItemType type, int itemId, int quantity = 1)
    {
        if (_sessionData == null || quantity <= 0 || itemId == 0)
            return false;

        var config = GameDataManager.Instance?.GetItemConfig(itemId);
        var slots = GetSlotList(type);
        if (slots == null)
            return false;

        int maxStack = config?.MaxStack ?? 1;
        int remaining = quantity;

        // 1. 先尝试堆叠到已有同 ID 格子
        if (remaining > 0)
        {
            foreach (var slot in slots)
            {
                if (slot.itemId == itemId && slot.quantity < maxStack)
                {
                    int canAdd = Mathf.Min(maxStack - slot.quantity, remaining);
                    slot.quantity += canAdd;
                    remaining -= canAdd;
                    if (remaining <= 0) break;
                }
            }
        }

        // 2. 如果还有剩余，找空格子进行填充
        if (remaining > 0)
        {
            foreach (var slot in slots)
            {
                if (slot.IsEmpty)
                {
                    int stackCount = Mathf.Min(remaining, maxStack);
                    slot.itemId = itemId;
                    slot.quantity = stackCount;
                    remaining -= stackCount;
                    if (remaining <= 0) break;
                }
            }
        }

        // 3. 如果还有剩余，创建新格子
        while (remaining > 0)
        {
            int stackCount = Mathf.Min(remaining, maxStack);
            slots.Add(new ItemSlotData(itemId, stackCount));
            remaining -= stackCount;
        }

        if (quantity > 0)
        {
            EventCenter.Instance.Publish(GameEventID.OnInventoryItemAdded,
                new InventoryChangeParams(type, itemId, quantity, InventoryChangeType.Add));
            EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                new InventoryChangeParams(type, itemId, quantity, InventoryChangeType.Add));
            Log($"Added {quantity} item(s) of type {type} with id {itemId}.");
        }

        return true;
    }

    /// <summary>
    /// 从背包移除物品
    /// </summary>
    /// <param name="type">物品类型</param>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveItem(ItemType type, int itemId, int quantity = 1)
    {
        if (_sessionData == null || quantity <= 0 || itemId == 0)
            return false;

        var slots = GetSlotList(type);
        if (slots == null)
            return false;

        int remaining = quantity;

        // 从后往前遍历
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = slots[i];
            if (slot.itemId == itemId)
            {
                int canRemove = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= canRemove;
                remaining -= canRemove;

                if (slot.quantity <= 0)
                    slots.RemoveAt(i);
            }
        }

        if (quantity - remaining > 0)
        {
            EventCenter.Instance.Publish(GameEventID.OnInventoryItemRemoved,
                new InventoryChangeParams(type, itemId, quantity - remaining, InventoryChangeType.Remove));
            EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                new InventoryChangeParams(type, itemId, quantity - remaining, InventoryChangeType.Remove));
            Log($"Removed {quantity - remaining} item(s) of type {type} with id {itemId}.");
        }

        return remaining <= 0;
    }

    /// <summary>
    /// 背包内部移动格子（支持堆叠）
    /// </summary>
    public void MoveSlot(ItemType type, int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        var slots = GetSlotList(type);
        if (slots == null) return;
        if (fromIndex < 0 || fromIndex >= slots.Count) return;
        if (toIndex < 0 || toIndex >= slots.Count) return;

        var fromSlot = slots[fromIndex];
        var toSlot = slots[toIndex];

        // 如果目标格子是空的，直接移动
        if (toSlot.IsEmpty)
        {
            slots[toIndex] = fromSlot;
            slots[fromIndex] = new ItemSlotData();
        }
        // 如果目标格子有相同物品，尝试堆叠
        else if (toSlot.itemId == fromSlot.itemId && !fromSlot.IsEmpty)
        {
            var config = GameDataManager.Instance?.GetItemConfig(toSlot.itemId);
            int maxStack = config?.MaxStack ?? 1;
            int totalQty = toSlot.quantity + fromSlot.quantity;

            if (totalQty <= maxStack)
            {
                // 可以合并到目标格子
                toSlot.quantity = totalQty;
                slots[fromIndex] = new ItemSlotData();
            }
            else
            {
                // 目标格子满了，目标格子填满，剩余放回原格子
                toSlot.quantity = maxStack;
                fromSlot.quantity = totalQty - maxStack;
            }
        }
        // 不同物品，交换位置
        else
        {
            (slots[fromIndex], slots[toIndex]) = (slots[toIndex], slots[fromIndex]);
        }

        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(type, 0, 0, InventoryChangeType.Move));
    }

    /// <summary>
    /// 背包与已装备武器交换（仅武器类型）
    /// </summary>
    /// <param name="inventorySlotIndex">背包格子索引</param>
    /// <param name="equippedSlotIndex">装备槽索引</param>
    /// <returns>是否交换成功</returns>
    public bool SwapWithEquipped(int inventorySlotIndex, int equippedSlotIndex)
    {
        var inventory = GetSlotList(ItemType.Weapon);
        var equipped = _sessionData?.equippedWeaponSlots;

        if (inventory == null || equipped == null)
            return false;
        if (inventorySlotIndex < 0 || inventorySlotIndex >= inventory.Count)
            return false;
        if (equippedSlotIndex < 0 || equippedSlotIndex >= equipped.Count)
            return false;

        var invSlot = inventory[inventorySlotIndex];
        var eqSlot = equipped[equippedSlotIndex];

        // 只有非空格子才能交换
        if (invSlot.IsEmpty || eqSlot.IsEmpty)
            return false;

        // 同物品 ID：尝试堆叠
        if (invSlot.itemId == eqSlot.itemId)
        {
            var config = GameDataManager.Instance?.GetItemConfig(invSlot.itemId);
            int maxStack = config?.MaxStack ?? 1;
            int totalQty = invSlot.quantity + eqSlot.quantity;

            if (totalQty <= maxStack)
            {
                // 可以合并到装备槽
                eqSlot.quantity = totalQty;
                inventory[inventorySlotIndex] = new ItemSlotData();
            }
            else
            {
                // 装备槽填满，剩余放回背包格子
                eqSlot.quantity = maxStack;
                invSlot.quantity = totalQty - maxStack;
            }
        }
        else
        {
            // 不同物品，交换数据
            inventory[inventorySlotIndex] = eqSlot;
            equipped[equippedSlotIndex] = invSlot;
        }

        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Swap));

        Log($"Swapped inventory slot {inventorySlotIndex} with equipped slot {equippedSlotIndex}");
        return true;
    }

    /// <summary>
    /// 卸下装备槽中的武器到背包
    /// </summary>
    /// <param name="equippedSlotIndex">装备槽索引</param>
    /// <param name="targetInventorySlotIndex">目标背包格子索引（用于堆叠）</param>
    /// <returns>是否卸下成功</returns>
    public bool UnequipWeapon(int equippedSlotIndex, int targetInventorySlotIndex = -1)
    {
        var equipped = _sessionData?.equippedWeaponSlots;
        var inventory = GetSlotList(ItemType.Weapon);

        if (equipped == null || inventory == null)
            return false;
        if (equippedSlotIndex < 0 || equippedSlotIndex >= equipped.Count)
            return false;

        var slot = equipped[equippedSlotIndex];
        if (slot.IsEmpty)
            return false;

        int itemId = slot.itemId;
        int quantity = slot.quantity;

        // 优先尝试堆叠到指定背包格子
        if (targetInventorySlotIndex >= 0 && targetInventorySlotIndex < inventory.Count)
        {
            var targetSlot = inventory[targetInventorySlotIndex];

            if (targetSlot.itemId == itemId && targetSlot.quantity < quantity)
            {
                var config = GameDataManager.Instance?.GetItemConfig(itemId);
                int maxStack = config?.MaxStack ?? 1;
                int totalQty = targetSlot.quantity + quantity;

                if (totalQty <= maxStack)
                {
                    // 可以合并
                    targetSlot.quantity = totalQty;
                    equipped[equippedSlotIndex] = new ItemSlotData();
                }
                else
                {
                    // 目标格子满了，填满目标，剩余放回原装备槽
                    targetSlot.quantity = maxStack;
                    slot.quantity = totalQty - maxStack;
                }

                EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                    new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Swap));

                Log($"Unequipped weapon at slot {equippedSlotIndex} to inventory slot {targetInventorySlotIndex}");
                return true;
            }
        }

        // 回退：添加到背包（会自动堆叠或创建新格子）
        bool added = AddItem(ItemType.Weapon, itemId, quantity);
        if (!added)
            return false;

        // 清空装备槽
        equipped[equippedSlotIndex] = new ItemSlotData();

        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Swap));

        Log($"Unequipped weapon at slot {equippedSlotIndex}: itemId={itemId}, quantity={quantity}");
        return true;
    }

    /// <summary>
    /// 交换两个装备槽的武器
    /// </summary>
    public void SwapEquippedSlots(int slotIndex1, int slotIndex2)
    {
        var equipped = _sessionData?.equippedWeaponSlots;
        if (equipped == null)
            return;
        if (slotIndex1 < 0 || slotIndex1 >= equipped.Count)
            return;
        if (slotIndex2 < 0 || slotIndex2 >= equipped.Count)
            return;
        if (slotIndex1 == slotIndex2)
            return;

        var temp = equipped[slotIndex1];
        equipped[slotIndex1] = equipped[slotIndex2];
        equipped[slotIndex2] = temp;

        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Swap));

        Log($"Swapped equipped slots {slotIndex1} and {slotIndex2}");
    }

    /// <summary>
    /// 将背包中的武器装备到指定装备槽
    /// </summary>
    /// <param name="inventorySlotIndex">背包格子索引</param>
    /// <param name="equippedSlotIndex">装备槽索引</param>
    /// <returns>是否装备成功</returns>
    public bool EquipFromInventory(int inventorySlotIndex, int equippedSlotIndex)
    {
        var inventory = GetSlotList(ItemType.Weapon);
        var equipped = _sessionData?.equippedWeaponSlots;

        if (inventory == null || equipped == null)
            return false;
        if (inventorySlotIndex < 0 || inventorySlotIndex >= inventory.Count)
            return false;
        if (equippedSlotIndex < 0 || equippedSlotIndex >= equipped.Count)
            return false;

        var invSlot = inventory[inventorySlotIndex];
        var eqSlot = equipped[equippedSlotIndex];

        // 装备槽为空：直接移动
        if (eqSlot.IsEmpty)
        {
            equipped[equippedSlotIndex] = invSlot;
            inventory[inventorySlotIndex] = new ItemSlotData();
        }
        // 同物品 ID：尝试堆叠
        else if (eqSlot.itemId == invSlot.itemId)
        {
            var config = GameDataManager.Instance?.GetItemConfig(invSlot.itemId);
            int maxStack = config?.MaxStack ?? 1;
            int totalQty = eqSlot.quantity + invSlot.quantity;

            if (totalQty <= maxStack)
            {
                // 可以合并到装备槽
                eqSlot.quantity = totalQty;
                inventory[inventorySlotIndex] = new ItemSlotData();
            }
            else
            {
                // 装备槽填满，剩余放回背包格子
                eqSlot.quantity = maxStack;
                invSlot.quantity = totalQty - maxStack;
            }
        }
        // 不同物品：交换
        else
        {
            inventory[inventorySlotIndex] = eqSlot;
            equipped[equippedSlotIndex] = invSlot;
        }

        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Swap));

        Log($"Equipped from inventory: invSlot={inventorySlotIndex} -> equipSlot={equippedSlotIndex}");
        return true;
    }

    /// <summary>
    /// 垃圾桶物品恢复到背包
    /// </summary>
    /// <param name="trashItemId">物品ID</param>
    /// <param name="quantity">数量</param>
    /// <returns>是否恢复成功</returns>
    public bool TrashToInventory(int trashItemId, int quantity)
    {
        if (trashItemId == 0 || quantity <= 0)
            return false;

        var config = GameDataManager.Instance?.GetItemConfig(trashItemId);
        if (config == null)
            return false;

        ItemType type = config.Type;
        var slots = GetSlotList(type);
        if (slots == null)
            return false;

        int remaining = quantity;
        int maxStack = config.MaxStack;

        // 先尝试堆叠
        foreach (var slot in slots)
        {
            if (slot.itemId == trashItemId && slot.quantity < maxStack)
            {
                int canAdd = Mathf.Min(maxStack - slot.quantity, remaining);
                slot.quantity += canAdd;
                remaining -= canAdd;
                if (remaining <= 0) break;
            }
        }

        // 如果还有剩余，创建新格子
        if (remaining > 0)
        {
            slots.Add(new ItemSlotData(trashItemId, remaining));
            remaining = 0;
        }

        EventCenter.Instance.Publish(GameEventID.OnInventoryItemAdded,
            new InventoryChangeParams(type, trashItemId, quantity, InventoryChangeType.Add));
        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(type, trashItemId, quantity, InventoryChangeType.Add));

        Log($"Trash restored: itemId={trashItemId}, quantity={quantity}");
        return true;
    }

    /// <summary>
    /// 清空指定类型背包
    /// </summary>
    public void ClearInventory(ItemType type)
    {
        var slots = GetSlotList(type);
        if (slots == null || slots.Count == 0)
            return;

        slots.Clear();
        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(type, 0, 0, InventoryChangeType.Clear));
        Log($"Cleared inventory of type {type}.");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 根据类型获取 SessionData 中对应的背包格子列表
    /// </summary>
    private List<ItemSlotData> GetSlotList(ItemType type)
    {
        if (_sessionData == null)
            return null;

        return type switch
        {
            ItemType.Weapon => _sessionData.inventoryWeaponSlots,
            ItemType.Ammo => _sessionData.inventoryAmmoSlots,
            ItemType.Potion => _sessionData.inventoryPotionSlots,
            ItemType.Armor => _sessionData.inventoryArmorSlots,
            ItemType.Relic => _sessionData.inventoryRelicSlots,
            ItemType.Currency => _sessionData.inventoryCurrencySlots,
            _ => null
        };
    }

    #endregion

    #region 日志

    private void Log(string msg) => Debug.Log($"[InventoryManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[InventoryManager] {msg}");

    #endregion
}
