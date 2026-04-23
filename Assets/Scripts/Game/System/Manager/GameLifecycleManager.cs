using System;
using Game.Event;
using LcIcemFramework.Core;
using LcIcemFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 游戏状态
/// </summary>
public enum GameState
{
    MainMenu,    // 主菜单
    Lobby,       // 大厅（准备阶段）
    Playing,     // 游戏中
    Interacting, // 交互中（背包、商店等），时间不暂停
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

    [Header("BGM 配置")]
    [SerializeField] private string _bgmMainMenu = "BGM-MainMenu";
    [SerializeField] private string _bgmLobby = "BGM-MainMenu";
    [SerializeField] private string _bgmPlaying = "BGM-Gaming";

    /// <summary>
    /// 当前游戏状态
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    /// <summary>
    /// 当前关卡管理器
    /// </summary>
    public LevelController LevelController { get; private set; }

    /// <summary>
    /// 暂停前是否来自大厅（用于 PausePanel 显示按钮判断）
    /// </summary>
    public bool WasInLobbyBeforePause => _wasInLobbyBeforePause;

    /// <summary>
    /// 是否有子面板打开（Pause 的子面板如 SettingsPanel）
    /// </summary>
    public bool HasChildPanelOpen { get; set; }

    /// <summary>
    /// 大厅玩家处理器（持有全局玩家数据）
    /// </summary>
    private PlayerHandler _lobbyPlayerHandler;

    /// <summary>
    /// 是否正在加载场景（加载期间禁用 Pause 检查）
    /// </summary>
    private bool _isSceneLoading;

    /// <summary>
    /// 是否正在加载场景（供外部访问）
    /// </summary>
    public bool IsSceneLoading => _isSceneLoading;

    /// <summary>
    /// 暂停前是否来自大厅（用于 PausePanel 显示按钮判断）
    /// </summary>
    private bool _wasInLobbyBeforePause;

    /// <summary>
    /// 游戏状态变化时触发
    /// </summary>
    public event Action<GameState> OnStateChanged;

    protected override void Init()
    {
        Log("GameLifecycleManager initialized");

        // 订阅死亡动画结束事件
        EventCenter.Instance.Subscribe(GameEventID.OnDeathAnimationEnd, OnDeathAnimationEnd);
    }

    private void Start()
    {
        // 在 Start 中调用 ChangeState，确保所有 SingletonMono 都已初始化完成
        // 如果在 Init() 中调用，此时 ManagerHub 可能尚未初始化，导致 ManagerHub.Audio 为 null
        ChangeState(GameState.MainMenu);
    }

    private bool _hasSubscribedPanelClosed;

    private void TrySubscribePanelClosed()
    {
        if (_hasSubscribedPanelClosed)
            return;
        if (ManagerHub.UI == null)
            return;
        if (!ManagerHub.UI.IsReady)
            return;

        ManagerHub.UI.OnPanelClosed += OnPanelClosed;
        _hasSubscribedPanelClosed = true;
    }

    private void OnPanelClosed(LcIcemFramework.BasePanel panel)
    {
        Debug.Log($"[OnPanelClosed] panel={panel?.GetType().Name}, CurrentState={CurrentState}");

        if (CurrentState == GameState.Interacting)
        {
            // Interacting 状态关闭面板时，恢复到之前的状态
            ChangeState(_stateBeforeInteracting);
            // 恢复玩家旋转
            AimInput.Enabled = true;
            // 恢复玩家移动
            UnlockPlayerMovement();
            // 恢复相机跟随
            CameraManager.Instance.EnableFollow();
        }
        else if (CurrentState == GameState.Paused)
        {
            // Paused 状态下，只有关闭的是 PausePanel 时才恢复游戏
            if (panel is PausePanel)
            {
                Debug.Log("[OnPanelClosed] PausePanel closed, resuming game");
                ResumeGame();
            }
        }
        else
        {
            // 其他状态（Lobby、Playing 等）：根据当前状态自动切换 ActionMap
            ManagerHub.Input.SwitchActionMapByGameState(CurrentState);
        }
    }

    private void OnDeathAnimationEnd()
    {
        GameOver();
    }

