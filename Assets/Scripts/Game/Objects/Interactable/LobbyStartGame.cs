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

        // 显示种子输入面板
        ManagerHub.UI.ShowPanel<EnterDungeonPanel>(UILayerType.Top, panel =>
        {
            panel.OnEnterDungeon += OnEnterDungeon;
            panel.OnDungeonPanelClosed += OnPanelClosed;
        });
    }

    private void OnEnterDungeon(long seed)
    {
        // 使用指定种子开始游戏
        GameLifecycleManager.Instance.UpdateSessionSeed(seed);
        GameLifecycleManager.Instance.EnterPlaying();
    }

    private void OnPanelClosed()
    {
        // 返回大厅，切换回 Player ActionMap
        ManagerHub.Input.SwitchActionMap("Player");

        // 重新显示交互提示
        _interactable.ResumePrompt();
    }
}
