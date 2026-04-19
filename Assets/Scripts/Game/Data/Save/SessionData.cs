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
    public List<int> inventoryWeaponIds = new();
    public List<int> inventoryAmmoIds = new();
    public List<int> inventoryPotionIds = new();
    public List<int> inventoryArmorIds = new();
    public List<int> inventoryRelicIds = new();
    public List<int> inventoryCurrencyIds = new();

    // 已装备武器列表（最大数量由 RoleStaticData.maxWeaponSlots 决定）
    public List<int> equippedWeaponIds = new();

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

        // 根据 maxWeaponSlots 分离已装备武器和背包武器
        var equipped = new List<int>();
        var inventory = new List<int>();

        if (initialWeaponIds != null)
        {
            for (int i = 0; i < initialWeaponIds.Count; i++)
            {
                if (i < maxWeaponSlots)
                    equipped.Add(initialWeaponIds[i]);
                else
                    inventory.Add(initialWeaponIds[i]);
            }
        }

        return new SessionData
        {
            seed = seedVal,
            startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            selectedRoleId = roleId,
            selectedRoleName = roleName,
            currentFloor = 0,
            playerPosX = 0,
            playerPosY = 0,
            inventoryWeaponIds = inventory,
            inventoryRelicIds = new List<int>(),
            equippedWeaponIds = equipped,
            layerSnapshots = new List<LayerSnapshot>(),
            modifiers = new List<ModifierData>(),
            currentHealth = 0f
        };
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
}
