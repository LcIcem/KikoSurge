using LcIcemFramework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PausePanel : BasePanel
{
    private const string BTN_RESUME = "btn_resume";
    private const string BTN_BACK_TO_LOBBY = "btn_backToLobby";
    private const string BTN_SETTINGS = "btn_settings";
    private const string BTN_BACK_TO_MAINMENU = "btn_backToMainmenu";
    private const string TXT_TITLE = "txt_title";

    private InputAction _resumeAction;

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0f;

        bool isFromLobby = GameLifecycleManager.Instance.WasInLobbyBeforePause;

        // 大厅暂停(3按钮): 返回大厅、游戏设置、返回主菜单
        // 游戏暂停(4按钮): 继续游戏、游戏设置、返回大厅、返回主菜单
        // resume 按钮在大厅暂停时隐藏
        var resumeBtn = GetControl<UnityEngine.UI.Button>(BTN_RESUME);
        if (resumeBtn != null)
            resumeBtn.gameObject.SetActive(!isFromLobby);

        // 大厅暂停时隐藏标题（只有游戏中才显示"暂停"）
        var titleText = GetControl<UnityEngine.UI.Text>(TXT_TITLE);
        if (titleText != null)
            titleText.gameObject.SetActive(!isFromLobby);

        // 订阅 Resume 动作（ESC）
        _resumeAction = ManagerHub.Input?.GetInputActionFromMap("UI", "Resume");
        if (_resumeAction != null)
            _resumeAction.performed += OnResumePerformed;
    }

    public override void Hide()
    {
        if (_resumeAction != null)
        {
            _resumeAction.performed -= OnResumePerformed;
            _resumeAction = null;
        }

        Time.timeScale = 1f;

        // 隐藏时重置所有按钮和标题为显示状态
        var resumeBtn = GetControl<UnityEngine.UI.Button>(BTN_RESUME);
        var lobbyBtn = GetControl<UnityEngine.UI.Button>(BTN_BACK_TO_LOBBY);
        var settingsBtn = GetControl<UnityEngine.UI.Button>(BTN_SETTINGS);
        var mainMenuBtn = GetControl<UnityEngine.UI.Button>(BTN_BACK_TO_MAINMENU);
        var titleText = GetControl<UnityEngine.UI.Text>(TXT_TITLE);
        if (resumeBtn != null) resumeBtn.gameObject.SetActive(true);
        if (lobbyBtn != null) lobbyBtn.gameObject.SetActive(true);
        if (settingsBtn != null) settingsBtn.gameObject.SetActive(true);
        if (mainMenuBtn != null) mainMenuBtn.gameObject.SetActive(true);
        if (titleText != null) titleText.gameObject.SetActive(true);

        base.Hide();
    }

    /// <summary>
    /// ESC 按下：如果是唯一面板则恢复游戏，否则只隐藏自己
    /// </summary>
    private void OnResumePerformed(InputAction.CallbackContext ctx)
    {
        // 检查是否有其他面板显示在 PausePanel 之上
        if (GameLifecycleManager.Instance.HasChildPanelOpen)
        {
            // 有子面板，只隐藏自己
            ManagerHub.UI.HidePanel<PausePanel>();
        }
        else
        {
            // 没有子面板，恢复游戏
            ManagerHub.UI.HidePanel<PausePanel>();
            GameLifecycleManager.Instance.ResumeGame();
        }
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_RESUME:
                // 恢复游戏并隐藏面板
                GameLifecycleManager.Instance.ResumeGame();
                break;
            case BTN_BACK_TO_LOBBY:
                // 返回大厅
                ManagerHub.UI.HidePanel<PausePanel>();
                GameLifecycleManager.Instance.SetSceneLoading(true);
                ManagerHub.Scene.LoadSceneAsync("Lobby_Scene", null, () =>
                {
                    GameLifecycleManager.Instance.ReturnToLobby();
                    GameLifecycleManager.Instance.SetSceneLoading(false);
                });
                break;
            case BTN_SETTINGS:
                ManagerHub.UI.ShowPanel<SettingsPanel>();
                break;
            case BTN_BACK_TO_MAINMENU:
                ManagerHub.UI.HidePanel<PausePanel>();
                GameLifecycleManager.Instance.ReturnToMainMenu();
                GameLifecycleManager.Instance.SetSceneLoading(true);
                ManagerHub.Scene.LoadSceneAsync("MainMenu_Scene", null, () =>
                {
                    GameLifecycleManager.Instance.SetSceneLoading(false);
                });
                break;
        }
    }
}
