using System.Collections;
using LcIcemFramework;
using LcIcemFramework.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

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
        AimInput.Enabled = false;

        bool isFromLobby = GameLifecycleManager.Instance.WasInLobbyBeforePause;

        var resumeBtn = GetControl<UnityEngine.UI.Button>(BTN_RESUME);
        if (resumeBtn != null)
            resumeBtn.gameObject.SetActive(!isFromLobby);

        var titleText = GetControl<UnityEngine.UI.Text>(TXT_TITLE);
        if (titleText != null)
            titleText.gameObject.SetActive(!isFromLobby);

        var lobbyBtn = GetControl<UnityEngine.UI.Button>(BTN_BACK_TO_LOBBY);
        if (lobbyBtn != null)
        {
            if (isFromLobby)
                lobbyBtn.transform.SetSiblingIndex(0);
            lobbyBtn.gameObject.SetActive(true);
        }
    }

    public override void Hide()
    {
        Time.timeScale = 1f;
        AimInput.Enabled = true;

        var resumeBtn = GetControl<UnityEngine.UI.Button>(BTN_RESUME);
        var lobbyBtn = GetControl<UnityEngine.UI.Button>(BTN_BACK_TO_LOBBY);
        var settingsBtn = GetControl<UnityEngine.UI.Button>(BTN_SETTINGS);
        var mainMenuBtn = GetControl<UnityEngine.UI.Button>(BTN_BACK_TO_MAINMENU);
        var titleText = GetControl<UnityEngine.UI.Text>(TXT_TITLE);
        if (resumeBtn != null) resumeBtn.gameObject.SetActive(true);
        if (lobbyBtn != null)
        {
            lobbyBtn.gameObject.SetActive(true);
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
                GameLifecycleManager.Instance.ResumeGame();
                break;
            case BTN_BACK_TO_LOBBY:
                GameLifecycleManager.Instance.ReturnToLobby();
                ManagerHub.UI.HidePanel<PausePanel>();
                MonoManager.Instance.StartCoroutine(LoadSceneWithLoading("Lobby_Scene", null));
                break;
            case BTN_SETTINGS:
                ManagerHub.UI.ShowPanel<SettingsPanel>();
                break;
            case BTN_BACK_TO_MAINMENU:
                GameLifecycleManager.Instance.ReturnToMainMenu();
                ManagerHub.UI.HidePanel<PausePanel>();
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
}