    private float _playTimeAccumulator;
    private const float PLAY_TIME_SAVE_INTERVAL = 60f; // 每60秒保存一次

    private void Update()
    {
        // 延迟订阅 UI 面板关闭事件（等待 UIManager 初始化完成）
        TrySubscribePanelClosed();

        // 场景加载期间不处理
        if (_isSceneLoading)
            return;

        // ========== Playing / Lobby 状态 ==========
        if (CurrentState == GameState.Playing || CurrentState == GameState.Lobby)
        {
            // 累计游玩时长
            _playTimeAccumulator += Time.deltaTime;
            if (_playTimeAccumulator >= PLAY_TIME_SAVE_INTERVAL)
            {
                SavePlayTime();
                _playTimeAccumulator = 0f;
            }

            // Pause 按键
            if (ManagerHub.Input.Actions.TryGetValue("Pause", out var pauseAction) &&
                pauseAction.WasPressedThisFrame())
            {
                PauseGame();
            }

            // OpenInventory 按键
            if (ManagerHub.Input.Actions.TryGetValue("OpenInventory", out var openInventoryAction) &&
                openInventoryAction.WasPressedThisFrame())
            {
                OpenInventory();
            }

            // ClosePanel 按键（无面板时无效）
            if (ManagerHub.Input.Actions.TryGetValue("ClosePanel", out var closePanelAction) &&
                closePanelAction.WasPressedThisFrame())
            {
                CloseCurrentPanel();
            }
        }

        // ========== Paused 状态 ==========
        if (CurrentState == GameState.Paused)
        {
            // ClosePanel 按键 → 先尝试关闭最上层面板，如果关闭成功则不再处理
            if (ManagerHub.Input.Actions.TryGetValue("ClosePanel", out var closePanelAction) &&
                closePanelAction.WasPressedThisFrame())
            {
                CloseCurrentPanel();
            }

            // CloseInventory 按键（暂停状态无效）
        }

        // ========== Interacting 状态 ==========
        if (CurrentState == GameState.Interacting)
        {
            // ClosePanel 按键 → 退出交互
            if (ManagerHub.Input.Actions.TryGetValue("ClosePanel", out var closePanelAction) &&
                closePanelAction.WasPressedThisFrame())
            {
                CloseCurrentPanel();
            }

            // CloseInventory 按键 → 退出交互
            if (ManagerHub.Input.Actions.TryGetValue("CloseInventory", out var closeInventoryAction) &&
                closeInventoryAction.WasPressedThisFrame())
            {
                CloseCurrentPanel();
            }
        }
    }

    /// <summary>
    /// 保存游玩时长到存档
    /// </summary>
    private void SavePlayTime()
    {
        if (_currentSessionSaveSlot >= 0 && _playTimeAccumulator > 0)
        {
            // 将累计的时间保存
            SaveLoadManager.Instance.AddPlayTime((long)_playTimeAccumulator);
            _playTimeAccumulator = 0f;
        }
    }

