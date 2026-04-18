using UnityEngine;
using LcIcemFramework;
using LcIcemFramework.Core;

/// <summary>
/// 大厅"开始游戏"交互行为
/// <para>挂载在与 Interactable 组件相同的 GameObject 上</para>
/// </summary>
public class LobbyStartGame : MonoBehaviour
{
    [SerializeField] private Interactable _interactable;

    private void Start()
    {
        _interactable.OnInteract += OnInteractTriggered;
    }

    private void OnInteractTriggered()
    {
        // 切换到 UI ActionMap（玩家输入被 UI 接管）
        ManagerHub.Input.SwitchActionMap("UI");

        // 显示进入地牢面板
        ManagerHub.UI.ShowPanel<EnterDungeonPanel>(UILayerType.Top, panel =>
        {
            panel.OnEnterDungeon += OnEnterDungeon;
            panel.OnDungeonPanelClosed += OnPanelClosed;
        });
    }

    private void OnEnterDungeon(long seed)
    {
        if (seed == 0)
        {
            // seed = 0 表示继续游戏
            GameLifecycleManager.Instance.ContinueGame(SaveLoadManager.Instance.CurrentSlotId);
        }
        else
        {
            // 非 0 seed 表示开始新游戏
            // 从当前选择的角色获取正确的 roleId（不是列表索引）
            int roleIndex = GameDataManager.Instance.CurSelRoleIndex;
            var roleData = GameDataManager.Instance.GetRoleStaticDataByCurSel();
            int roleId = roleData?.roleId ?? roleIndex;

            // 保存选择的角色ID到 SaveLoadManager（用于后续开始游戏时创建角色）
            SaveLoadManager.Instance.SetLastSelectedRoleId(roleId);

            // SessionManager 内部会使用 SaveLoadManager.LastSelectedRoleId 创建角色
            SessionManager.Instance.StartSession(seed);
            GameLifecycleManager.Instance.UpdateSessionSeed(seed);
            GameLifecycleManager.Instance.EnterPlaying();
        }
    }

    private void OnPanelClosed()
    {
        // 返回大厅，切换回 Player ActionMap
        ManagerHub.Input.SwitchActionMap("Player");

        // 重新显示交互提示
        _interactable.ResumePrompt();
    }
}
