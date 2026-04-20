using UnityEngine;

/// <summary>
/// 伤害计算器
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 计算子弹命中敌人造成的伤害
    /// </summary>
    public static DamageResult CalculateEnemyDamage(BulletDamageParams p, Vector3 worldPosition)
    {
        // 尝试通过 Lua 计算
        if (DamageLuaBridge.IsLuaAvailable)
        {
            DamageResult? luaResult = DamageLuaBridge.CalculateViaLua(p, worldPosition);
            if (luaResult.HasValue)
                return luaResult.Value;
        }

        // C# 原生计算（Lua 未加载时的 fallback）
        return CalculateEnemyDamageNative(p, worldPosition);
    }

    /// <summary>
    /// C# 原生伤害计算
    /// </summary>
    private static DamageResult CalculateEnemyDamageNative(BulletDamageParams p, Vector3 worldPosition)
    {
        // 1. 暴击判定
        float critRate = Mathf.Clamp01(p.playerCritRate + p.weaponCritRate);
        bool isCrit = Random.value < critRate;

        Debug.Log($"[Damage] critRate={critRate} (player={p.playerCritRate}, weapon={p.weaponCritRate}), isCrit={isCrit}");

        // 2. 伤害加成
        float damaged = p.bulletBaseDamage * (1 + p.playerDamageBonus + p.weaponDamageBonusPercent) + p.weaponDamage;
        float rawDamage = damaged;

        // 3. 暴击伤害
        float critMultiplier = isCrit ? (p.playerCritMultiplier + p.weaponCritMultiplier) : 1f;
        damaged *= critMultiplier;

        // 4. 防御减伤
        float effectiveDefense = p.targetDefense * (1 - p.playerDefBreak);
        float reduction = effectiveDefense / (effectiveDefense + 100f);
        float finalDamage = damaged * (1 - reduction);

        return new DamageResult
        {
            finalDamage = finalDamage,
            isCrit = isCrit,
            critRate = critRate,
            critMultiplier = critMultiplier,
            source = DamageSource.PlayerBullet,
            rawDamage = rawDamage,
            defenseReduction = damaged - finalDamage,
            worldPosition = worldPosition
        };
    }

    /// <summary>
    /// 计算敌人攻击玩家造成的伤害
    /// </summary>
    public static DamageResult CalculatePlayerDamage(float enemyAttack, float playerDefense, float playerDefBreak, float playerDamageReduction, Vector3 worldPosition)
    {
        float baseDamage = enemyAttack;

        // 防御减伤
        float effectiveDefense = playerDefense * (1 - playerDefBreak);
        float reduction = effectiveDefense / (effectiveDefense + 100f);
        float afterDefense = baseDamage * (1 - reduction);

        // 护甲减免
        float finalDamage = afterDefense * (1 - playerDamageReduction);

        return new DamageResult
        {
            finalDamage = finalDamage,
            isCrit = false,
            critRate = 0f,
            critMultiplier = 1f,
            source = DamageSource.EnemyAttack,
            rawDamage = baseDamage,
            defenseReduction = baseDamage - afterDefense,
            worldPosition = worldPosition
        };
    }
}
