using LcIcemFramework.Managers.UI;

/// <summary>
/// 键位设置页面（预留）
/// </summary>
public class KeysSettingsPage : ISettingsPage
{
    private BasePanel _ownerPanel;

    public void Init(BasePanel ownerPanel)
    {
        _ownerPanel = ownerPanel;
    }

    public void OnEnter()
    {
        // TODO: 实现键位设置
    }

    public void OnExit()
    {
    }

    public void OnTogValueChanged(string togName, bool value)
    {
    }

    public void OnSliderValueChanged(string sliderName, float value)
    {
    }
}
