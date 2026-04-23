using System.Collections;
using LcIcemFramework;
using UnityEngine;

/// <summary>
/// 登录/主菜单面板
/// </summary>
public class LoginPanel : BasePanel
{
    private const string BTN_START = "btn_start";
    private const string BTN_SAVE_SLOT = "btn_saveSlot";
    private const string BTN_SETTINGS = "btn_settings";
    private const string BTN_QUIT = "btn_quit";

    public override bool CanBeClosedByClosePanel => false;

    public override void Show()
    {
        base.Show();
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_START:
                OnStartClicked();
                break;
            case BTN_SAVE_SLOT:
                ManagerHub.UI.ShowPanel<SaveSlotPanel>();
                break;
            case BTN_SETTINGS:
                ManagerHub.UI.ShowPanel<SettingsPanel>();
                break;
            case BTN_QUIT:
                GameLifecycleManager.Instance.QuitGame();
                break;
        }
    }

    private void OnStartClicked()
    {
        int currentSlot = SaveLoadManager.Instance.CurrentSlotId;

        if (!SaveLoadManager.Instance.HasSaveData(currentSlot))
        {
            SaveLoadManager.Instance.CreateNewSave(currentSlot);
        }

        ManagerHub.UI.HidePanel<LoginPanel>();
        LoadingPanel panel = null;
        ManagerHub.UI.ShowPanel<LoadingPanel>(UILayerType.Top, p => panel = p);

        MonoManager.Instance.StartCoroutine(LoadLobbyScene(currentSlot, panel));
    }

    private IEnumerator LoadLobbyScene(int slot, LoadingPanel panel)
    {
        yield return new WaitForSeconds(0.1f);

        panel?.UpdateProgress(0.1f);

        bool done = false;
        ManagerHub.Scene.LoadSceneAsync("Lobby_Scene",
            p => panel?.UpdateProgress(Mathf.Lerp(0.1f, 0.6f, p)),
            () => done = true);

        yield return new WaitUntil(() => done);
        panel?.UpdateProgress(0.8f);
        yield return new WaitForSeconds(0.1f);
        panel?.UpdateProgress(1f);
        yield return new WaitForSeconds(0.2f);

        panel?.Hide();
        ManagerHub.UI.HidePanel<LoadingPanel>();

        if (GameLifecycleManager.Instance != null)
        {
            GameLifecycleManager.Instance.EnterLobby(slot);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
            if (GameLifecycleManager.Instance != null)
                GameLifecycleManager.Instance.EnterLobby(slot);
        }
    }
}
