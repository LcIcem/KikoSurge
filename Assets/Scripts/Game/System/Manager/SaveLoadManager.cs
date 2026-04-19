using System;
using System.Collections.Generic;
using UnityEngine;
using LcIcemFramework;
using LcIcemFramework.Core;
using LcIcemFramework.Util.Crypto;
using LcIcemFramework.Util.Data;
using LcIcemFramework.Util.Const;

/// <summary>
/// 存档加载管理器
/// <para>封装 SaveManager，提供高层存档 API</para>
/// </summary>
public class SaveLoadManager : SingletonMono<SaveLoadManager>
{
    private const int MAX_SLOT = 3;
    private const int SAVE_VERSION = 1;
    private const string PLAYERPREFS_LAST_SLOT = "LastUsedSaveSlot";

    [Header("当前存档槽位")]
    private int _currentSlotId = -1;

    [Header("当前存档数据（运行时）")]
    private PlayerSaveData _currentSaveData;

    /// <summary>
    /// 当前存档槽位
    /// </summary>
    public int CurrentSlotId => _currentSlotId;

    /// <summary>
    /// 当前存档数据
    /// </summary>
    public PlayerSaveData CurrentSaveData => _currentSaveData;

    /// <summary>
    /// 上次选择的角色ID（用于开始游戏时创建角色）
    /// </summary>
    public int LastSelectedRoleId => _currentSaveData?.lastSelectedRoleId ?? 0;

    /// <summary>
    /// 是否有进行中的游戏
    /// </summary>
    public bool HasActiveSession => _currentSaveData != null && _currentSaveData.HasActiveSession;

    /// <summary>
    /// 获取当前会话的玩家运行时数据（用于继续游戏时恢复玩家数据）
    /// <para>由 RoleStaticData + PlayerMetaData(全局加成) + SessionData.modifiers 计算得出</para>
    /// </summary>
    public PlayerRuntimeData GetCurrentPlayerData()
    {
        if (_currentSaveData?.sessionData == null)
            return null;

        var session = _currentSaveData.sessionData;
        var staticData = GameDataManager.Instance?.GetRoleStaticData(session.selectedRoleId);
        if (staticData == null)
        {
            Debug.LogError("[SaveLoadManager] Cannot compute player data: static data not found");
            return null;
        }

        return PlayerRuntimeData.ComputeRuntimeData(
            staticData,
            _currentSaveData.metaData,
            session.modifiers
        );
    }

    /// <summary>
    /// 获取当前会话的当前楼层
    /// </summary>
    public int GetCurrentFloor()
    {
        return _currentSaveData?.sessionData?.currentFloor ?? 0;
    }

    /// <summary>
    /// 获取当前会话的修饰器列表
    /// </summary>
    public List<ModifierData> GetCurrentModifiers()
    {
        return _currentSaveData?.sessionData?.modifiers;
    }

    protected override void Init()
    {
        // 读取上次使用的槽位，默认为0
        int lastSlot = PlayerPrefs.GetInt(PLAYERPREFS_LAST_SLOT, 0);
        SelectSlot(lastSlot);
    }

    /// <summary>
    /// 设置最后选择的角色ID（切换角色时调用，持久化到存档）
    /// </summary>
    public void SetLastSelectedRoleId(int roleId)
    {
        if (_currentSaveData != null)
        {
            _currentSaveData.lastSelectedRoleId = roleId;
            SaveCurrentSlot();
        }
    }

    #region 槽位查询

    /// <summary>
    /// 检查槽位是否有存档
    /// </summary>
    public bool HasSaveData(int slotId)
    {
        if (slotId < 0 || slotId >= MAX_SLOT)
            return false;
        return ManagerHub.Save.Exists(slotId);
    }

    /// <summary>
    /// 获取所有已有存档的槽位列表
    /// </summary>
    public int[] GetUsedSlots()
    {
        return ManagerHub.Save.GetUsedSlots();
    }

