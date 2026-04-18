using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework.Data;
using LcIcemFramework;
using LcIcemFramework.Util.Data;

/// <summary>
/// 游戏数据管理器
/// </summary>
public class GameDataManager : SingletonMono<GameDataManager>
{
    // 角色静态数据配置
    private RoleStaticDataConfig _roleStaticDataConfig_SO;
    public bool IsRoleStaticDataLoaded { get; private set; }
    public int CurSelRoleIndex { get; set; } = 0;   // 当前选择的角色索引

    // 武器配置字典：Key = WeaponId, Value = 配置SO
    private Dictionary<int, GunConfig> _weaponConfigDict = new();
    public Dictionary<int, GunConfig> WeaponConfigDict => _weaponConfigDict;

    // 敌人配置字典：Key = EnemyId, Value = 配置SO
    private Dictionary<int, EnemyConfig> _enemyConfigDict = new();
    public Dictionary<int, EnemyConfig> EnemyConfigDict => _enemyConfigDict;

    // 掉落表字典：Key = EnemyId, Value = LootTableConfig
    private Dictionary<int, LootTableConfig> _lootTableDict = new();
    public Dictionary<int, LootTableConfig> LootTableDict => _lootTableDict;

    // 设置数据
    private SettingsData _settingsData;
    public SettingsData SettingsData => _settingsData;
    private string _settingsFilePath => Path.Combine(Application.persistentDataPath, "settings.json");


    protected override void Init()
    {
        // 加载角色静态数据配置
        ManagerHub.Addressables.LoadAsync<RoleStaticDataConfig>("RoleStaticData_Config", OnRoleStaticDataLoaded);

        // 加载武器配置（统一SO）
        ManagerHub.Addressables.LoadAsync<WeaponConfigRegistry>("Weapon_Config_Registry", OnAllWeaponDefsLoaded);

        // 加载敌人配置（统一SO）
        ManagerHub.Addressables.LoadAsync<EnemyConfigRegistry>("Enemy_Config_Registry", OnAllEnemyDefsLoaded);

        // 加载掉落表配置（统一SO）
        ManagerHub.Addressables.LoadAsync<LootTableRegistry>("LootTable_Registry", OnAllLootTablesLoaded);
    }

    private void Start() {
        LoadSettings();
        LoadKeybindings();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        // 退订事件
    }

    #region 处理事件

    #endregion

    #region RoleStaticData数据相关
    // 角色静态数据 加载完毕 回调
    private void OnRoleStaticDataLoaded(RoleStaticDataConfig so)
    {
        if (so != null)
        {
            _roleStaticDataConfig_SO = so;
            IsRoleStaticDataLoaded = true;
            Log("RoleStaticData 加载完成");
        }
        else
        {
            LogError("RoleStaticData 加载失败");
        }
    }

    // 获取当前选择角色静态数据
    public RoleStaticData GetRoleStaticDataByCurSel()
    {
        if (!IsRoleStaticDataLoaded)
        {
            LogWarning("RoleStaticData 尚未加载完成");
            return null;
        }
        return _roleStaticDataConfig_SO.roleStaticDataList[CurSelRoleIndex];
    }

    // 获取角色静态数据
    public RoleStaticData GetRoleStaticData(int roleId)
    {
        if (!IsRoleStaticDataLoaded)
        {
            LogWarning("RoleStaticData 尚未加载完成");
            return null;
        }
        return _roleStaticDataConfig_SO.GetRoleStaticData(roleId);
    }

    // 获取默认角色静态数据
    public RoleStaticData GetDefaultRoleStaticData()
    {
        if (!IsRoleStaticDataLoaded)
        {
            LogWarning("RoleStaticData 尚未加载完成");
            return null;
        }
        return _roleStaticDataConfig_SO.GetDefaultRole();
    }

    #endregion

    #region WeaponData武器配置相关

