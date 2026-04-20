using LcIcemFramework;
using UnityEngine;
using XLua;

/// <summary>
/// C# 与 Lua 伤害公式桥接
/// </summary>
public static class DamageLuaBridge
{
    private static LuaEnv _luaEnv;
    private static LuaTable _damageModule;
    private static bool _luaLoaded = false;
    private static bool _initializing = false;

    public static bool IsLuaAvailable => _luaLoaded;

    /// <summary>
    /// 初始化 Lua 环境（异步通过 Addressables 加载）
    /// </summary>
    public static void Initialize()
    {
        if (_luaEnv != null || _initializing) return;
        _initializing = true;

        // 通过 Addressables 异步加载 Lua 脚本
        ManagerHub.Addressables.LoadAsync<TextAsset>("DamageFormula", (luaText) =>
        {
            _luaEnv = new LuaEnv();
            var results = _luaEnv.DoString(luaText.text);
            if (results != null && results.Length > 0)
            {
                _damageModule = results[0] as LuaTable;
                _luaLoaded = _damageModule != null;
            }
            _initializing = false;
            Debug.Log("[DamageLuaBridge] Lua script loaded via Addressables. Loaded: " + _luaLoaded);
        });
    }

    /// <summary>
    /// 通过 Lua 计算伤害
    /// </summary>
    public static DamageResult? CalculateViaLua(BulletDamageParams p, Vector3 worldPosition)
    {
        if (_damageModule == null || _luaEnv == null)
            return null;

        try
        {
            LuaFunction calcFunc = _damageModule.Get<LuaFunction>("CalculateDamage");

            // 将参数转换为 Lua table
            LuaTable paramTable = _luaEnv.NewTable();

            paramTable.Set("baseDamage", p.bulletBaseDamage);
            paramTable.Set("baseCritRate", p.playerCritRate);
            paramTable.Set("baseCritMultiplier", p.playerCritMultiplier);
            paramTable.Set("weaponCritRate", p.weaponCritRate);
            paramTable.Set("weaponCritMultiplier", p.weaponCritMultiplier);
            paramTable.Set("playerDamageBonusPercent", p.playerDamageBonus);
            paramTable.Set("weaponDamageBonusPercent", p.weaponDamageBonusPercent);
            paramTable.Set("weaponFlatDamage", p.weaponDamage);
            paramTable.Set("targetDefense", p.targetDefense);
            paramTable.Set("defBreak", p.playerDefBreak);

            Debug.Log($"[Lua] playerCritRate={p.playerCritRate}, weaponCritRate={p.weaponCritRate}");

            LuaTable result = calcFunc.Func<LuaTable, LuaTable>(paramTable);

            // 清理
            paramTable.Dispose();

            if (result != null)
            {
                bool isCrit = result.Get<bool>("isCrit");
                float critRate = result.Get<float>("critRate");
                Debug.Log($"[Lua] isCrit={isCrit}, critRate={critRate}");
            }

            if (result == null)
                return null;

            return new DamageResult
            {
                finalDamage = result.Get<float>("finalDamage"),
                isCrit = result.Get<bool>("isCrit"),
                critRate = result.Get<float>("critRate"),
                critMultiplier = result.Get<float>("critMultiplier"),
                source = DamageSource.PlayerBullet,
                rawDamage = p.bulletBaseDamage,
                defenseReduction = 0f,
                worldPosition = worldPosition
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DamageLuaBridge] Error calling Lua: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// 热更新重载 Lua 脚本
    /// </summary>
    public static void ReloadScript()
    {
        if (_damageModule != null)
        {
            _damageModule.Dispose();
            _damageModule = null;
        }

        _luaLoaded = false;

        // 重新加载
        ManagerHub.Addressables.LoadAsync<TextAsset>("DamageFormula", (luaText) =>
        {
            if (_luaEnv == null)
                _luaEnv = new LuaEnv();

            var results = _luaEnv.DoString(luaText.text);
            if (results != null && results.Length > 0)
            {
                _damageModule = results[0] as LuaTable;
                _luaLoaded = _damageModule != null;
            }
            Debug.Log("[DamageLuaBridge] Lua script reloaded. Loaded: " + _luaLoaded);
        });
    }

    /// <summary>
    /// 释放 Lua 环境
    /// </summary>
    public static void Dispose()
    {
        if (_damageModule != null)
        {
            _damageModule.Dispose();
            _damageModule = null;
        }

        if (_luaEnv != null)
        {
            _luaEnv.Dispose();
            _luaEnv = null;
        }

        _luaLoaded = false;
    }
}
