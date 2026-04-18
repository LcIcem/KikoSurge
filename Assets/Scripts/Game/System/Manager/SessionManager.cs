using System;
using System.Collections.Generic;
using UnityEngine;
using LcIcemFramework;
using LcIcemFramework.Core;

/// <summary>
/// Session 管理器
/// <para>统一管理当前 session 的所有数据，提供统一的访问接口</para>
/// </summary>
public class SessionManager : SingletonMono<SessionManager>
{
    /// <summary>
    /// 当前会话数据
    /// </summary>
    public SessionData CurrentSession => _currentSession;

    /// <summary>
    /// 是否有进行中的 session
    /// </summary>
    public bool HasActiveSession => _currentSession != null;

    private SessionData _currentSession;

    protected override void Init()
    {
        // 初始化为空，等待 StartSession 或 LoadSession
        _currentSession = null;
    }

    #region Session 生命周期

    /// <summary>
    /// 开始新 session
    /// </summary>
    public void StartSession(long seed)
    {
        int roleId = SaveLoadManager.Instance.LastSelectedRoleId;
        _currentSession = SessionData.CreateNew(roleId, seed);
        var roleData = GameDataManager.Instance?.GetRoleStaticData(roleId);
        Debug.Log($"[SessionManager] Session started: role={roleData?.roleName ?? roleId.ToString()}, seed={seed}, maxWeaponSlots={roleData?.maxWeaponSlots ?? 2}, equipped=[{string.Join(", ", _currentSession.equippedWeaponIds)}], inventory=[{string.Join(", ", _currentSession.inventoryWeaponIds)}]");

        // 将 runtime session 与存档关联（共享同一个 SessionData 引用）
        // 这样 SaveSession() 才能正确地将数据写回存档
        if (SaveLoadManager.Instance?.CurrentSaveData != null)
        {
            SaveLoadManager.Instance.CurrentSaveData.sessionData = _currentSession;
        }

        // 立即保存 session 关键数据（seed、roleId）到存档
        SaveLoadManager.Instance?.SaveSession();
    }

    /// <summary>
    /// 加载已有 session
    /// </summary>
    public void LoadSession(SessionData sessionData)
    {
        _currentSession = sessionData;
        Debug.Log($"[SessionManager] Session loaded: role={sessionData.selectedRoleName}, floor={sessionData.currentFloor}");
    }

    /// <summary>
    /// 保存当前 session
    /// </summary>
    public void SaveSession()
    {
        if (_currentSession == null)
        {
            Debug.LogWarning("[SessionManager] No active session to save");
            return;
        }

        SaveLoadManager.Instance.SaveSession();
    }

    /// <summary>
    /// 结束当前 session
    /// </summary>
    public void EndSession(bool isVictory)
    {
        if (_currentSession == null)
        {
            Debug.LogWarning("[SessionManager] No active session to end");
            return;
        }

        SaveLoadManager.Instance.EndGame(isVictory);
        _currentSession = null;
        Debug.Log($"[SessionManager] Session ended: victory={isVictory}");
    }

    #endregion

    #region 数据访问

    /// <summary>
    /// 获取玩家运行时数据（静态+全局加成+修饰器计算后的最终数据）
    /// </summary>
    public PlayerRuntimeData GetPlayerData()
    {
        if (_currentSession == null)
        {
            Debug.LogWarning("[SessionManager] No active session");
            return null;
        }

        Debug.Log($"[SessionManager] GetPlayerData: selectedRoleId={_currentSession.selectedRoleId}, IsRoleStaticDataLoaded={GameDataManager.Instance?.IsRoleStaticDataLoaded}");

        var roleData = GameDataManager.Instance?.GetRoleStaticData(_currentSession.selectedRoleId);

        // Fallback: if roleId doesn't match, try using CurSelRoleIndex as list index
        if (roleData == null)
        {
            Debug.LogWarning($"[SessionManager] RoleId={_currentSession.selectedRoleId} not found, trying fallback with CurSelRoleIndex={GameDataManager.Instance?.CurSelRoleIndex}");
            roleData = GameDataManager.Instance?.GetRoleStaticDataByCurSel();
        }

        if (roleData == null)
        {
            Debug.LogError($"[SessionManager] Cannot compute player data: role static data not found for roleId={_currentSession.selectedRoleId}");
            return null;
        }

        return PlayerRuntimeData.ComputeRuntimeData(
            roleData,
            SaveLoadManager.Instance?.CurrentSaveData?.metaData,
            _currentSession.modifiers,
            _currentSession.currentHealth
        );
    }

