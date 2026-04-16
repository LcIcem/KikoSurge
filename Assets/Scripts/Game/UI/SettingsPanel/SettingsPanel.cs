using System.Collections.Generic;
using LcIcemFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板：音频设置 + 键位设置
/// </summary>
public class SettingsPanel : BasePanel
{
    private const string BTN_CLOSE = "btn_close";
    private const string BTN_CATEGORY_AUDIO = "btn_category_audio";
    private const string BTN_CATEGORY_KEYS = "btn_category_keys";

    // 键位页拖拽配置（仅 KeysSettingsPage 需要）
    [Header("键位页配置")]
    [SerializeField] private RectTransform _keysContentContainer;
    [SerializeField] private GameObject _keysItemTemplate;
    [SerializeField] private Button _keysBtnResetAll;

    // 页面实例（代码创建）
    private readonly ISettingsPage _audioPage = new AudioSettingsPage();
    private readonly ISettingsPage _keysPage = new KeysSettingsPage();

    private ISettingsPage _curPage;

    #region 生命周期

    protected override void Awake()
    {
        base.Awake();
        SetupPages();
    }

    public override void Show()
    {
        base.Show();
        ShowSettings(_audioPage.PageKey);
    }

    public override void Hide()
    {
        _curPage?.OnExit();
        base.Hide();
    }


    #endregion

    #region 页面初始化

    private void SetupPages()
    {
        Debug.Log($"[SettingsPanel] SetupPages. keysContainer={_keysContentContainer}, template={_keysItemTemplate}");

        _audioPage.Init(this);

        _keysPage.Init(this);
        if (_keysPage is KeysSettingsPage keys)
            keys.SetDraggedRefs(_keysContentContainer, _keysItemTemplate, _keysBtnResetAll);
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
                ShowSettings(_audioPage.PageKey);
                break;
            case BTN_CATEGORY_KEYS:
                ShowSettings(_keysPage.PageKey);
                break;
            default:
                _curPage?.OnClick(btnName);
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
        Debug.Log($"[SettingsPanel] ShowSettings: {pageName}");

        var panelAudio = transform.Find("RightPanel/panel_audio")?.gameObject;
        var panelKeys = transform.Find("RightPanel/panel_keys")?.gameObject;
        if (panelAudio != null) panelAudio.SetActive(pageName == _audioPage.PageKey);
        if (panelKeys != null) panelKeys.SetActive(pageName == _keysPage.PageKey);

        ISettingsPage newPage = pageName == _audioPage.PageKey ? _audioPage : _keysPage;
        if (newPage != null && newPage != _curPage)
        {
            _curPage?.OnExit();
            _curPage = newPage;
            _curPage?.OnEnter();
        }
    }

    #endregion
}
