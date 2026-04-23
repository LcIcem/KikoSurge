using System.Collections;
using LcIcemFramework;
using LcIcemFramework.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Game.Event;

/// <summary>
/// 游戏结束面板
/// <para>用于两种情况：玩家死亡（显示"游戏结束"）和通关（显示"通关成功"）</para>
/// </summary>
public class GameOverPanel : BasePanel
{
    private const string TXT_TITLE = "txt_title";
    private const string BTN_RESTART = "btn_restart";
    private const string BTN_BACK_TO_LOBBY = "btn_backToLobby";
    private const string BTN_BACK_TO_MAINMENU = "btn_backToMainmenu";

    private bool _isGameOver = true;

    public override bool CanBeClosedByClosePanel => false;

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0;
        AimInput.Enabled = false;
        UpdateTitle();
    }

    public override void Hide()
    {
        base.Hide();
        Time.timeScale = 1f;
        AimInput.Enabled = true;
        _isGameOver = true;
    }

    public void ShowAsGameOver()
    {
        _isGameOver = true;
        Show();
    }

    public void ShowAsClear()
    {
        _isGameOver = false;
        Show();
    }

    private void UpdateTitle()
    {
        var titleText = GetControl<Text>(TXT_TITLE);
        if (titleText != null)
            titleText.text = _isGameOver ? "游戏结束" : "通关成功";
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_RESTART:
                ManagerHub.UI.HidePanel<GameOverPanel>();
                MonoManager.Instance.StartCoroutine(RestartWithLoading());
                break;
            case BTN_BACK_TO_LOBBY:
                ManagerHub.UI.HidePanel<GameOverPanel>();
                GameLifecycleManager.Instance.ReturnToLobby();
                MonoManager.Instance.StartCoroutine(LoadSceneWithLoading("Lobby_Scene", null));
                break;
            case BTN_BACK_TO_MAINMENU:
                ManagerHub.UI.HidePanel<GameOverPanel>();
                GameLifecycleManager.Instance.ReturnToMainMenu();
                MonoManager.Instance.StartCoroutine(LoadSceneWithLoading("MainMenu_Scene", null));
                break;
        }
    }

    private IEnumerator LoadSceneWithLoading(string sceneName, UnityAction onComplete)
    {
        LoadingPanel panel = null;
        ManagerHub.UI.ShowPanel<LoadingPanel>(UILayerType.Top, p => panel = p);
        yield return new WaitForSeconds(0.1f);

        panel?.UpdateProgress(0.1f);

        bool done = false;
        ManagerHub.Scene.LoadSceneAsync(sceneName,
            p => panel?.UpdateProgress(Mathf.Lerp(0.1f, 0.7f, p)),
            () => done = true);

        yield return new WaitUntil(() => done);
        panel?.UpdateProgress(1f);
        yield return new WaitForSeconds(0.2f);

        panel?.Hide();
        ManagerHub.UI.HidePanel<LoadingPanel>();
        onComplete?.Invoke();
    }

    private IEnumerator RestartWithLoading()
    {
        LoadingPanel panel = null;
        ManagerHub.UI.ShowPanel<LoadingPanel>(UILayerType.Top, p => panel = p);
        yield return new WaitForSeconds(0.1f);

        panel?.UpdateProgress(0.15f);
        yield return new WaitForSeconds(0.3f);

        GameLifecycleManager.Instance.RestartGame();

        panel?.UpdateProgress(0.5f);
        yield return new WaitForSeconds(0.3f);

        panel?.UpdateProgress(0.7f);
        yield return new WaitForSeconds(0.3f);

        panel?.UpdateProgress(1f);
        yield return new WaitForSeconds(0.2f);

        panel?.Hide();
        ManagerHub.UI.HidePanel<LoadingPanel>();
    }
}
