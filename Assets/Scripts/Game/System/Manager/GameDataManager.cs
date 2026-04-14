using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework.Managers;
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

    // 武器配置字典：Key = WeaponId, Value = 配置SO
    private Dictionary<int, WeaponDefBase> _weaponConfigDict = new();
    public Dictionary<int, WeaponDefBase> WeaponConfigDict => _weaponConfigDict;


    protected override void Init()
    {
        // 加载角色信息
        ManagerHub.Addressables.LoadAsync<RoleInfo_SO>("RoleInfo_SO", OnRoleInfoLoaded);

        // 加载武器配置
        ManagerHub.Addressables.LoadAsync<GunWeaponDef_SO>("GunWeaponDef_SO", so => OnWeaponDefLoaded(so));
        ManagerHub.Addressables.LoadAsync<ShotgunWeaponDef_SO>("ShotgunWeaponDef_SO", so => OnWeaponDefLoaded(so));
    }

    private void Start() {
        // 订阅事件
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
    /// 武器配置加载完毕回调
    /// </summary>
    private void OnWeaponDefLoaded(WeaponDefBase so)
    {
        if (so == null) return;

        if (_weaponConfigDict.ContainsKey(so.WeaponId))
        {
            LogError($"武器ID冲突: {so.WeaponId}");
            return;
        }

        _weaponConfigDict[so.WeaponId] = so;
        Log($"武器配置加载完成: ID={so.WeaponId}, Name={so.WeaponName}");
    }

    /// <summary>
    /// 根据武器ID获取武器配置（O(1) 查找）
    /// </summary>
    public WeaponDefBase GetWeaponConfig(int weaponId)
    {
        if (_weaponConfigDict.TryGetValue(weaponId, out var config))
            return config;
        LogWarning($"未找到武器配置: ID={weaponId}");
        return null;
    }

    #endregion

    // 日志
    private void Log(string msg) => Debug.Log("[GameDataManager] " + msg);
    private void LogWarning(string msg) => Debug.LogWarning("[GameDataManager] " + msg);
    private void LogError(string msg) => Debug.LogError("[GameDataManager] " + msg);
}
