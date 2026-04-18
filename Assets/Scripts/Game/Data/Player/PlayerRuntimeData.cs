using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家运行时数据
/// <para>存储计算后的最终属性：RoleStaticData + PlayerMetaData(全局加成) + SessionData.modifiers</para>
/// <para>由 PlayerHandler 在创建玩家时通过 ComputeRuntimeData() 生成</para>
/// </summary>
[Serializable]
public class PlayerRuntimeData
{
    // 角色标识
    public int id;
    public string name;

    // ========== 运行时计算的最终属性 ==========
    public float maxHealth;     // 最终最大生命值
    public float atk;           // 最终攻击力
    public float def;           // 最终防御力
    public float moveSpeed;     // 最终移动速度
    public float dashSpeed;     // 最终冲刺速度
    public float dashDuration;   // 最终冲刺持续时间
    public float dashGap;       // 最终冲刺间隔
    public float invincibleDuration; // 最终无敌持续时间
    public float hurtDuration;   // 最终受伤动画持续时间

    // 当前生命值（单独存储，因为会频繁变化）
    private float _health = 5f;

    public float Health
    {
        get => _health;
        set => _health = Mathf.Clamp(value, 0, maxHealth);
    }

    /// <summary>
    /// 是否已死亡
    /// </summary>
    public bool IsDead => _health <= 0f;

    /// <summary>
    /// 从静态数据 + 全局加成 + 单局修饰器计算运行时数据
    /// </summary>
    /// <param name="staticData">角色静态数据</param>
    /// <param name="metaData">全局进度数据（全局加成）</param>
    /// <param name="modifiers">单局获得的修饰器列表</param>
    /// <param name="currentHealth">当前生命值（不传则默认满血）</param>
    /// <returns>计算后的运行时数据</returns>
    public static PlayerRuntimeData ComputeRuntimeData(
        RoleStaticData staticData,
        PlayerMetaData metaData,
        List<ModifierData> modifiers,
        float? currentHealth = null)
    {
        if (staticData == null)
        {
            Debug.LogError("[PlayerRuntimeData] staticData is null");
            return new PlayerRuntimeData();
        }

        float globalHealthBonus = metaData?.globalMaxHealthBonus ?? 0f;
        float globalAtkBonus = metaData?.globalAtkBonus ?? 0f;
        float globalDefBonus = metaData?.globalDefBonus ?? 0f;

        // 计算基础加成后的值
        float baseMaxHealth = staticData.baseMaxHealth + globalHealthBonus;
        float baseAtk = staticData.baseAtk + globalAtkBonus;
        float baseDef = staticData.baseDef + globalDefBonus;

        // 应用单局修饰器加成
        float finalMaxHealth = ApplyModifiers(baseMaxHealth, ModifierType.MaxHealth, modifiers);
        float finalAtk = ApplyModifiers(baseAtk, ModifierType.Attack, modifiers);
        float finalDef = ApplyModifiers(baseDef, ModifierType.Defense, modifiers);
        float finalMoveSpeed = ApplyModifiers(staticData.baseMoveSpeed, ModifierType.MoveSpeed, modifiers);
        float finalDashSpeed = ApplyModifiers(staticData.dashSpeed, ModifierType.DashSpeed, modifiers);
        float finalDashDuration = ApplyModifiers(staticData.dashDuration, ModifierType.DashDuration, modifiers);
        float finalInvincibleDuration = ApplyModifiers(staticData.invincibleDuration, ModifierType.InvincibleDuration, modifiers);
        float finalHurtDuration = ApplyModifiers(staticData.hurtDuration, ModifierType.HurtDuration, modifiers);

        // 如果没有传入当前生命值或值无效，默认满血
        float health = (currentHealth == null || currentHealth <= 0) ? finalMaxHealth : currentHealth.Value;

        return new PlayerRuntimeData
        {
            id = staticData.roleId,
            name = staticData.roleName,
            maxHealth = finalMaxHealth,
            atk = finalAtk,
            def = finalDef,
            moveSpeed = finalMoveSpeed,
            dashSpeed = finalDashSpeed,
            dashDuration = finalDashDuration,
            dashGap = staticData.dashGap, // 冲刺间隔通常不需要加成
            invincibleDuration = finalInvincibleDuration,
            hurtDuration = finalHurtDuration,
            _health = health
        };
    }

    /// <summary>
    /// 应用修饰器加成
    /// </summary>
    private static float ApplyModifiers(float baseValue, ModifierType type, List<ModifierData> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
            return baseValue;

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
    /// 创建仅含基础数据的运行时数据（无加成，用于某些不需要加成的场景）
    /// </summary>
    public static PlayerRuntimeData CreateBasic(RoleStaticData staticData)
    {
        if (staticData == null)
        {
            Debug.LogError("[PlayerRuntimeData] staticData is null");
            return new PlayerRuntimeData();
        }

        return new PlayerRuntimeData
        {
            id = staticData.roleId,
            name = staticData.roleName,
            maxHealth = staticData.baseMaxHealth,
            atk = staticData.baseAtk,
            def = staticData.baseDef,
            moveSpeed = staticData.baseMoveSpeed,
            dashSpeed = staticData.dashSpeed,
            dashDuration = staticData.dashDuration,
            dashGap = staticData.dashGap,
            invincibleDuration = staticData.invincibleDuration,
            hurtDuration = staticData.hurtDuration,
            _health = staticData.baseMaxHealth
        };
    }
}
