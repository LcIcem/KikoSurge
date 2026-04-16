using LcIcemFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录/主菜单面板
/// </summary>
public class LoginPanel : BasePanel
{
    private const string BTN_NEW_GAME = "btn_newGame";
    private const string BTN_CONTINUE = "btn_continue";
    private const string BTN_SETTINGS = "btn_settings";
    private const string BTN_QUIT = "btn_quit";

    public override void Show()
    {
        base.Show();
        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        bool hasSave = ManagerHub.Save.Exists(0);
        var btn = GetControl<Button>(BTN_CONTINUE);
        if (btn != null)
            btn.interactable = hasSave;
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_NEW_GAME:
                ManagerHub.Scene.LoadSceneAsync("Game_Scene", null, () =>
                {
                    GameLifecycleManager.Instance.StartNewGame(0);
                    GameLifecycleManager.Instance.EnterPlaying();
                });
                ManagerHub.UI.HidePanel<LoginPanel>();
                break;
            case BTN_CONTINUE:
                if (ManagerHub.Save.Exists(0))
                {
                    ManagerHub.Scene.LoadSceneAsync("Game_Scene", null, () =>
                    {
                        GameLifecycleManager.Instance.ContinueGame(0);
                    });
                }
                break;
            case BTN_SETTINGS:
                ManagerHub.UI.ShowPanel<SettingsPanel>();
                break;
            case BTN_QUIT:
                GameLifecycleManager.Instance.QuitGame();
                break;
        }
    }
}