    /// <summary>
    /// 设置场景加载状态（外部调用）
    /// </summary>
    public void SetSceneLoading(bool loading)
    {
        _isSceneLoading = loading;
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
    /// 进入大厅的时间戳（用于计算游玩时长）
    /// </summary>
    private long _lobbyEnterTime;

    /// <summary>
    /// 进入 Interacting 状态前的游戏状态（用于恢复）
    /// </summary>
    private GameState _stateBeforeInteracting;

    /// <summary>
    /// 进入大厅（仅切换状态，不创建 session）
    /// </summary>
    /// <param name="saveSlot">存档槽位</param>
    public void EnterLobby(int saveSlot)
    {
        Debug.Log($"[EnterLobby] saveSlot={saveSlot}");
        _currentSessionSaveSlot = saveSlot;

        // 记录进入大厅的时间（用于计算游玩时长）
        _lobbyEnterTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 只进入 Lobby 状态（大厅/准备阶段）
        // Session 创建在 EnterDungeonPanel 中通过 StartSession 完成
        ChangeState(GameState.Lobby);
    }

    /// <summary>
    /// 创建大厅玩家（持有全局玩家数据）
    /// <para>由 LobbySceneInstaller 在 Lobby 场景加载后调用</para>
    /// </summary>
    /// <param name="spawnPos">出生点位置</param>
    public void CreateLobbyPlayer(Vector3 spawnPos)
    {
        Log($"CreateLobbyPlayer at {spawnPos}");

        // 创建大厅玩家处理器（PlayerHandler 会自动创建基础数据）
        _lobbyPlayerHandler = new PlayerHandler();
        // isLobbyPlayer=true 表示大厅玩家，不显示 HubPanel，但保持武器跟随鼠标
        _lobbyPlayerHandler.CreatePlayer(spawnPos, existingData: null, isLobbyPlayer: true);
    }

    /// <summary>
    /// 更新当前会话种子（在进入地牢前可修改）
    /// </summary>
    public void UpdateSessionSeed(long seed)
    {
        _currentSessionSeed = seed;
        Debug.Log($"[UpdateSessionSeed] seed updated to: {seed}");
    }

    /// <summary>
    /// 继续游戏（断点续玩）
    /// </summary>
    /// <param name="saveSlot">存档槽位</param>
    /// <param name="onProgress">加载进度回调</param>
    public void ContinueGame(int saveSlot, UnityAction<float> onProgress = null)
    {
        Log($"ContinueGame: slot={saveSlot}");

        // 确保选择正确的槽位
        SaveLoadManager.Instance.SelectSlot(saveSlot);

        // 从 sessionData 获取 seed 并加载到 SessionManager
        var sessionData = SaveLoadManager.Instance.CurrentSaveData?.sessionData;
        if (sessionData != null)
        {
            _currentSessionSeed = sessionData.seed;
            _currentSessionSaveSlot = saveSlot;
            Debug.Log($"[ContinueGame] sessionData: roleId={sessionData.selectedRoleId}, roleName={sessionData.selectedRoleName}, seed={sessionData.seed}, floor={sessionData.currentFloor}, currentCheckpoint={sessionData.currentCheckpoint != null}");
            // 加载 session 到 SessionManager
            SessionManager.Instance.LoadSession(sessionData);
            Log($"继续游戏: seed={_currentSessionSeed}, floor={sessionData.currentFloor}");
        }
        else
        {
            Debug.LogError("[ContinueGame] sessionData is null - no active session to continue!");
            return;
        }

        // 进入游戏（会加载 Game_Scene 并恢复 session）
        EnterPlaying(onProgress);
        EventCenter.Instance.Publish(GameEventID.OnSessionContinue);
    }

    /// <summary>
    /// 进入游戏
    /// <para>如果是继续游戏，根据 checkpoint 恢复对应层；如果是新游戏，从第0层开始</para>
    /// </summary>
    public void EnterPlaying(UnityAction<float> onProgress = null)
    {
        // 防止重复进入
        if (_isSceneLoading)
        {
            Log("EnterPlaying skipped: already loading");
            return;
        }

        Log($"EnterPlaying: Starting gameplay. CurrentState={CurrentState}");

        // 获取要进入的层索引（继续游戏时从 checkpoint 恢复）
        int targetFloor = 0;
        var checkpoint = SessionManager.Instance.GetCurrentCheckpoint();
        if (checkpoint != null)
        {
            targetFloor = checkpoint.floorIndex;
        }

        // 加载 Game_Scene（包含 A* 和地牢生成器），完成后实例化 LevelController
        _isSceneLoading = true;
        ManagerHub.Scene.LoadSceneAsync("Game_Scene", onProgress, () =>
        {
            Debug.Log("[EnterPlaying] Scene loaded callback executing...");
            // 实例化 LevelController
            if (_levelControllerPrefab != null)
            {
                LevelController = Instantiate(_levelControllerPrefab);
                LevelController.Initialize(_currentSessionSeed);
                LevelController.EnterLayer(targetFloor);
            }
            else
            {
                LogError("LevelController Prefab is not assigned!");
                return;
            }

            ChangeState(GameState.Playing);
            EventCenter.Instance.Publish(targetFloor == 0 ? GameEventID.OnSessionStart : GameEventID.OnSessionContinue);
            _isSceneLoading = false;
            Debug.Log("[EnterPlaying] Scene loaded callback completed");
        });
    }

    /// <summary>
    /// 主菜单点击开始游戏（跳过 Lobby 直接进入游戏）
    /// </summary>
    /// <param name="saveSlot">存档槽位</param>
    public void StartGameFromMenu(int saveSlot)
    {
        EnterLobby(saveSlot);
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

        // 隐藏游戏Hub
        ManagerHub.UI.HidePanel<HubPanel>();

        ChangeState(GameState.Lobby);
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        Log("ReturnToMainMenu");

        // 保存剩余的游玩时长
        if (_playTimeAccumulator > 0)
        {
            SaveLoadManager.Instance.AddPlayTime((long)_playTimeAccumulator);
            _playTimeAccumulator = 0f;
        }

        // 销毁关卡
        if (LevelController != null)
        {
            Destroy(LevelController.gameObject);
            LevelController = null;
        }

        // 大厅玩家由 LobbySceneInstaller 在 Lobby 场景销毁时自动清理
        // 无需在此处管理

        // 隐藏游戏Hub
        ManagerHub.UI.HidePanel<HubPanel>();

        ChangeState(GameState.MainMenu);
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Log("QuitGame");

        // 保存剩余的游玩时长
        if (_playTimeAccumulator > 0)
        {
            SaveLoadManager.Instance.AddPlayTime((long)_playTimeAccumulator);
            _playTimeAccumulator = 0f;
        }

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

        // 切换 BGM
        SwitchBGM(oldState, newState);

        // 通知状态变化
        OnStateChanged?.Invoke(newState);
    }

    private void SwitchBGM(GameState oldState, GameState newState)
    {
        if (ManagerHub.Audio == null)
            return;

        // Interacting、GameOver、Paused 状态不切换 BGM，保持当前播放
        if (newState == GameState.Interacting || newState == GameState.GameOver || newState == GameState.Paused)
            return;

        // 从 Interacting 恢复时也不切换 BGM，保持之前的 BGM 继续播放
        if (oldState == GameState.Interacting)
            return;

        // 从暂停恢复到之前的状态（Playing<->Lobby）不切换 BGM
        if (oldState == GameState.Paused)
        {
            // _wasInLobbyBeforePause 表示进入暂停前是否在 Lobby
            // 如果恢复到相同状态则不切换 BGM
            bool wasInLobbyBeforePause = _wasInLobbyBeforePause;
            bool returningToSameState = (wasInLobbyBeforePause && newState == GameState.Lobby) ||
                                       (!wasInLobbyBeforePause && newState == GameState.Playing);
            if (returningToSameState)
                return;
        }

        // 正常切换 BGM
        string bgmId = newState switch
        {
            GameState.MainMenu => _bgmMainMenu,
            GameState.Lobby => _bgmLobby,
            GameState.Playing => _bgmPlaying,
            _ => _bgmMainMenu
        };

        ManagerHub.Audio.PlayBGM(bgmId);
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing || CurrentState == GameState.Lobby)
        {
            _wasInLobbyBeforePause = (CurrentState == GameState.Lobby);
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
            ChangeState(_wasInLobbyBeforePause ? GameState.Lobby : GameState.Playing);
            ManagerHub.UI.HidePanel<PausePanel>();
            // 恢复玩家瞄准输入
            AimInput.Enabled = true;
        }
    }

