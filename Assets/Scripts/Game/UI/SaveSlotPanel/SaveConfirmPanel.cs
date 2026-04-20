using LcIcemFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 存档确认面板（重复点击非空存档时弹出）
/// </summary>
public class SaveConfirmPanel : BasePanel
{
    private const string BTN_OVERWRITE = "btn_overwrite";
    private const string BTN_DELETE = "btn_delete";
    private const string BTN_CANCEL = "btn_cancel";

    private int _slotIndex;
    private System.Action<int, ConfirmAction> _onConfirm;

    public void Initialize(int slotIndex, System.Action<int, ConfirmAction> onConfirm)
    {
        _slotIndex = slotIndex;
        _onConfirm = onConfirm;
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_OVERWRITE:
                _onConfirm?.Invoke(_slotIndex, ConfirmAction.Overwrite);
                ManagerHub.UI.HidePanel<SaveConfirmPanel>();
                break;
            case BTN_DELETE:
                _onConfirm?.Invoke(_slotIndex, ConfirmAction.Delete);
                ManagerHub.UI.HidePanel<SaveConfirmPanel>();
                break;
            case BTN_CANCEL:
                _onConfirm?.Invoke(_slotIndex, ConfirmAction.Cancel);
                ManagerHub.UI.HidePanel<SaveConfirmPanel>();
                break;
        }
    }
}
