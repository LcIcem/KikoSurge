using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色静态配置数据
/// </summary>
[System.Serializable]
public class RoleStaticData
{
    [Header("角色信息")]
    [Tooltip("角色ID（用于查找和标识）")]
    public int roleId;

    [Tooltip("角色名称")]
    public string roleName;

    [Tooltip("角色头像")]
    public Sprite roleIcon;

    [Tooltip("角色预制体")]
    public GameObject prefab;

    [Header("基础属性")]
    [Tooltip("基础生命值")]
    public float baseMaxHealth = 5f;

    [Tooltip("基础攻击力")]
    public float baseAtk = 1f;

    [Tooltip("基础防御力")]
    public float baseDef = 1f;

    [Tooltip("基础移动速度")]
    public float baseMoveSpeed = 4f;

    [Header("战斗属性")]
    [Tooltip("冲刺速度")]
    public float dashSpeed = 6f;

    [Tooltip("冲刺持续时间")]
    public float dashDuration = 0.1f;

    [Tooltip("冲刺间隔时间")]
    public float dashGap = 0.2f;

    [Tooltip("无敌持续时间")]
    public float invincibleDuration = 1.0f;

    [Tooltip("受伤动画持续时间")]
    public float hurtDuration = 0.3f;

    [Header("武器配置")]
    [Tooltip("初始武器ID列表")]
    public List<int> initialWeaponIds = new();

    [Tooltip("最大可装备武器数量")]
    public int maxWeaponSlots = 2;

    [Header("背包配置")]
    [Tooltip("背包初始空格子数量")]
    public int initialEmptySlotCount = 20;

    [Tooltip("背包最大格子数量（0表示无限制）")]
    public int maxInventorySlotCount = 0;
}
