using System;
using LcIcemFramework;
using LcIcemFramework.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 进入地牢面板 - 种子输入
/// <para>在大厅显示，允许玩家输入自定义种子（留空则使用随机种子）</para>
/// </summary>
public class EnterDungeonPanel : BasePanel
{
    // 控件名称常量
    private const string BTN_ENTER = "btn_enter";
    private const string BTN_BACK = "btn_back";
    private const string INPUT_SEED = "input_seed";

    // 事件
    public event Action<long> OnEnterDungeon;
    public event Action OnDungeonPanelClosed;

    public override void Show()
    {
        base.Show();
        gameObject.SetActive(true);
    }

    public override void Hide()
    {
        base.Hide();
        gameObject.SetActive(false);
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_ENTER:
                OnEnterClicked();
                break;
            case BTN_BACK:
                OnBackClicked();
                break;
        }
    }

    private void OnEnterClicked()
    {
        long seed = Environment.TickCount; // 默认随机种子

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

        Debug.Log($"[EnterDungeonPanel] 进入地牢，种子: {seed}");

        // 先隐藏面板，再触发事件（事件会触发场景加载）
        Hide();
        OnEnterDungeon?.Invoke(seed);
    }

    private void OnBackClicked()
    {
        Debug.Log("[EnterDungeonPanel] 返回大厅");
        Hide();
        OnDungeonPanelClosed?.Invoke();
    }
}