    /// <summary>
    /// 加载指定槽位的存档元信息（不加载完整数据）
    /// </summary>
    public PlayerSaveData LoadSlotInfo(int slotId)
    {
        if (slotId < 0 || slotId >= MAX_SLOT)
            return null;

        try
        {
            string path = GetSlotPath(slotId);
            if (!System.IO.File.Exists(path))
                return null;

            string encrypted = System.IO.File.ReadAllText(path);
            string json = EncryptUtil.AESDecrypt(encrypted, Constants.SAVE_ENCRYPTION_KEY);
            return JsonUtil.FromJson<PlayerSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadManager] LoadSlotInfo failed: {e.Message}");
            return null;
        }
    }

    #endregion

    #region 存档操作

    /// <summary>
    /// 选择存档槽位（加载存档）
    /// </summary>
    public bool SelectSlot(int slotId)
    {
        if (slotId < 0 || slotId >= MAX_SLOT)
        {
            Debug.LogError($"[SaveLoadManager] Invalid slot: {slotId}");
            return false;
        }

        _currentSlotId = slotId;

        // 持久化当前槽位
        PlayerPrefs.SetInt(PLAYERPREFS_LAST_SLOT, slotId);

        if (HasSaveData(slotId))
        {
            _currentSaveData = ManagerHub.Save.Load<PlayerSaveData>(slotId);
            if (_currentSaveData == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Slot {slotId} save data corrupted, creating new");
                _currentSaveData = PlayerSaveData.CreateNew(slotId);
            }
            Debug.Log($"[SaveLoadManager] Slot {slotId} loaded: {(_currentSaveData.HasActiveSession ? "Continue" : "New Game")}");
        }
        else
        {
            _currentSaveData = PlayerSaveData.CreateNew(slotId);
            Debug.Log($"[SaveLoadManager] Slot {slotId} created (empty)");
        }

        return true;
    }

    /// <summary>
    /// 创建新存档（仅创建元数据，无 session）
    /// </summary>
    public void CreateNewSave(int slotId)
    {
        if (slotId < 0 || slotId >= MAX_SLOT)
        {
            Debug.LogError($"[SaveLoadManager] Invalid slot: {slotId}");
            return;
        }

        _currentSlotId = slotId;
        _currentSaveData = PlayerSaveData.CreateNew(slotId);
        SaveCurrentSlot();
        Debug.Log($"[SaveLoadManager] CreateNewSave: slot={slotId}");
    }

    /// <summary>
    /// 保存当前游戏（SessionData）
    /// </summary>
    public void SaveSession()
    {
        if (_currentSaveData?.sessionData == null || SessionManager.Instance?.CurrentSession == null)
        {
            Debug.LogWarning("[SaveLoadManager] No active session to save");
            return;
        }

        var session = _currentSaveData.sessionData;
        var current = SessionManager.Instance.CurrentSession;

        // 保存所有 session 关键数据
        session.seed = current.seed;
        session.currentFloor = current.currentFloor;
        session.SetPlayerPos(current.GetPlayerPos());
        session.inventoryWeaponIds = current.inventoryWeaponIds;
        session.inventoryRelicIds = current.inventoryRelicIds;
        session.equippedWeaponIds = current.equippedWeaponIds;
        session.currentHealth = current.currentHealth;
        session.currentCheckpoint = current.currentCheckpoint;

        // 保存 modifiers 列表（创建副本避免引用问题）
        session.modifiers = current.modifiers != null
            ? new List<ModifierData>(current.modifiers)
            : new List<ModifierData>();

        SaveCurrentSlot();
        Debug.Log($"[SaveLoadManager] Session saved: floor={current.currentFloor}, health={current.currentHealth}, checkpoint={current.currentCheckpoint != null}, modifiers={session.modifiers.Count}");
    }

    /// <summary>
    /// 保存玩家当前生命值（用于检查点）
    /// </summary>
    public void SavePlayerHealth(float health)
    {
        if (_currentSaveData?.sessionData == null)
            return;

        // 生命值存储在 currentCheckpoint 中
        if (_currentSaveData.sessionData.currentCheckpoint != null)
        {
            // 可以存储在 checkpoint 的某个字段
            // 目前简化处理，存储到 session 级别
        }
    }

    /// <summary>
    /// 保存当前槽位（内部方法）
    /// </summary>
    private void SaveCurrentSlot()
    {
        if (_currentSlotId < 0 || _currentSlotId >= MAX_SLOT)
        {
            Debug.LogWarning("[SaveLoadManager] No slot selected, cannot save");
            return;
        }

        if (_currentSaveData == null)
        {
            Debug.LogWarning("[SaveLoadManager] No save data, cannot save");
            return;
        }

        _currentSaveData.lastPlayedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        ManagerHub.Save.Save(_currentSlotId, _currentSaveData as LcIcemFramework.SaveData);
        Debug.Log($"[SaveLoadManager] Slot {_currentSlotId} saved");
    }

    /// <summary>
    /// 立即持久化当前 session 到磁盘（用于检查点保存后立即落盘）
    /// </summary>
    public void FlushSession()
    {
        SaveCurrentSlot();
    }

    /// <summary>
    /// 添加修饰器到当前 session
    /// </summary>
    public void AddModifier(ModifierData modifier)
    {
        if (_currentSaveData?.sessionData == null)
        {
            Debug.LogWarning("[SaveLoadManager] No active session to add modifier");
            return;
        }

        _currentSaveData.sessionData.AddModifier(modifier);
        Debug.Log($"[SaveLoadManager] Modifier added: {modifier.modifierName} = {modifier.value}");
    }

    /// <summary>
    /// 移除修饰器
    /// </summary>
    public void RemoveModifier(int modifierId)
    {
        if (_currentSaveData?.sessionData == null)
            return;

        _currentSaveData.sessionData.RemoveModifier(modifierId);
    }

    /// <summary>
    /// 结束当前游戏（死亡或胜利）
    /// </summary>
    public void EndGame(bool isVictory)
    {
        if (_currentSaveData?.sessionData == null)
        {
            Debug.LogWarning("[SaveLoadManager] No active session to end");
            return;
        }

        // 应用游戏结果到 MetaData
        _currentSaveData.metaData.ApplyGameResult(isVictory);

        // 清空 SessionData
        _currentSaveData.sessionData.Clear();
        _currentSaveData.sessionData = null;

        // 保存
        SaveCurrentSlot();
        Debug.Log($"[SaveLoadManager] Game ended: victory={isVictory}");
    }

    /// <summary>
    /// 删除指定槽位的存档
    /// </summary>
    public bool DeleteSlot(int slotId)
    {
        if (slotId < 0 || slotId >= MAX_SLOT)
            return false;

        bool deleted = ManagerHub.Save.Delete(slotId);

        if (deleted && _currentSlotId == slotId)
        {
            _currentSaveData = null;
            _currentSlotId = -1;
        }

        Debug.Log($"[SaveLoadManager] Slot {slotId} deleted: {deleted}");
        return deleted;
    }

    /// <summary>
    /// 累计游玩时长
    /// </summary>
    public void AddPlayTime(long seconds)
    {
        if (_currentSaveData != null)
        {
            _currentSaveData.AddPlayTime(seconds);
            SaveCurrentSlot();
        }
    }

    #endregion

    #region 工具方法

    private string GetSlotPath(int slotId)
    {
        string saveDir = System.IO.Path.Combine(Application.persistentDataPath, "saves");
        return System.IO.Path.Combine(saveDir, $"save_{slotId}.json");
    }

    /// <summary>
    /// 生成新的随机种子
    /// </summary>
    public long GenerateSeed()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    #endregion

    #region 日志

    private void Log(string msg) => Debug.Log($"[SaveLoadManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[SaveLoadManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[SaveLoadManager] {msg}");

    #endregion
}