    /// <summary>
    /// 打开背包
    /// </summary>
    public void OpenInventory()
    {
        // 只在 Playing 状态允许打开背包
        if (CurrentState == GameState.Playing)
        {
            // 保存当前状态，切换到交互状态
            _stateBeforeInteracting = CurrentState;
            ChangeState(GameState.Interacting);
            // 关闭玩家旋转
            AimInput.Enabled = false;
            // 锁定玩家移动，防止滑行
            LockPlayerMovement();
            // 禁用相机跟随
            CameraManager.Instance.DisableFollow();
            ManagerHub.UI.ShowPanel<InventoryPanel>();
        }
    }

    /// <summary>
    /// 打开商店
    /// </summary>
    public void OpenShop(UnityEngine.Events.UnityAction<global::ShopPanel> onShopShown = null)
    {
        // 只在 Playing 状态允许打开商店
        if (CurrentState == GameState.Playing)
        {
            // 保存当前状态，切换到交互状态
            _stateBeforeInteracting = CurrentState;
            ChangeState(GameState.Interacting);
            // 关闭玩家旋转
            AimInput.Enabled = false;
            // 锁定玩家移动，防止滑行
            LockPlayerMovement();
            // 禁用相机跟随
            CameraManager.Instance.DisableFollow();
            ManagerHub.UI.ShowPanel<ShopPanel>(UILayerType.Top, onShopShown);
        }
    }

