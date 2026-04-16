using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单条角色数据
/// </summary>
[System.Serializable]
public class RoleInfo
{
    [Header("角色信息")]
    [Tooltip(tooltip: "角色id")]
    public int id;              // 角色id
    [Tooltip(tooltip: "角色预设体")]
    public GameObject prefabs; // 角色预设体
    [Tooltip(tooltip: "角色名")]
    public string name;        // 角色名
    [Tooltip(tooltip: "基础生命值")]
    public float health = 5;   // 基础生命值
    [Tooltip(tooltip: "基础最大生命值")]
    public float maxHealth = 5;   // 基础最大生命值
    [Tooltip(tooltip: "基础攻击力")]
    public float atk = 1;      // 基础攻击力
    [Tooltip(tooltip: "基础防御力")]
    public float def = 1;      // 基础防御力
    [Tooltip(tooltip: "基础移动速度")]
    public float moveSpeed = 4; // 基础移动速度

    [Header("武器配置")]
    [Tooltip("初始武器Id列表")]
    public List<int> initialWeaponIds = new();
    [Tooltip("最大可装备武器数量")]
    public int maxWeaponSlots = 2;

    public PlayerData ConvertToPlayerData()
    {
        var data = new PlayerData
        {
            id = id,
            name = name,
            maxHealth = maxHealth,
            Health = health,
            atk = atk,
            def = def,
            moveSpeed = moveSpeed
        };

        return data;
    }
}