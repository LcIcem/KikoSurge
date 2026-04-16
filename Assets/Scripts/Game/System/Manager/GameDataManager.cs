using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework.Data;
using LcIcemFramework.Managers;
using LcIcemFramework.Util.Data;
using Unity.VisualScripting;

/// <summary>
/// 游戏数据管理器
/// </summary>
public class GameDataManager : SingletonMono<GameDataManager>
{
    // 角色信息相关
    private RoleInfo_SO _roleInfo_SO;
    public bool IsRoleInfoLoaded { get; private set; } // 角色信息是否加载成功
    public int CurSelRoleIndex { get; set; } = 0;   // 当前选择的角色索引
    // 玩家数据相关
    public PlayerData PlayerData { get; set; }

    // 武器配置字典：Key = WeaponName, Value = 配置SO
    private Dictionary<string, GunConfig> _weaponConfigDict = new();
    public Dictionary<string, GunConfig> WeaponConfigDict => _weaponConfigDict;

    // 敌人配置字典：Key = EnemyId, Value = 配置SO
    private Dictionary<int, EnemyDefBase> _enemyConfigDict = new();
    public Dictionary<int, EnemyDefBase> EnemyConfigDict => _enemyConfigDict;

    // 掉落表字典：Key = EnemyType, Value = LootTable_SO
    private Dictionary<EnemyType, LootTable_SO> _lootTableDict = new();
    public Dictionary<EnemyType, LootTable_SO> LootTableDict => _lootTableDict;

    // 设置数据
    private SettingsData _settingsData;
    public SettingsData SettingsData => _settingsData;
    private string _settingsFilePath => Path.Combine(Application.persistentDataPath, "settings.json");


    protected override void Init()
    {
        // 加载角色信息
        ManagerHub.Addressables.LoadAsync<RoleInfo_SO>("RoleInfo_SO", OnRoleInfoLoaded);

        // 加载武器配置（统一SO）
        ManagerHub.Addressables.LoadAsync<AllWeaponDefs_SO>("AllWeaponDefs_SO", OnAllWeaponDefsLoaded);

        // 加载敌人配置（统一SO）
        ManagerHub.Addressables.LoadAsync<AllEnemyDefs_SO>("AllEnemyDefs_SO", OnAllEnemyDefsLoaded);

        // 加载掉落表配置（统一SO）
        ManagerHub.Addressables.LoadAsync<AllLootTables_SO>("AllLootTables_SO", OnAllLootTablesLoaded);
    }

    private void Start() {
        LoadSettings();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        // 退订事件
    }

    #region 处理事件

    #endregion

    #region RoleInfo数据相关
    // 角色信息数据 加载完毕 回调
    private void OnRoleInfoLoaded(RoleInfo_SO so)
    {
        if (so != null)
        {
            _roleInfo_SO = so;
            IsRoleInfoLoaded = true;
            Log("RoleInfo 加载完成");
        }
        else
        {
            LogError("RoleInfo 加载失败");
        }
    }

    // 获取角色信息
    public RoleInfo_SO GetRoleInfo()
    {
        if (!IsRoleInfoLoaded)
            LogWarning("RoleInfo 尚未加载完成");
        return _roleInfo_SO;
    }

    // 获取当前选择角色信息
    public RoleInfo GetRoleDataByCurSel()
    {
        if (!IsRoleInfoLoaded)
            LogWarning("RoleInfo 尚未加载完成");
        return _roleInfo_SO.roleInfos[CurSelRoleIndex];
    }
    #endregion

    #region PlayerData数据相关
    // 设置玩家数据
    public bool SetPlayerData(PlayerData p)
    {
        if (p == null)
            return false;

        PlayerData = p;
        return true;
    }

    // 获取玩家数据
    public PlayerData GetPlayerData()
    {
        if (PlayerData == null)
            return default;
        return PlayerData;
    }

    #endregion

    #region WeaponData武器配置相关