    /// <summary>
    /// 锁定玩家移动（进入交互状态时调用）
    /// </summary>
    private void LockPlayerMovement()
    {
        Player player = null;

        // Playing 状态：通过 LevelController 获取玩家
        if (LevelController?.PlayerInstance != null)
        {
            player = LevelController.PlayerInstance.GetComponent<Player>();
        }
        // Lobby 状态：通过 Tag 查找玩家
        else
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            player = go?.GetComponent<Player>();
        }

        player?.LockMovement();
    }

    /// <summary>
    /// 解锁玩家移动（退出交互状态时调用）
    /// </summary>
    private void UnlockPlayerMovement()
    {
        Player player = null;

        // Playing 状态：通过 LevelController 获取玩家
        if (LevelController?.PlayerInstance != null)
        {
            player = LevelController.PlayerInstance.GetComponent<Player>();
        }
        // Lobby 状态：通过 Tag 查找玩家
        else
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            player = go?.GetComponent<Player>();
        }

        player?.UnlockMovement();
    }

    /// <summary>
    /// 关闭当前最上层的 UI 面板（仅响应可关闭的面板）
    /// </summary>
    public void CloseCurrentPanel()
    {
        // 防止递归调用
        if (_isClosingPanel)
            return;
        _isClosingPanel = true;

        Debug.Log($"[CloseCurrentPanel] called, CurrentState={CurrentState}");
        // 检查最上层面板是否可以通过 ClosePanel 关闭
        var topPanel = ManagerHub.UI.GetTopPanel();
        Debug.Log($"[CloseCurrentPanel] topPanel={topPanel?.GetType().Name}, CanBeClosed={topPanel?.CanBeClosedByClosePanel}");
        if (topPanel != null && !topPanel.CanBeClosedByClosePanel)
        {
            _isClosingPanel = false;
            return;
        }

        ManagerHub.UI.CloseTopPanel();
        _isClosingPanel = false;
    }

    private bool _isClosingPanel;

    /// <summary>
    /// 游戏结束（死亡）
    /// <para>调用 EndSession(false) 清空 session，session 结束后 checkpoint 不会被使用</para>
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
    /// <para>调用 EndSession(true) 更新 metaData 并清空 session</para>
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

        // 销毁所有UI面板（确保旧面板不会残留）
        ManagerHub.UI.HideAllPanels();

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

        // 恢复时间（以防从暂停状态重启）
        Time.timeScale = 1f;

        // 恢复瞄准输入（死亡时会被禁用）
        AimInput.Enabled = true;

        // 重新生成 seed 并创建新 session（全新开始）
        long newSeed = SaveLoadManager.Instance.GenerateSeed();
        int roleId = SaveLoadManager.Instance.CurrentSaveData?.sessionData?.selectedRoleId ?? 0;

        Debug.Log($"[RestartGame] Generating new seed={newSeed} for roleId={roleId}");
        SessionManager.Instance.StartSession(newSeed);
        UpdateSessionSeed(newSeed);

        Debug.Log("[RestartGame] Calling EnterLobby...");
        EnterLobby(_currentSessionSaveSlot);
        Debug.Log("[RestartGame] EnterLobby done. Calling EnterPlaying...");
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
    private void LogWarning(string msg) => Debug.LogWarning($"[GameLifecycleManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[GameLifecycleManager] {msg}");

    /// <summary>
    /// 应用退出时保存游玩时间（捕获强制退出、崩溃等情况）
    /// </summary>
    private void OnApplicationQuit()
    {
        if (_playTimeAccumulator > 0)
        {
            SaveLoadManager.Instance?.AddPlayTime((long)_playTimeAccumulator);
            _playTimeAccumulator = 0f;
        }
    }
}
