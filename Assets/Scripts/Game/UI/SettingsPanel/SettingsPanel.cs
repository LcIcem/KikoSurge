using System.Collections.Generic;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.UI;
using UnityEngine;

/// <summary>
/// 设置面板：音频设置 + 键位设置预留
/// </summary>
public class SettingsPanel : BasePanel
{
    // 控件名称常量
    private const string BTN_CLOSE = "btn_close";
    private const string BTN_CATEGORY_AUDIO = "btn_category_audio";
    private const string BTN_CATEGORY_KEYS = "btn_category_keys";

    // 设置面板名称
    private const string PANEL_AUDIO = "panel_audio";
    private const string PANEL_KEYS = "panel_keys";

    // 所有设置页面容器
    private Dictionary<string, ISettingsPage> _pages = new Dictionary<string, ISettingsPage>();

    // 当前设置的页面
    private ISettingsPage _curPage;

    #region 生命周期

    protected override void Awake()
    {
        base.Awake();
        RegisterPages();
    }

    public override void Show()
    {
        base.Show();
        // 默认显示第一个注册的页面
        var enumerator = _pages.GetEnumerator();
        if (enumerator.MoveNext())
            ShowSettings(enumerator.Current.Key);
    }

    #endregion

    #region 页面注册

    private void RegisterPages()
    {
        var audioPage = new AudioSettingsPage();
        audioPage.Init(this);
        _pages[PANEL_AUDIO] = audioPage;

        var keysPage = new KeysSettingsPage();
        keysPage.Init(this);
        _pages[PANEL_KEYS] = keysPage;
    }

    #endregion

    #region 事件处理

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_CLOSE:
                ManagerHub.UI.HidePanel<SettingsPanel>();
                break;
            case BTN_CATEGORY_AUDIO:
                ShowSettings(PANEL_AUDIO);
                break;
            case BTN_CATEGORY_KEYS:
                ShowSettings(PANEL_KEYS);
                break;
        }
    }

    protected override void OnTogValueChanged(string togName, bool value)
    {
        _curPage?.OnTogValueChanged(togName, value);
    }

    protected override void OnSliderValueChanged(string sliderName, float value)
    {
        _curPage?.OnSliderValueChanged(sliderName, value);
    }

    #endregion

    #region 分类切换

    private void ShowSettings(string pageName)
    {
        // 激活目标页面
        foreach (var page in _pages)
        {
            print(page.Key);
            var panelObj = transform.Find("RightPanel/" + page.Key)?.gameObject;
            if (panelObj != null)
                panelObj.SetActive(page.Key == pageName);
        }

        // 触发页面进入/退出
        if (_pages.TryGetValue(pageName, out var newPage) && newPage != _curPage)
        {
            _curPage?.OnExit();
            _curPage = newPage;
            _curPage?.OnEnter();
        }
    }

    #endregion
}