    /// <summary>
    /// 所有武器配置加载完毕回调
    /// </summary>
    private void OnAllWeaponDefsLoaded(AllWeaponDefs_SO so)
    {
        if (so == null || so.weaponConfigs == null)
        {
            LogError("武器配置加载失败");
            return;
        }

        foreach (var config in so.weaponConfigs)
        {
            if (config == null) continue;

            // 使用 Config 自己的 ID 或者名字做 key
            string key = config.gunName;
            if (_weaponConfigDict.ContainsKey(key))
            {
                LogWarning($"武器配置重复: {key}");
                continue;
            }

            _weaponConfigDict[key] = config;
            Log($"武器配置加载完成: Name={config.gunName}");
        }
    }

    /// <summary>
    /// 根据武器名称获取武器配置
    /// </summary>
    public GunConfig GetWeaponConfig(string weaponName)
    {
        if (_weaponConfigDict.TryGetValue(weaponName, out var config))
            return config;
        LogWarning($"未找到武器配置: {weaponName}");
        return null;
    }

    #endregion

    #region EnemyData敌人配置相关

    /// <summary>
    /// 所有敌人配置加载完毕回调
    /// </summary>
    private void OnAllEnemyDefsLoaded(AllEnemyDefs_SO so)
    {
        if (so == null || so.enemyDefs == null)
        {
            LogError("敌人配置加载失败");
            return;
        }

        foreach (var enemyDef in so.enemyDefs)
        {
            if (enemyDef == null) continue;

            if (_enemyConfigDict.ContainsKey(enemyDef.EnemyId))
            {
                LogError($"敌人ID冲突: {enemyDef.EnemyId}");
                continue;
            }

            _enemyConfigDict[enemyDef.EnemyId] = enemyDef;
            Log($"敌人配置加载完成: ID={enemyDef.EnemyId}, Name={enemyDef.EnemyName}");
        }
    }

    /// <summary>
    /// 根据敌人ID获取敌人配置（O(1) 查找）
    /// </summary>
    public EnemyDefBase GetEnemyConfig(int enemyId)
    {
        if (_enemyConfigDict.TryGetValue(enemyId, out var config))
            return config;
        LogWarning($"未找到敌人配置: ID={enemyId}");
        return null;
    }

    #endregion

    #region LootData掉落表配置相关

    /// <summary>
    /// 所有掉落表配置加载完毕回调
    /// </summary>
    private void OnAllLootTablesLoaded(AllLootTables_SO so)
    {
        if (so == null || so.lootTables == null)
        {
            LogError("掉落表配置加载失败");
            return;
        }

        foreach (var table in so.lootTables)
        {
            if (table == null) continue;

            if (_lootTableDict.ContainsKey(table.EnemyType))
            {
                LogWarning($"掉落表重复: {table.EnemyType}");
                continue;
            }

            _lootTableDict[table.EnemyType] = table;
            Log($"掉落表加载完成: EnemyType={table.EnemyType}, Entries={table.Entries.Count}");
        }
    }

    /// <summary>
    /// 根据敌人类型获取掉落表
    /// </summary>
    public LootTable_SO GetLootTable(EnemyType enemyType)
    {
        if (_lootTableDict.TryGetValue(enemyType, out var table))
            return table;
        return null;
    }

    #endregion

    #region SettingsData设置数据相关

    /// <summary>
    /// 加载设置数据
    /// </summary>
    public void LoadSettings()
    {
        _settingsData = JsonUtil.LoadFromFile<SettingsData>(_settingsFilePath);
        if (_settingsData == null)
        {
            _settingsData = new SettingsData();
            Log("未找到设置文件，使用默认设置");
        }
        else
        {
            Log("设置数据加载完成");
        }
    }

    /// <summary>
    /// 保存设置数据
    /// </summary>
    public void SaveSettings()
    {
        if (_settingsData == null) return;
        JsonUtil.SaveToFile(_settingsFilePath, _settingsData);
        Log("设置数据已保存");
    }

    /// <summary>
    /// 获取设置数据
    /// </summary>
    public SettingsData GetSettingsData()
    {
        if (_settingsData == null)
            LoadSettings();
        return _settingsData;
    }

    #endregion

    // 日志
    private void Log(string msg) => Debug.Log("[GameDataManager] " + msg);
    private void LogWarning(string msg) => Debug.LogWarning("[GameDataManager] " + msg);
    private void LogError(string msg) => Debug.LogError("[GameDataManager] " + msg);
}
