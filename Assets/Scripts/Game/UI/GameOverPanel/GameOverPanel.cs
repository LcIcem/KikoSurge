using LcIcemFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏结束面板
/// <para>用于两种情况：玩家死亡（显示"游戏结束"）和通关（显示"通关成功"）</para>
/// </summary>
public class GameOverPanel : BasePanel
{
    // 控件名称常量
    private const string TXT_TITLE = "txt_title";
    private const string BTN_RESTART = "btn_restart";
    private const string BTN_BACK_TO_MAINMENU = "btn_backToMainmenu";

    // true = 死亡（游戏结束），false = 通关
    private bool _isGameOver = true;

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0;
        UpdateTitle();
    }

    public override void Hide()
    {
        base.Hide();
        Time.timeScale = 1f;
        _isGameOver = true;
    }

    /// <summary>
    /// 显示为游戏结束（死亡）
    /// </summary>
    public void ShowAsGameOver()
    {
        _isGameOver = true;
        Show();
    }

    /// <summary>
    /// 显示为通关成功
    /// </summary>
    public void ShowAsClear()
    {
        _isGameOver = false;
        Show();
    }

    private void UpdateTitle()
    {
        var titleText = GetControl<Text>(TXT_TITLE);
        if (titleText != null)
        {
            titleText.text = _isGameOver ? "游戏结束" : "通关成功";
        }
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_RESTART:
                GameLifecycleManager.Instance.RestartGame();
                break;
            case BTN_BACK_TO_MAINMENU:
                ManagerHub.UI.HidePanel<GameOverPanel>();
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
