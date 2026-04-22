using System;
using LcIcemFramework;
using LcIcemFramework.Core;
using UnityEngine;

/// <summary>
/// 自定义光标管理器
/// <para>使用 Unity Cursor API 切换准心/光标图片，无需 UI Image 跟随</para>
/// </summary>
public class CursorManager : SingletonMono<CursorManager>
{
    [Header("光标配置（拖入 Texture2D）")]
    public Texture2D aimTexture;       // 游戏中的准心
    public Texture2D cursorTexture;  // 主菜单/UI 时的光标

    [Header("运行时调试（可调整）")]
    [Range(16f, 128f)]
    public float cursorSize = 32f;  // 光标图片的像素尺寸（正方形）

    private bool _isInGame;
    private bool _isUIPanelOpen;

    protected override void Init()
    {
        try
        {
            TrySubscribeEvents();
            ApplyCursorState();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void Update()
    {
        if (!_subscriptionsReady)
            TrySubscribeEvents();
    }

    private bool _subscriptionsReady;

    private void TrySubscribeEvents()
    {
        if (_subscriptionsReady) return;
        if (GameLifecycleManager.Instance == null) return;
        if (ManagerHub.UI == null) return;

        GameLifecycleManager.Instance.OnStateChanged += OnGameStateChanged;
        ManagerHub.UI.OnAnyPanelShown += OnAnyPanelShown;
        ManagerHub.UI.OnAnyPanelHidden += OnAnyPanelHidden;

        _subscriptionsReady = true;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 恢复系统默认光标
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (GameLifecycleManager.Instance != null)
            GameLifecycleManager.Instance.OnStateChanged -= OnGameStateChanged;
        if (ManagerHub.Instance != null && ManagerHub.UI != null)
        {
            ManagerHub.UI.OnAnyPanelShown -= OnAnyPanelShown;
            ManagerHub.UI.OnAnyPanelHidden -= OnAnyPanelHidden;
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        // 进入 Lobby 或 Playing 时，重置 UI 面板状态（进入游戏时可能有瞬时面板动画，不影响）
        if (newState == GameState.Lobby || newState == GameState.Playing)
            _isUIPanelOpen = false;

        SetInGameMode(newState == GameState.Lobby || newState == GameState.Playing);
    }

    private void OnAnyPanelShown()
    {
        _isUIPanelOpen = true;
        ApplyCursorState();
    }

    private void OnAnyPanelHidden()
    {
        _isUIPanelOpen = false;
        ApplyCursorState();
    }

    public void SetInGameMode(bool inGame)
    {
        _isInGame = inGame;
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        bool isGame = _isInGame && !_isUIPanelOpen;

        if (isGame)
        {
            Cursor.lockState = CursorLockMode.Confined;
            if (aimTexture != null)
                Cursor.SetCursor(aimTexture, new Vector2(cursorSize / 2f, cursorSize / 2f), CursorMode.ForceSoftware);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            if (cursorTexture != null)
                Cursor.SetCursor(cursorTexture, new Vector2(0, cursorSize), CursorMode.ForceSoftware);
        }
    }
}
