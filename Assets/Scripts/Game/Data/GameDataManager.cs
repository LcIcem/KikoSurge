using System.Collections;
using UnityEngine;

using LcIcemFramework.Core;
using LcIcemFramework.Managers;

public class GameDataManager : Singleton<GameDataManager>
{
    // 角色信息相关
    private RoleInfo_SO _roleInfo_SO;
    public bool IsRoleInfoLoaded { get; private set; } // 角色信息是否加载成功
    public int CurSelRoleIndex { get; set; } = 0;   // 当前选择的角色索引
    // 玩家数据相关
    private PlayerData _playerData;


    protected override void Init()
    {
        // 加载资源
        ManagerHub.Addressables.LoadAsync<RoleInfo_SO>("RoleInfo_SO", OnRoleInfoLoaded);
    }

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

        _playerData = p;
        return true;
    }

    // 获取玩家数据
    public PlayerData GetPlayerData()
    {
        if (_playerData == null)
            return default;
        return _playerData;
    }

    #endregion

    // 日志
    private void Log(string msg) => Debug.Log("[GameDataManager] " + msg);
    private void LogWarning(string msg) => Debug.LogWarning("[GameDataManager] " + msg);
    private void LogError(string msg) => Debug.LogError("[GameDataManager] " + msg);
}
