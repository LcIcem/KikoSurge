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

        // session 绑定后，重新计算所有遗物 modifiers（加载存档或开始新游戏时都生效）
        RefreshAllRelicModifiers();
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
        return AddItemToSlot(type, itemId, quantity, allowStack: true);
    }

    /// <summary>
    /// 添加物品到空格子（不堆叠，用于拆分等场景）
    /// </summary>
    /// <param name="type">物品类型</param>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量</param>
    /// <returns>是否添加成功</returns>
    public bool AddItemToEmptySlot(ItemType type, int itemId, int quantity = 1)
    {
        Debug.Log($"[InventoryManager.AddItemToEmptySlot] type={type}, itemId={itemId}, quantity={quantity}");
        return AddItemToSlot(type, itemId, quantity, allowStack: false);
    }

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <param name="type">物品类型</param>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量</param>
    /// <param name="allowStack">是否允许堆叠</param>
    /// <returns>是否添加成功</returns>
    private bool AddItemToSlot(ItemType type, int itemId, int quantity, bool allowStack)
    {
        if (_sessionData == null || quantity <= 0 || itemId == 0)
            return false;

        var config = GameDataManager.Instance?.GetItemConfig(itemId);
        var slots = GetSlotList(type);
        if (slots == null)
            return false;

        int maxStack = config?.MaxStack ?? 1;
        int remaining = quantity;

        // 1. 先尝试堆叠到已有同 ID 格子（仅当允许堆叠时）
        if (remaining > 0 && allowStack)
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
                    // 武器槽需要设置初始弹药
                    if (type == ItemType.Weapon)
                        slot.ammo = GameDataManager.Instance?.GetWeaponConfig(itemId)?.magazineSize ?? 0;
                    remaining -= stackCount;
                    if (remaining <= 0) break;
                }
            }
        }

        // 3. 如果还有剩余，创建新格子
        while (remaining > 0)
        {
            int stackCount = Mathf.Min(remaining, maxStack);
            // 武器槽需要设置初始弹药
            int ammo = (type == ItemType.Weapon) ? (GameDataManager.Instance?.GetWeaponConfig(itemId)?.magazineSize ?? 0) : 0;
            slots.Add(new ItemSlotData(itemId, stackCount, ammo));
            remaining -= stackCount;
        }

        if (quantity > 0)
        {
            // 遗物在背包中就自动生效，重新计算所有遗物 modifiers
            if (type == ItemType.Relic)
            {
                RefreshAllRelicModifiers();
            }

            EventCenter.Instance.Publish(GameEventID.OnInventoryItemAdded,
                new InventoryChangeParams(type, itemId, quantity, InventoryChangeType.Add));
            EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                new InventoryChangeParams(type, itemId, quantity, InventoryChangeType.Add));
            Log($"Added {quantity} item(s) of type {type} with id {itemId} (allowStack={allowStack}).");
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
        Debug.Log($"[InventoryManager.RemoveItem] type={type}, itemId={itemId}, quantity={quantity}");

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
            // 遗物移除时重新计算所有遗物 modifiers
            if (type == ItemType.Relic)
            {
                RefreshAllRelicModifiers();
            }

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
    /// <param name="targetInventorySlotIndex">目标背包格子索引（-1 表示不指定，自动寻找空格子）</param>
    /// <returns>是否卸下成功</returns>
    public bool UnequipWeapon(int equippedSlotIndex, int targetInventorySlotIndex = -1)
    {
        Debug.Log($"[UnequipWeapon] called: equippedSlotIndex={equippedSlotIndex}, targetInventorySlotIndex={targetInventorySlotIndex}");

        var equipped = _sessionData?.equippedWeaponSlots;
        var inventory = GetSlotList(ItemType.Weapon);

        Debug.Log($"[UnequipWeapon] equipped.Count={equipped?.Count ?? -1}, inventory.Count={inventory?.Count ?? -1}");

        if (equipped == null || inventory == null)
        {
            Debug.Log("[UnequipWeapon] FAIL: equipped or inventory is null");
            return false;
        }
        if (equippedSlotIndex < 0 || equippedSlotIndex >= equipped.Count)
        {
            Debug.Log($"[UnequipWeapon] FAIL: equippedSlotIndex out of range");
            return false;
        }

        var slot = equipped[equippedSlotIndex];
        Debug.Log($"[UnequipWeapon] slot.itemId={slot.itemId}, slot.quantity={slot.quantity}, IsEmpty={slot.IsEmpty}");
        if (slot.IsEmpty)
        {
            Debug.Log("[UnequipWeapon] FAIL: slot is empty");
            return false;
        }

        int itemId = slot.itemId;
        int quantity = slot.quantity;

        // 指定了目标格子
        if (targetInventorySlotIndex >= 0 && targetInventorySlotIndex < inventory.Count)
        {
            var targetSlot = inventory[targetInventorySlotIndex];
            Debug.Log($"[UnequipWeapon] targetSlot itemId={targetSlot.itemId}, IsEmpty={targetSlot.IsEmpty}");

            // 目标格子是空的：直接移动
            if (targetSlot.IsEmpty)
            {
                inventory[targetInventorySlotIndex] = new ItemSlotData(itemId, quantity, slot.ammo);
                equipped[equippedSlotIndex] = new ItemSlotData();

                EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                    new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Swap));

                Log($"Unequipped weapon at slot {equippedSlotIndex} to empty inventory slot {targetInventorySlotIndex}");
                return true;
            }

            // 目标格子有物品：交换（不堆叠）
            // 武器只能交换，不能堆叠
            inventory[targetInventorySlotIndex] = slot;
            equipped[equippedSlotIndex] = targetSlot;

            EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Swap));

            Log($"Unequipped weapon at slot {equippedSlotIndex} swapped with inventory slot {targetInventorySlotIndex}");
            return true;
        }

        // 未指定目标格子：自动寻找空格子添加
        Debug.Log("[UnequipWeapon] No target specified, using AddItem");
        bool added = AddItem(ItemType.Weapon, itemId, quantity);
        if (!added)
        {
            Debug.Log("[UnequipWeapon] FAIL: AddItem returned false");
            return false;
        }

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
        // 装备槽有物品：交换（武器不堆叠，直接整个交换）
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

    /// <summary>
    /// 拆分物品（不触发合并，直接在原位置减少数量 + 新格子放置剩余）
    /// </summary>
    /// <param name="slotIndex">原格子索引</param>
    /// <param name="type">物品类型</param>
    /// <param name="itemId">物品ID</param>
    /// <param name="originalQuantity">原始数量</param>
    /// <param name="splitQuantity">要拆分出去的数量</param>
    public void SplitItem(int slotIndex, ItemType type, int itemId, int originalQuantity, int splitQuantity)
    {
        if (slotIndex < 0 || originalQuantity <= 0 || splitQuantity <= 0)
            return;

        var slots = GetSlotList(type);
        if (slots == null || slotIndex >= slots.Count)
            return;

        int remainQuantity = originalQuantity - splitQuantity;

        // 获取武器初始弹药（用于拆分后新格子的 ammo）
        int weaponAmmo = (type == ItemType.Weapon) ? (GameDataManager.Instance?.GetWeaponConfig(itemId)?.magazineSize ?? 0) : 0;

        // 原格子保留剩余数量
        slots[slotIndex] = new ItemSlotData(itemId, remainQuantity, weaponAmmo);

        // 找一个空格子放置拆分出的数量
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == slotIndex)
                continue;
            if (slots[i].IsEmpty)
            {
                slots[i] = new ItemSlotData(itemId, splitQuantity, weaponAmmo);
                Log($"SplitItem: slot[{slotIndex}]={remainQuantity}, slot[{i}]={splitQuantity}");
                EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                    new InventoryChangeParams(type, itemId, originalQuantity, InventoryChangeType.Move));
                return;
            }
        }

        // 没有空格子，创建新格子
        slots.Add(new ItemSlotData(itemId, splitQuantity, weaponAmmo));
        Log($"SplitItem: slot[{slotIndex}]={remainQuantity}, newSlot={splitQuantity}");
        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(type, itemId, originalQuantity, InventoryChangeType.Move));
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 重新计算所有遗物的 modifiers（添加/移除遗物时调用）
    /// </summary>
    private void RefreshAllRelicModifiers()
    {
        if (_sessionData == null)
            return;

        var playerData = SessionManager.Instance?.GetPlayerData();
        float oldMaxHealth = playerData?.maxHealth ?? 0f;
        float oldHealth = playerData?.Health ?? 0f;

        // 清空旧的 relic modifiers（只清空 sourceId > 0 的，通过 modifierId 关联遗物）
        _sessionData.modifiers.RemoveAll(m => m.modifierId > 0);
        _sessionData.activeRelicEffects.Clear();

        // 遍历背包里所有遗物，重新应用
        var relicSlots = _sessionData.inventoryRelicSlots;
        if (relicSlots != null)
        {
            foreach (var slot in relicSlots)
            {
                if (slot.IsEmpty)
                    continue;

                var config = GameDataManager.Instance?.GetItemConfig(slot.itemId) as RelicConfig;
                if (config == null)
                    continue;

                // 应用 modifiers
                if (config.modifiers != null)
                {
                    foreach (var mod in config.modifiers)
                    {
                        if (mod.value != 0f)
                        {
                            var modifierData = new ModifierData(
                                slot.itemId,
                                config.Name ?? $"Relic_{slot.itemId}",
                                mod.type,
                                mod.value
                            );
                            _sessionData.modifiers.Add(modifierData);
                        }
                    }
                }

                // 应用效果
                if (config.effects != null)
                {
                    foreach (var effect in config.effects)
                    {
                        if (effect != null)
                        {
                            _sessionData.activeRelicEffects.Add(effect);
                        }
                    }
                }
            }
        }

        // 计算新的 maxHealth 和血量
        var newPlayerData = SessionManager.Instance?.GetPlayerData();
        if (newPlayerData != null)
        {
            float newMaxHealth = newPlayerData.maxHealth;
            float newHealth;

            if (oldMaxHealth > 0f && newMaxHealth != oldMaxHealth)
            {
                // 保持血量比例（防止 oldMaxHealth 为 0 时除法得到 NaN）
                float healthRatio = oldHealth / oldMaxHealth;
                if (float.IsNaN(healthRatio) || float.IsInfinity(healthRatio))
                {
                    newHealth = oldHealth;
                }
                else
                {
                    newHealth = newMaxHealth * healthRatio;
                }
                newHealth = Mathf.Clamp(newHealth, 0f, newMaxHealth);
            }
            else
            {
                newHealth = oldHealth;
            }

            // 设置新血量（使用新的 maxHealth clamp）
            SessionManager.Instance?.SetPlayerHealth(newHealth);

            // 同步 Player._playerData（与 HeartSystem 使用同一份数据）
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var player = playerGo?.GetComponent<Player>();
            if (player != null)
            {
                player.RuntimeData.maxHealth = newMaxHealth;
                player.RuntimeData.Health = newHealth;
            }

            // 通知血条 UI 刷新（使用 Player._playerData 引用）
            if (player != null)
            {
                EventCenter.Instance.Publish(GameEventID.UpdateHeartDisplay, player.RuntimeData);
            }
            Debug.Log($"[RefreshAllRelicModifiers] maxHealth: {oldMaxHealth:F0} -> {newMaxHealth:F0}, health: {oldHealth:F0} -> {newHealth:F0}");
        }
    }

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
            ItemType.Potion => _sessionData.inventoryPotionSlots,
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
