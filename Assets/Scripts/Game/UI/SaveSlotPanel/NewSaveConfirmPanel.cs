using LcIcemFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新建存档确认面板（用于空存档）
/// </summary>
public class NewSaveConfirmPanel : BasePanel
{
    private const string BTN_NEW_GAME = "btn_newSave";
    private const string BTN_BACK = "btn_back";

    private int _slotIndex;
    private System.Action<int> _onConfirm;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    /// <param name="onConfirm">确认回调，参数为槽位索引</param>
    public void Initialize(int slotIndex, System.Action<int> onConfirm)
    {
        _slotIndex = slotIndex;
        _onConfirm = onConfirm;
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_NEW_GAME:
                OnNewGameClicked();
                break;
            case BTN_BACK:
                OnBackClicked();
                break;
        }
    }

    private void OnNewGameClicked()
    {
        _onConfirm?.Invoke(_slotIndex);
        ManagerHub.UI.HidePanel<NewSaveConfirmPanel>();
    }

    private void OnBackClicked()
    {
        ManagerHub.UI.HidePanel<NewSaveConfirmPanel>();
    }
}
