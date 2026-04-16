using System;
using Game.Event;
using LcIcemFramework.Core;
using LcIcemFramework;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 游戏状态
/// </summary>
public enum GameState
{
    SaveSelect,  // 存档选择
    Lobby,       // 大厅
    Playing,     // 游戏中
    Paused,      // 暂停
    GameOver     // 游戏结束
}

/// <summary>
/// 游戏生命周期管理器
/// <para>作为游戏总入口，协调整个游戏生命周期</para>
/// </summary>
public class GameLifecycleManager : SingletonMono<GameLifecycleManager>
{
    [Header("LevelController")]
    [SerializeField] private LevelController _levelControllerPrefab;

    [Header("关卡种子（调试用）")]
    [SerializeField] private long _debugSeed = 12345;

    /// <summary>
    /// 当前游戏状态
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.SaveSelect;

    /// <summary>
    /// 当前关卡管理器
    /// </summary>
    public LevelController LevelController { get; private set; }

    protected override void Init()
    {
        Log("GameLifecycleManager initialized");
        ChangeState(GameState.Lobby);
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    /// <param name="saveSlot">存档槽位</param>
    /// <param name="seed">游戏种子（默认使用时间戳）</param>
    public void StartNewGame(int saveSlot, long? seed = null)
    {
        long sessionSeed = seed ?? Environment.TickCount;

        Log($"StartNewGame: slot={saveSlot}, seed={sessionSeed}");

        // TODO: SaveLoadManager.CreateNewSave(saveSlot, sessionSeed);
        // TODO: GameDataManager.StartNewSession(sessionSeed);

        // 实例化 LevelController
        if (_levelControllerPrefab != null)
        {
            LevelController = Instantiate(_levelControllerPrefab);
            LevelController.Initialize(sessionSeed);
            LevelController.EnterFirstLayer();
        }
        else
        {
            LogError("LevelController Prefab is not assigned!");
        }

        ChangeState(GameState.Playing);
        EventCenter.Instance.Publish(GameEventID.OnSessionStart, sessionSeed);
    }

    /// <summary>
    /// 继续游戏（断点续玩）
    /// </summary>
    /// <param name="saveSlot">存档槽位</param>
    public void ContinueGame(int saveSlot)
    {
        Log($"ContinueGame: slot={saveSlot}");

        // TODO: SaveLoadManager.LoadSave(saveSlot);
        // TODO: GameDataManager.RestoreSession(saveData);

        // 从存档恢复 LevelController 状态
        if (LevelController != null)
        {
            // TODO: 从 SessionData 恢复当前层信息
            // _levelManager.RestoreFromSession(currentSession);
        }

        ChangeState(GameState.Playing);
        EventCenter.Instance.Publish(GameEventID.OnSessionContinue);
    }

    /// <summary>
    /// 返回大厅
    /// </summary>
    public void ReturnToLobby()
    {
        Log("ReturnToLobby");

        // 销毁关卡
        if (LevelController != null)
        {
            Destroy(LevelController.gameObject);
            LevelController = null;
        }

        // TODO: 保存当前进度

        ChangeState(GameState.Lobby);
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Log("QuitGame");
        Application.Quit();
    }

    /// <summary>
    /// 切换游戏状态
    /// </summary>
    private void ChangeState(GameState newState)
    {
        GameState oldState = CurrentState;
        CurrentState = newState;
        Log($"GameState changed: {oldState} -> {newState}");
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            // TODO: 显示暂停菜单
        }
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
            // TODO: 隐藏暂停菜单
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void GameOver()
    {
        Log("GameOver");
        ChangeState(GameState.GameOver);
        EventCenter.Instance.Publish(GameEventID.OnSessionEnd);
    }

    // 调试方法

    /// <summary>
    /// 调试：使用默认种子开始新游戏
    /// </summary>
    public void DebugStartNewGame(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            StartNewGame(0, _debugSeed);
    }

    /// <summary>
    /// 调试：进入下一层
    /// </summary>
    public void DebugEnterNextLayer(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && LevelController != null && CurrentState == GameState.Playing)
        {
            LevelController.EnterNextLayer();
        }
    }

    private void Log(string msg) => Debug.Log($"[GameLifecycleManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[GameLifecycleManager] {msg}");
}
