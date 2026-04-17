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
    MainMenu,    // 主菜单
    Lobby,       // 大厅（准备阶段）
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

    /// <summary>
    /// 当前游戏状态
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    /// <summary>
    /// 当前关卡管理器
    /// </summary>
    public LevelController LevelController { get; private set; }

    /// <summary>
    /// 设置面板是否阻塞继续游戏（从暂停菜单打开时为 true）
    /// 为 true 时 ESC 仅关闭设置面板，不继续游戏
    /// </summary>
    public bool IsSettingsPanelBlocking { get; set; } = false;

    protected override void Init()
    {
        Log("GameLifecycleManager initialized");
        ChangeState(GameState.MainMenu);

        // 订阅死亡动画结束事件
        EventCenter.Instance.Subscribe(GameEventID.OnDeathAnimationEnd, OnDeathAnimationEnd);
    }

    private void OnDeathAnimationEnd()
    {
        GameOver();
    }

    private void Update()
    {
        // 游戏中按 ESC/Pause 打开暂停菜单
        if (CurrentState == GameState.Playing)
        {
            var pauseAction = ManagerHub.Input.Actions["Pause"];
            if (pauseAction != null && pauseAction.WasPressedThisFrame())
            {
                PauseGame();
            }
        }
        // 暂停时按 ESC/Resume 继续游戏
        else if (CurrentState == GameState.Paused)
        {
            // SettingsPanel 阻塞继续，ESC 只关闭设置面板，不继续游戏
            if (IsSettingsPanelBlocking)
                return;

            var resumeAction = ManagerHub.Input.Actions["Resume"];
            if (resumeAction != null && resumeAction.WasPressedThisFrame())
            {
                ResumeGame();
            }
        }
    }

    /// <summary>
    /// 当前游戏种子（供 EnterPlaying 使用）
    /// </summary>
    private long _currentSessionSeed;

    /// <summary>
    /// 当前存档槽位（供 RestartGame 使用）
    /// </summary>
    private int _currentSessionSaveSlot;

    /// <summary>
    /// 开始新游戏（从主菜单进入大厅）
    /// </summary>
    /// <param name="saveSlot">存档槽位</param>
    /// <param name="seed">游戏种子（默认使用时间戳）</param>
    public void StartNewGame(int saveSlot, long? seed = null)
    {
        Debug.Log($"[StartNewGame] saveSlot={saveSlot}, seed={seed}");
        _currentSessionSeed = seed ?? Environment.TickCount;
        _currentSessionSaveSlot = saveSlot;

        Log($"StartNewGame: slot={saveSlot}, seed={_currentSessionSeed}");

        // TODO: SaveLoadManager.CreateNewSave(saveSlot, sessionSeed);
        // TODO: GameDataManager.StartNewSession(sessionSeed);

        // 实例化 LevelController
        if (_levelControllerPrefab != null)
        {
            LevelController = Instantiate(_levelControllerPrefab);
            LevelController.Initialize(_currentSessionSeed);
        }
        else
        {
            LogError("LevelController Prefab is not assigned!");
        }

        // 先进入 Lobby 状态（大厅/准备阶段）
        ChangeState(GameState.Lobby);
    }

    /// <summary>
    /// 从大厅进入游戏（正式开玩）
    /// </summary>
    public void EnterPlaying()
    {
        if (LevelController == null)
        {
            LogError("EnterPlaying: LevelController is null!");
            return;
        }

        Log("EnterPlaying: Starting gameplay");
        LevelController.EnterFirstLayer();
        ChangeState(GameState.Playing);
        EventCenter.Instance.Publish(GameEventID.OnSessionStart, _currentSessionSeed);
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

        // 先进入 Lobby 状态
        ChangeState(GameState.Lobby);
        EventCenter.Instance.Publish(GameEventID.OnSessionContinue);
    }

    /// <summary>
    /// 主菜单点击开始游戏（跳过 Lobby 直接进入游戏）
    /// </summary>
    /// <param name="saveSlot">存档槽位</param>
    public void StartGameFromMenu(int saveSlot)
    {
        StartNewGame(saveSlot);
        EnterPlaying();
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
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        Log("ReturnToMainMenu");

        // 销毁关卡
        if (LevelController != null)
        {
            Destroy(LevelController.gameObject);
            LevelController = null;
        }

        // 隐藏游戏Hub
        ManagerHub.UI.HidePanel<HubPanel>();

        // TODO: 保存当前进度

        ChangeState(GameState.MainMenu);
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

        // 自动切换 Action Map（确保 ManagerHub.Input 已初始化）
        if (ManagerHub.Input != null)
        {
            ManagerHub.Input.SwitchActionMapByGameState(newState);
        }
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            // 显示暂停菜单
            ManagerHub.UI.ShowPanel<PausePanel>();
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
            // 隐藏暂停菜单
            ManagerHub.UI.HidePanel<PausePanel>();
        }
    }

    /// <summary>
    /// 游戏结束（显示失败面板）
    /// </summary>
    public void GameOver()
    {
        Log("GameOver");
        ChangeState(GameState.GameOver);
        EventCenter.Instance.Publish(GameEventID.OnSessionEnd);
        ManagerHub.UI.ShowPanel<GameOverPanel>(UILayerType.Top, panel =>
        {
            panel.ShowAsGameOver();
        });
    }

    /// <summary>
    /// 游戏通关（显示成功面板）
    /// </summary>
    public void GameClear()
    {
        Log("GameClear");
        ChangeState(GameState.GameOver);
        ManagerHub.UI.ShowPanel<GameOverPanel>(UILayerType.Top, panel =>
        {
            panel.ShowAsClear();
        });
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        Log("RestartGame");

        Debug.Log($"[RestartGame] LevelController = {LevelController?.name ?? "null"}");
        Debug.Log($"[RestartGame] CurrentState = {CurrentState}");

        // 清理敌人和掉落物
        try { EnemyFactory.Instance?.ReleaseAll(); } catch (System.Exception e) { Debug.LogException(e); }
        try { LootManager.Instance?.ClearAll(); } catch (System.Exception e) { Debug.LogException(e); }

        // 通过 PlayerHandler 清理玩家
        Debug.Log($"[RestartGame] Calling DestroyPlayer...");
        LevelController?.DestroyPlayer();

        // 销毁关卡
        if (LevelController != null)
        {
            Debug.Log($"[RestartGame] Destroying LevelController: {LevelController.name}");
            Destroy(LevelController.gameObject);
            LevelController = null;
        }
        else
        {
            Debug.Log("[RestartGame] LevelController is null, skipping destroy");
        }

        // 隐藏 GameOver 面板和 Hub 面板（Hub 在新游戏开始时会重新 Show）
        ManagerHub.UI.HidePanel<GameOverPanel>();
        ManagerHub.UI.HidePanel<HubPanel>();

        // 恢复时间（以防从暂停状态重启）
        Time.timeScale = 1f;

        Debug.Log("[RestartGame] Calling StartNewGame...");
        StartNewGame(_currentSessionSaveSlot);
        Debug.Log("[RestartGame] StartNewGame done. Calling EnterPlaying...");
        EnterPlaying();
        Debug.Log("[RestartGame] EnterPlaying done.");
    }

    // 调试方法

    /// <summary>
    /// 调试：使用默认种子开始新游戏
    /// </summary>
    public void DebugStartNewGame(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            StartGameFromMenu(0);
        }
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
