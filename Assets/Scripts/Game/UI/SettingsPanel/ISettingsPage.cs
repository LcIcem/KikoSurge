using LcIcemFramework;

/// <summary>
/// 设置页面接口
/// </summary>
public interface ISettingsPage
{
    /// <summary>
    /// 面板标识键，与 RightPanel 下的子面板名称对应
    /// </summary>
    string PageKey { get; }

    /// <summary>
    /// 初始化
    /// </summary>
    void Init(BasePanel ownerPanel);

    /// <summary>
    /// 进入页面时调用
    /// </summary>
    void OnEnter();

    /// <summary>
    /// 退出页面时调用
    /// </summary>
    void OnExit();

    /// <summary>
    /// Toggle 值变化时调用
    /// </summary>
    void OnTogValueChanged(string togName, bool value);

    /// <summary>
    /// Slider 值变化时调用
    /// </summary>
    void OnSliderValueChanged(string sliderName, float value);

    /// <summary>
    /// Button 点击时调用
    /// </summary>
    void OnClick(string btnName);
}
