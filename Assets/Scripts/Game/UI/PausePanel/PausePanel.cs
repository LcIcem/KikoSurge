using LcIcemFramework;
using UnityEngine;

public class PausePanel : BasePanel
{
    private const string BTN_RESUME = "btn_resume";
    private const string BTN_SETTINGS = "btn_settings";
    private const string BTN_BACK_TO_MAINMENU = "btn_backToMainmenu";

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0;
    }

    public override void Hide()
    {
        base.Hide();
        Time.timeScale = 1f;
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_RESUME:
                GameLifecycleManager.Instance.ResumeGame();
                break;
            case BTN_SETTINGS:
                ManagerHub.UI.ShowPanel<SettingsPanel>();
                break;
            case BTN_BACK_TO_MAINMENU:
                ManagerHub.UI.HidePanel<PausePanel>();
                GameLifecycleManager.Instance.ReturnToMainMenu();
                ManagerHub.Scene.LoadSceneAsync("MainMenu_Scene");
                break;
        }
    }
}