    /// <summary>
    /// 所有武器配置加载完毕回调
    /// </summary>
    private void OnAllWeaponDefsLoaded(WeaponConfigRegistry so)
    {
        if (so == null || so.weaponConfigs == null)
        {
            LogError("武器配置加载失败");
            return;
        }

        foreach (var config in so.weaponConfigs)
        {
            if (config == null) continue;

            // 使用 Id 做 key
            if (_weaponConfigDict.ContainsKey(config.Id))
            {
                LogWarning($"武器配置重复: Id={config.Id}, Name={config.gunName}");
                continue;
            }

            _weaponConfigDict[config.Id] = config;
            Log($"武器配置加载完成: Id={config.Id}, Name={config.gunName}");
        }
    }

    /// <summary>
    /// 根据武器Id获取武器配置
    /// </summary>
    public GunConfig GetWeaponConfig(int id)
    {
        if (_weaponConfigDict.TryGetValue(id, out var config))
            return config;
        LogWarning($"未找到武器配置: Id={id}");
        return null;
    }

    #endregion

    #region EnemyData敌人配置相关

    /// <summary>
    /// 所有敌人配置加载完毕回调
    /// </summary>
    private void OnAllEnemyDefsLoaded(EnemyConfigRegistry so)
    {
        if (so == null || so.enemyConfigs == null)
        {
            LogError("敌人配置加载失败");
            return;
        }

        foreach (var enemyDef in so.enemyConfigs)
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
    public EnemyConfig GetEnemyConfig(int enemyId)
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
    private void OnAllLootTablesLoaded(LootTableRegistry so)
    {
        if (so == null || so.lootTables == null)
        {
            LogError("掉落表配置加载失败");
            return;
        }

        foreach (var table in so.lootTables)
        {
            if (table == null) continue;

            if (_lootTableDict.ContainsKey(table.EnemyId))
            {
                LogWarning($"掉落表重复: EnemyId={table.EnemyId}");
                continue;
            }

            _lootTableDict[table.EnemyId] = table;
            Log($"掉落表加载完成: EnemyId={table.EnemyId}, Entries={table.Entries.Count}");
        }
    }

    /// <summary>
    /// 根据敌人ID获取掉落表
    /// </summary>
    public LootTableConfig GetLootTable(int enemyId)
    {
        if (_lootTableDict.TryGetValue(enemyId, out var table))
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

    #region Keybindings键位数据相关

    /// <summary>
    /// 保存键位设置
    /// </summary>
    public void SaveKeybindings()
    {
        SaveKeybindings(ManagerHub.Input.GetCurrentActionMapName());
    }

    /// <summary>
    /// 保存键位设置（指定 Map）
    /// </summary>
    /// <param name="mapName">要保存的 Map 名称</param>
    public void SaveKeybindings(string mapName)
    {
        if (_settingsData == null) return;

        // 合并：先把磁盘已有数据加载进来，再合并当前内存覆盖（避免丢失其他 Map 的覆盖）
        string existingJson = _settingsData.keybindingsJson;
        _settingsData.keybindingsJson = ManagerHub.Input.SaveBindingOverridesWithMerge(mapName, existingJson);
        SaveSettings();
        Log($"键位设置已保存 (map={mapName})");
    }

    /// <summary>
    /// 加载键位设置
    /// </summary>
    public void LoadKeybindings()
    {
        if (_settingsData == null || string.IsNullOrEmpty(_settingsData.keybindingsJson))
        {
            Log("无键位覆盖数据或为空");
            return;
        }
        ManagerHub.Input.LoadBindingOverrides(_settingsData.keybindingsJson);
        Log("键位设置已加载");
    }

    /// <summary>
    /// 重置键位设置
    /// </summary>
    public void ResetKeybindings()
    {
        if (_settingsData == null) return;
        _settingsData.keybindingsJson = "";
        SaveSettings();
        ManagerHub.Input.ResetBindingOverrides();
        Log("键位设置已重置");
    }

    #endregion

    // 日志
    private void Log(string msg) => Debug.Log("[GameDataManager] " + msg);
    private void LogWarning(string msg) => Debug.LogWarning("[GameDataManager] " + msg);
    private void LogError(string msg) => Debug.LogError("[GameDataManager] " + msg);
}
