using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单局游戏进度数据
/// </summary>
[Serializable]
public class SessionData
{
    // Session 元数据
    public long seed;
    public long startTimestamp;
    public int selectedRoleId;
    public string selectedRoleName;

    // 当前楼层
    public int currentFloor;

    // 当前层的玩家位置
    public float playerPosX;
    public float playerPosY;

    // 背包（按类型分组）
    public List<ItemSlotData> inventoryWeaponSlots = new();
    public List<ItemSlotData> inventoryAmmoSlots = new();
    public List<ItemSlotData> inventoryPotionSlots = new();
    public List<ItemSlotData> inventoryArmorSlots = new();
    public List<ItemSlotData> inventoryRelicSlots = new();
    public List<ItemSlotData> inventoryCurrencySlots = new();

    // 已装备武器列表（最大数量由 RoleStaticData.maxWeaponSlots 决定）
    public List<ItemSlotData> equippedWeaponSlots = new();

    // 地牢状态快照
    public List<LayerSnapshot> layerSnapshots;

    /// <summary>
    /// 当前楼层的检查点数据
    /// </summary>
    public LayerSnapshot currentCheckpoint;

    /// <summary>
    /// 当前生命值（仅存储，计算后的属性由 ComputeRuntimeData 提供）
    /// </summary>
    public float currentHealth;

    /// <summary>
    /// 当前单局获得的所有修饰器（永久加成）
    /// </summary>
    public List<ModifierData> modifiers;

    public SessionData()
    {
        layerSnapshots = new List<LayerSnapshot>();
        modifiers = new List<ModifierData>();
    }

    public static SessionData CreateNew(int roleId, long seedVal)
    {
        // 从 GameDataManager 获取角色静态数据
        var roleData = GameDataManager.Instance?.GetRoleStaticData(roleId);
        string roleName = roleData?.roleName ?? roleId.ToString();
        var initialWeaponIds = roleData?.initialWeaponIds;
        int maxWeaponSlots = roleData?.maxWeaponSlots ?? 2;
        int initialEmptySlots = roleData?.initialEmptySlotCount ?? 20;
        int maxInventorySlots = roleData?.maxInventorySlotCount ?? 0;

        // 根据 maxWeaponSlots 分离已装备武器和背包武器
        var equipped = new List<ItemSlotData>();
        var inventory = new List<ItemSlotData>();

        if (initialWeaponIds != null)
        {
            for (int i = 0; i < initialWeaponIds.Count; i++)
            {
                var slot = new ItemSlotData(initialWeaponIds[i], 1);
                if (i < maxWeaponSlots)
                    equipped.Add(slot);
                else
                    inventory.Add(slot);
            }
        }

        // 创建初始空格子
        var emptyWeaponSlots = CreateEmptySlots(initialEmptySlots);
        var emptyAmmoSlots = CreateEmptySlots(initialEmptySlots);
        var emptyPotionSlots = CreateEmptySlots(initialEmptySlots);
        var emptyArmorSlots = CreateEmptySlots(initialEmptySlots);
        var emptyRelicSlots = CreateEmptySlots(initialEmptySlots);
        var emptyCurrencySlots = CreateEmptySlots(initialEmptySlots);

        return new SessionData
        {
            seed = seedVal,
            startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            selectedRoleId = roleId,
            selectedRoleName = roleName,
            currentFloor = 0,
            playerPosX = 0,
            playerPosY = 0,
            inventoryWeaponSlots = MergeInventoryAndEmpty(inventory, emptyWeaponSlots),
            inventoryAmmoSlots = emptyAmmoSlots,
            inventoryPotionSlots = emptyPotionSlots,
            inventoryArmorSlots = emptyArmorSlots,
            inventoryRelicSlots = MergeInventoryAndEmpty(new List<ItemSlotData>(), emptyRelicSlots),
            inventoryCurrencySlots = emptyCurrencySlots,
            equippedWeaponSlots = equipped,
            layerSnapshots = new List<LayerSnapshot>(),
            modifiers = new List<ModifierData>(),
            currentHealth = 0f
        };
    }

    /// <summary>
    /// 创建指定数量的空格子
    /// </summary>
    private static List<ItemSlotData> CreateEmptySlots(int count)
    {
        var slots = new List<ItemSlotData>();
        for (int i = 0; i < count; i++)
        {
            slots.Add(new ItemSlotData()); // itemId = 0 表示空格子
        }
        return slots;
    }

    /// <summary>
    /// 合并已有物品和空格子
    /// </summary>
    private static List<ItemSlotData> MergeInventoryAndEmpty(List<ItemSlotData> inventory, List<ItemSlotData> emptySlots)
    {
        var result = new List<ItemSlotData>(inventory);
        result.AddRange(emptySlots);
        return result;
    }

    /// <summary>
    /// 获取玩家位置向量
    /// </summary>
    public Vector2 GetPlayerPos()
    {
        return new Vector2(playerPosX, playerPosY);
    }

    /// <summary>
    /// 设置玩家位置
    /// </summary>
    public void SetPlayerPos(Vector2 pos)
    {
        playerPosX = pos.x;
        playerPosY = pos.y;
    }

    /// <summary>
    /// 添加修饰器
    /// </summary>
    public void AddModifier(ModifierData modifier)
    {
        modifiers.Add(modifier);
    }

    /// <summary>
    /// 移除修饰器
    /// </summary>
    public void RemoveModifier(int modifierId)
    {
        modifiers.RemoveAll(m => m.modifierId == modifierId);
    }

    /// <summary>
    /// 获取指定类型的所有修饰器
    /// </summary>
    public List<ModifierData> GetModifiersByType(ModifierType type)
    {
        return modifiers.FindAll(m => m.type == type);
    }

    /// <summary>
    /// 计算某属性的总加成值
    /// </summary>
    public float CalculateModifierBonus(ModifierType type, float baseValue)
    {
        float bonus = 0f;
        foreach (var mod in modifiers)
        {
            if (mod.type == type)
            {
                bonus += mod.value;
            }
        }
        return baseValue + bonus;
    }

    /// <summary>
    /// 保存检查点
    /// </summary>
    public void SaveCheckpoint(LayerSnapshot snapshot)
    {
        currentCheckpoint = snapshot;
    }

    /// <summary>
    /// 获取显示信息
    /// </summary>
    public string GetDisplayInfo()
    {
        return $"第{currentFloor + 1}层";
    }

    /// <summary>
    /// 清空所有数据（游戏结束时调用）
    /// </summary>
    public void Clear()
    {
        layerSnapshots?.Clear();
        modifiers?.Clear();
        currentCheckpoint = null;
        currentHealth = 0f;
    }

    /// <summary>
    /// 获取已装备武器的ID列表（用于UI显示）
    /// </summary>
    public List<int> equippedWeaponIds
    {
        get
        {
            var ids = new List<int>();
            foreach (var slot in equippedWeaponSlots)
            {
                ids.Add(slot.itemId);
            }
            return ids;
        }
    }
}
