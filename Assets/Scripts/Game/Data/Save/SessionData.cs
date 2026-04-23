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
    public List<ItemSlotData> inventoryPotionSlots = new();
    public List<ItemSlotData> inventoryRelicSlots = new();
    public List<ItemSlotData> inventoryCurrencySlots = new();

    // 已装备武器列表（最大数量由 RoleStaticData.maxWeaponSlots 决定）
    public List<ItemSlotData> equippedWeaponSlots = new();

    // 垃圾桶待删除物品（用于关闭背包后恢复）
    public ItemSlotData trashPendingItem;

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

    /// <summary>
    /// 当前单局激活的遗物效果列表
    /// </summary>
    public List<RelicEffect> activeRelicEffects;

    public SessionData()
    {
        layerSnapshots = new List<LayerSnapshot>();
        modifiers = new List<ModifierData>();
        activeRelicEffects = new List<RelicEffect>();
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

        // 先填充 maxWeaponSlots 个空槽位（保证装备槽位完整）
        for (int i = 0; i < maxWeaponSlots; i++)
        {
            equipped.Add(new ItemSlotData());
        }

        // 放置初始武器到对应槽位
        if (initialWeaponIds != null)
        {
            for (int i = 0; i < initialWeaponIds.Count; i++)
            {
                if (i >= maxWeaponSlots)
                {
                    // 超出槽位数量的武器放入背包
                    var weaponConfig = GameDataManager.Instance?.GetWeaponConfig(initialWeaponIds[i]);
                    int ammo = weaponConfig?.magazineSize ?? 0;
                    inventory.Add(new ItemSlotData(initialWeaponIds[i], 1, ammo));
                }
                else
                {
                    // 从武器配置获取初始弹药量
                    var weaponConfig = GameDataManager.Instance?.GetWeaponConfig(initialWeaponIds[i]);
                    int ammo = weaponConfig?.magazineSize ?? 0;
                    equipped[i] = new ItemSlotData(initialWeaponIds[i], 1, ammo);
                }
            }
        }

        // 创建初始空格子
        var emptyWeaponSlots = CreateEmptySlots(initialEmptySlots);
        var emptyPotionSlots = CreateEmptySlots(initialEmptySlots);
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
            inventoryPotionSlots = emptyPotionSlots,
            inventoryRelicSlots = MergeInventoryAndEmpty(new List<ItemSlotData>(), emptyRelicSlots),
            inventoryCurrencySlots = emptyCurrencySlots,
            equippedWeaponSlots = equipped,
            layerSnapshots = new List<LayerSnapshot>(),
            modifiers = new List<ModifierData>(),
            activeRelicEffects = new List<RelicEffect>(),
            currentHealth = float.NaN  // 使用 NaN 表示"未设置"，GetPlayerData 会自动使用 maxHealth
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
        activeRelicEffects?.Clear();
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
