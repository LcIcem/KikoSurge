using LcIcemFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录/主菜单面板
/// </summary>
public class LoginPanel : BasePanel
{
    private const string BTN_START = "btn_start";
    private const string BTN_SAVE_SLOT = "btn_saveSlot";
    private const string BTN_SETTINGS = "btn_settings";
    private const string BTN_QUIT = "btn_quit";

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

        // 检测当前槽位是否有存档
        if (!SaveLoadManager.Instance.HasSaveData(currentSlot))
        {
            // 没有存档，创建新存档到当前槽位
            SaveLoadManager.Instance.CreateNewSave(currentSlot);
        }

        // 进入大厅
        ManagerHub.UI.HidePanel<LoginPanel>();
        ManagerHub.Scene.LoadSceneAsync("Lobby_Scene", null, () =>
        {
            GameLifecycleManager.Instance.EnterLobby(currentSlot);
        });
    }
}
