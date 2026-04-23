using System.Collections;
using UnityEngine;
using LcIcemFramework;
using LcIcemFramework.Core;
using Game.Util;

/// <summary>
/// 大厅"开始游戏"交互行为
/// <para>挂载在与 Interactable 组件相同的 GameObject 上</para>
/// </summary>
public class LobbyStartGame : MonoBehaviour
{
    [SerializeField] private Interactable _interactable;
    private EnterDungeonPanel _enterDungeonPanel;

    private void Start()
    {
        _interactable.SetHintText("进入[{0}]");
        _interactable.OnInteract += OnInteractTriggered;
    }

    private void OnInteractTriggered()
    {
        _interactable.ShowInfoCard(false);
        Player.StartInteraction(_interactable);

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
        player?.LockMovement();

        AimInput.Enabled = false;
        ManagerHub.Input.SwitchActionMap("UI");

        ManagerHub.UI.ShowPanel<EnterDungeonPanel>(UILayerType.Top, panel =>
        {
            _enterDungeonPanel = panel;
            panel.OnEnterDungeon += OnEnterDungeon;
            panel.OnDungeonPanelClosed += OnPanelClosed;
        });
    }

    private void OnEnterDungeon(long seed)
    {
        if (_enterDungeonPanel != null)
        {
            _enterDungeonPanel.OnDungeonPanelClosed -= OnPanelClosed;
        }

        Player.EndInteraction();
        AimInput.Enabled = true;

        LoadingPanel panel = null;
        ManagerHub.UI.ShowPanel<LoadingPanel>(UILayerType.Top, p => panel = p);

        MonoManager.Instance.StartCoroutine(LoadingSequence(seed, panel));
    }

    private IEnumerator LoadingSequence(long seed, LoadingPanel panel)
    {
        // 显示 loading
        if (panel != null)
            panel.UpdateProgress(1f);

        // 触发场景加载
        if (seed == 0)
        {
            GameLifecycleManager.Instance.ContinueGame(SaveLoadManager.Instance.CurrentSlotId);
        }
        else
        {
            int roleIndex = GameDataManager.Instance.CurSelRoleIndex;
            var roleData = GameDataManager.Instance.GetRoleStaticDataByCurSel();
            int roleId = roleData?.roleId ?? roleIndex;

            SaveLoadManager.Instance.SetLastSelectedRoleId(roleId);
            SessionManager.Instance.StartSession(seed);
            GameLifecycleManager.Instance.UpdateSessionSeed(seed);
            GameLifecycleManager.Instance.EnterPlaying();
        }

        // 等待场景激活完成
        yield return new WaitUntil(() => !GameLifecycleManager.Instance.IsSceneLoading);

        // 隐藏 loading
        if (panel != null)
        {
            panel.Hide();
            ManagerHub.UI.HidePanel<LoadingPanel>();
        }
    }

    private void OnPanelClosed()
    {
        Player.EndInteraction();
        ManagerHub.Input.SwitchActionMap("Player");

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
        player?.UnlockMovement();

        AimInput.Enabled = true;

        if (_interactable != null)
            _interactable.ResumePrompt();
    }
}
