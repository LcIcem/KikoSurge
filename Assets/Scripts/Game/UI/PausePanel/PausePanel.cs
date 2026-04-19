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

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0f;

        // 暂停时禁止玩家瞄准输入
        AimInput.Enabled = false;

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

        // 大厅暂停时显示"返回大厅"按钮（需要确认返回）
        var lobbyBtn = GetControl<UnityEngine.UI.Button>(BTN_BACK_TO_LOBBY);
        if (lobbyBtn != null)
        {
            if (isFromLobby)
            {
                // 大厅暂停：将"返回大厅"按钮移到最上面（sibling index 0）
                lobbyBtn.transform.SetSiblingIndex(0);
            }
            lobbyBtn.gameObject.SetActive(true);
        }
    }

    public override void Hide()
    {
        Time.timeScale = 1f;

        // 隐藏时恢复玩家瞄准输入
        AimInput.Enabled = true;

        // 隐藏时重置所有按钮和标题为显示状态
        var resumeBtn = GetControl<UnityEngine.UI.Button>(BTN_RESUME);
        var lobbyBtn = GetControl<UnityEngine.UI.Button>(BTN_BACK_TO_LOBBY);
        var settingsBtn = GetControl<UnityEngine.UI.Button>(BTN_SETTINGS);
        var mainMenuBtn = GetControl<UnityEngine.UI.Button>(BTN_BACK_TO_MAINMENU);
        var titleText = GetControl<UnityEngine.UI.Text>(TXT_TITLE);
        if (resumeBtn != null) resumeBtn.gameObject.SetActive(true);
        if (lobbyBtn != null)
        {
            lobbyBtn.gameObject.SetActive(true);
            // 恢复按钮原始顺序：resume(0), settings(1), lobby(2), mainMenu(3)
            lobbyBtn.transform.SetSiblingIndex(2);
        }
        if (settingsBtn != null) settingsBtn.gameObject.SetActive(true);
        if (mainMenuBtn != null) mainMenuBtn.gameObject.SetActive(true);
        if (titleText != null) titleText.gameObject.SetActive(true);

        base.Hide();
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
