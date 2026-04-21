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
            Debug.Log("[DamageCalculator] 使用 Lua 计算伤害");
            DamageResult? luaResult = DamageLuaBridge.CalculateViaLua(p, worldPosition);
            if (luaResult.HasValue)
                return luaResult.Value;
        }

        Debug.Log("[DamageCalculator] 使用 C# 原生计算伤害");
        // C# 原生计算（Lua 未加载时的 fallback）
        return CalculateEnemyDamageNative(p, worldPosition);
    }

    /// <summary>
    /// C# 原生伤害计算
    /// </summary>
    private static DamageResult CalculateEnemyDamageNative(BulletDamageParams p, Vector3 worldPosition)
    {
        // 打印所有输入参数
        Debug.Log($"========== 伤害计算 ==========");
        Debug.Log($"[输入] bulletBaseDamage={p.bulletBaseDamage}");
        Debug.Log($"[输入] playerAtk={p.playerAtk}, playerCritRate={p.playerCritRate}, playerCritMultiplier={p.playerCritMultiplier}");
        Debug.Log($"[输入] playerDamageBonus={p.playerDamageBonus}, playerDefBreak={p.playerDefBreak}");
        Debug.Log($"[输入] weaponDamage={p.weaponDamage}, weaponCritRate={p.weaponCritRate}, weaponCritMultiplier={p.weaponCritMultiplier}");
        Debug.Log($"[输入] weaponDamageBonus={p.weaponDamageBonus}");
        Debug.Log($"[输入] targetDefense={p.targetDefense}");

        // 1. 暴击判定
        float critRate = Mathf.Clamp01(p.playerCritRate + p.weaponCritRate);
        bool isCrit = Random.value < critRate;

        Debug.Log($"[暴击] critRate={critRate} (player={p.playerCritRate} + weapon={p.weaponCritRate}), roll={Random.value}, isCrit={isCrit}");

        // 2. 伤害加成
        float beforeBonus = p.bulletBaseDamage;
        float afterBonus = (1 + p.playerDamageBonus + p.weaponDamageBonus);
        float damaged = beforeBonus * afterBonus + p.weaponDamage;
        float rawDamage = damaged;

        Debug.Log($"[伤害加成] {beforeBonus} * {afterBonus} + {p.weaponDamage} = {damaged}");

        // 3. 暴击伤害
        float critMultiplier = isCrit ? (p.playerCritMultiplier + p.weaponCritMultiplier) : 1f;
        float beforeCrit = damaged;
        damaged *= critMultiplier;

        Debug.Log($"[暴击伤害] {beforeCrit} * {critMultiplier} = {damaged}");

        // 4. 防御减伤
        float effectiveDefense = p.targetDefense * (1 - p.playerDefBreak);
        float reduction = effectiveDefense / (effectiveDefense + 100f);
        float beforeDefense = damaged;
        float finalDamage = damaged * (1 - reduction);

        Debug.Log($"[防御减伤] effectiveDefense={effectiveDefense} (def={p.targetDefense}, defBreak={p.playerDefBreak})");
        Debug.Log($"[防御减伤] reduction={reduction} ({effectiveDefense}/({effectiveDefense}+100))");
        Debug.Log($"[防御减伤] {beforeDefense} * (1 - {reduction}) = {finalDamage}");
        Debug.Log($"========== 最终结果 ==========");
        Debug.Log($"finalDamage={finalDamage}, isCrit={isCrit}, rawDamage={rawDamage}");

        return new DamageResult
        {
            finalDamage = finalDamage,
            isCrit = isCrit,
            critRate = critRate,
            critMultiplier = critMultiplier,
            source = DamageSource.PlayerBullet,
            rawDamage = rawDamage,
            defenseReduction = beforeDefense - finalDamage,
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
