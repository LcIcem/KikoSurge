using System.Collections;
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
            // 确保 GameLifecycleManager 已初始化（防止 domain reload 后静态实例丢失）
            if (GameLifecycleManager.Instance != null)
            {
                GameLifecycleManager.Instance.EnterLobby(currentSlot);
            }
            else
            {
                Debug.LogWarning("[LoginPanel] GameLifecycleManager is null after scene load, retrying...");
                StartCoroutine(RetryEnterLobby(currentSlot));
            }
        });
    }

    private IEnumerator RetryEnterLobby(int slot)
    {
        yield return new WaitForSeconds(0.1f);
        if (GameLifecycleManager.Instance != null)
        {
            GameLifecycleManager.Instance.EnterLobby(slot);
        }
        else
        {
            Debug.LogError("[LoginPanel] GameLifecycleManager still null after retry!");
        }
    }
}
