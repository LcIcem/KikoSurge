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

    // 武器配置字典：Key = itemConfig.Id, Value = WeaponConfig
    private Dictionary<int, WeaponConfig> _weaponConfigDict = new();
    public Dictionary<int, WeaponConfig> WeaponConfigDict => _weaponConfigDict;

    // 物品配置字典：Key = ItemConfig.Id, Value = ItemConfig
    private Dictionary<int, ItemConfig> _itemConfigs = new();
    public Dictionary<int, ItemConfig> ItemConfigDict => _itemConfigs;

    // 敌人配置字典：Key = EnemyId, Value = 配置SO
    private Dictionary<int, EnemyConfig> _enemyConfigDict = new();
    public Dictionary<int, EnemyConfig> EnemyConfigDict => _enemyConfigDict;

    // 设置数据
    private SettingsData _settingsData;
    public SettingsData SettingsData => _settingsData;
    private string _settingsFilePath => Path.Combine(Application.persistentDataPath, "settings.json");


    protected override void Init()
    {
        // 加载角色静态数据配置
        ManagerHub.Addressables.LoadAsync<RoleStaticDataConfig>("RoleStaticData_Config", OnRoleStaticDataLoaded);

        // 加载所有武器配置（按标签批量加载）
        ManagerHub.Addressables.LoadByLabelAsync<WeaponConfig>("WeaponConfigs", OnAllWeaponConfigsLoaded);

        // 加载所有物品配置（按标签批量加载）
        ManagerHub.Addressables.LoadByLabelAsync<ItemConfig>("ItemConfigs", OnAllItemConfigsLoaded);

        // 加载敌人配置（统一SO）
        ManagerHub.Addressables.LoadAsync<EnemyConfigRegistry>("Enemy_Config_Registry", OnAllEnemyDefsLoaded);

        // 加载设置数据（必须在 ApplySettings 之前完成）
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
    private void OnAllWeaponConfigsLoaded(IList<WeaponConfig> configs)
    {
        if (configs == null || configs.Count == 0)
        {
            LogWarning("武器配置加载失败或为空");
            return;
        }

        foreach (var config in configs)
        {
            if (config == null || config.itemConfig == null) continue;

            int id = config.itemConfig.Id;
            if (_weaponConfigDict.ContainsKey(id))
            {
                LogWarning($"武器配置重复: Id={id}, Name={config.itemConfig?.Name}");
                continue;
            }

            _weaponConfigDict[id] = config;
        }
        Log($"武器配置加载完成: {_weaponConfigDict.Count} 个");
    }

    /// <summary>
    /// 根据武器Id获取武器配置
    /// </summary>
    public WeaponConfig GetWeaponConfig(int id)
    {
        if (_weaponConfigDict.TryGetValue(id, out var config))
            return config;
        LogWarning($"未找到武器配置: Id={id}");
        return null;
    }

    #endregion

    #region ItemData物品配置相关

    /// <summary>
    /// 所有物品配置加载完毕回调
    /// </summary>
    private void OnAllItemConfigsLoaded(IList<ItemConfig> configs)
    {
        if (configs == null || configs.Count == 0)
        {
            LogWarning("物品配置加载失败或为空");
            return;
        }

        foreach (var config in configs)
        {
            if (config == null) continue;

            if (_itemConfigs.ContainsKey(config.Id))
            {
                LogWarning($"物品配置重复: Id={config.Id}, Name={config.Name}");
                continue;
            }

            _itemConfigs[config.Id] = config;
        }
        Log($"物品配置加载完成: {_itemConfigs.Count} 个");
    }

    /// <summary>
    /// 根据物品Id获取物品配置
    /// </summary>
    public ItemConfig GetItemConfig(int id)
    {
        if (_itemConfigs.TryGetValue(id, out var config))
            return config;
        LogWarning($"未找到物品配置: Id={id}");
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