    /// <summary>
    /// 获取当前玩家生命值
    /// </summary>
    public float GetPlayerHealth()
    {
        return _currentSession?.currentHealth ?? 0f;
    }

    /// <summary>
    /// 设置当前玩家生命值
    /// </summary>
    public void SetPlayerHealth(float health)
    {
        if (_currentSession != null)
        {
            _currentSession.currentHealth = health;
        }
    }

    /// <summary>
    /// 获取修饰器列表
    /// </summary>
    public List<ModifierData> GetModifiers()
    {
        return _currentSession?.modifiers;
    }

    /// <summary>
    /// 添加修饰器
    /// </summary>
    public void AddModifier(ModifierData modifier)
    {
        _currentSession?.AddModifier(modifier);
    }

    /// <summary>
    /// 移除修饰器
    /// </summary>
    public void RemoveModifier(int modifierId)
    {
        _currentSession?.RemoveModifier(modifierId);
    }

    /// <summary>
    /// 获取背包武器ID列表
    /// </summary>
    public List<int> GetInventoryWeaponIds()
    {
        return _currentSession?.inventoryWeaponIds ?? new List<int>();
    }

    /// <summary>
    /// 设置背包武器ID列表
    /// </summary>
    public void SetInventoryWeaponIds(List<int> weaponIds)
    {
        if (_currentSession != null)
        {
            _currentSession.inventoryWeaponIds = weaponIds ?? new List<int>();
        }
    }

    /// <summary>
    /// 获取已装备武器ID列表
    /// </summary>
    public List<int> GetEquippedWeaponIds()
    {
        return _currentSession?.equippedWeaponIds ?? new List<int>();
    }

    /// <summary>
    /// 设置已装备武器ID列表
    /// </summary>
    public void SetEquippedWeaponIds(List<int> weaponIds)
    {
        if (_currentSession != null)
        {
            _currentSession.equippedWeaponIds = weaponIds ?? new List<int>();
        }
    }

    /// <summary>
    /// 获取背包遗物ID列表
    /// </summary>
    public List<int> GetInventoryRelicIds()
    {
        return _currentSession?.inventoryRelicIds ?? new List<int>();
    }

    /// <summary>
    /// 设置背包遗物ID列表
    /// </summary>
    public void SetInventoryRelicIds(List<int> relicIds)
    {
        if (_currentSession != null)
        {
            _currentSession.inventoryRelicIds = relicIds ?? new List<int>();
        }
    }

    /// <summary>
    /// 获取货币
    /// </summary>
    public int GetCurrency()
    {
        return _currentSession?.gold ?? 0;
    }

    /// <summary>
    /// 设置货币
    /// </summary>
    public void SetCurrency(int amount)
    {
        if (_currentSession != null)
        {
            _currentSession.gold = amount;
        }
    }

    /// <summary>
    /// 添加货币
    /// </summary>
    public void AddCurrency(int amount)
    {
        if (_currentSession != null)
        {
            _currentSession.gold += amount;
        }
    }

    /// <summary>
    /// 花费货币
    /// </summary>
    public bool SpendCurrency(int amount)
    {
        if (_currentSession != null && _currentSession.gold >= amount)
        {
            _currentSession.gold -= amount;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取当前楼层
    /// </summary>
    public int GetCurrentFloor()
    {
        return _currentSession?.currentFloor ?? 0;
    }

    /// <summary>
    /// 设置当前楼层
    /// </summary>
    public void SetCurrentFloor(int floor)
    {
        if (_currentSession != null)
        {
            _currentSession.currentFloor = floor;
        }
    }

    /// <summary>
    /// 获取玩家位置
    /// </summary>
    public Vector2 GetPlayerPos()
    {
        return _currentSession?.GetPlayerPos() ?? Vector2.zero;
    }

    /// <summary>
    /// 设置玩家位置
    /// </summary>
    public void SetPlayerPos(Vector2 pos)
    {
        _currentSession?.SetPlayerPos(pos);
    }

    /// <summary>
    /// 获取当前检查点
    /// </summary>
    public LayerSnapshot GetCurrentCheckpoint()
    {
        return _currentSession?.currentCheckpoint;
    }

    /// <summary>
    /// 保存检查点（用于中途退出后继续游玩）
    /// <para>注意：死亡后session结束，此checkpoint不会被使用</para>
    /// </summary>
    public void SaveCheckpoint(LayerSnapshot snapshot)
    {
        _currentSession?.SaveCheckpoint(snapshot);
        // 立即持久化到磁盘
        SaveLoadManager.Instance.FlushSession();
    }

    #endregion
}
