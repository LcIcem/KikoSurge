using System;
using LcIcemFramework;
using LcIcemFramework.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 进入地牢面板 - 大厅交互面板
/// <para>在大厅显示，提供继续游戏和开始新游戏选项</para>
/// </summary>
public class EnterDungeonPanel : BasePanel
{
    // 控件名称常量
    private const string BTN_CONTINUE = "btn_continue";
    private const string BTN_NEW_GAME = "btn_newGame";
    private const string BTN_BACK = "btn_back";
    private const string INPUT_SEED = "input_seed";

    // 事件
    public event Action<long> OnEnterDungeon;
    public event Action OnDungeonPanelClosed;

    public override void Show()
    {
        base.Show();
        gameObject.SetActive(true);
        RefreshButtonStates();
    }

    public override void Hide()
    {
        base.Hide();
        gameObject.SetActive(false);
    }

    private void RefreshButtonStates()
    {
        bool hasActiveSession = SaveLoadManager.Instance.HasActiveSession;

        var continueBtn = GetControl<Button>(BTN_CONTINUE);
        if (continueBtn != null)
            continueBtn.interactable = hasActiveSession;

        var newGameBtn = GetControl<Button>(BTN_NEW_GAME);
        if (newGameBtn != null)
            newGameBtn.interactable = true;
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_CONTINUE:
                OnContinueClicked();
                break;
            case BTN_NEW_GAME:
                OnNewGameClicked();
                break;
            case BTN_BACK:
                OnBackClicked();
                break;
        }
    }

    private void OnContinueClicked()
    {
        if (!SaveLoadManager.Instance.HasActiveSession)
        {
            Debug.LogWarning("[EnterDungeonPanel] 没有进行中的游戏，无法继续");
            return;
        }

        Hide();
        OnEnterDungeon?.Invoke(0); // seed = 0 表示继续，用存档中的 seed
    }

    private void OnNewGameClicked()
    {
        long seed = Environment.TickCount;

        var input = GetControl<InputField>(INPUT_SEED);
        if (input != null && !string.IsNullOrWhiteSpace(input.text))
        {
            if (long.TryParse(input.text, out long parsedSeed))
            {
                seed = parsedSeed;
            }
            else
            {
                Debug.LogWarning("[EnterDungeonPanel] 种子解析失败，使用随机种子");
            }
        }

        Hide();
        OnEnterDungeon?.Invoke(seed);
    }

    private void OnBackClicked()
    {
        Hide();
        OnDungeonPanelClosed?.Invoke();
    }
}
