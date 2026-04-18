using LcIcemFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 存档槽位面板
/// </summary>
public class SaveSlotPanel : BasePanel
{
    private const string BTN_SLOT_0 = "btn_slot0";
    private const string BTN_SLOT_1 = "btn_slot1";
    private const string BTN_SLOT_2 = "btn_slot2";
    private const string BTN_BACK = "btn_back";

    private const string TXT_SLOT_0_TIME = "txt_slot0Time";
    private const string TXT_SLOT_1_TIME = "txt_slot1Time";
    private const string TXT_SLOT_2_TIME = "txt_slot2Time";

    // 边框高亮控件名称
    private const string IMG_SLOT_0_BORDER = "img_slot0Border";
    private const string IMG_SLOT_1_BORDER = "img_slot1Border";
    private const string IMG_SLOT_2_BORDER = "img_slot2Border";

    private const string BORDER_NORMAL = "SlotBorderNormal";   // 普通边框sprite
    private const string BORDER_SELECTED = "SlotBorderSelected"; // 选中边框sprite

    private int _lastClickedSlot = -1;
    private int _selectedSlot = -1;
    private PlayerSaveData[] _slotData = new PlayerSaveData[3];

    public override void Show()
    {
        base.Show();

        // 设置当前选中的槽位（从 SaveLoadManager 获取）
        _selectedSlot = SaveLoadManager.Instance.CurrentSlotId;
        RefreshAllSlots();
        UpdateAllSlotHighlights();
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            _slotData[i] = SaveLoadManager.Instance.LoadSlotInfo(i);
            RefreshSlotUI(i);
        }
    }

    private void RefreshSlotUI(int slotIndex)
    {
        var saveData = _slotData[slotIndex];
        bool hasData = saveData != null;

        string timeKey = $"txt_slot{slotIndex}Time";

        var timeTxt = GetControl<Text>(timeKey);

        if (hasData && timeTxt != null)
        {
            timeTxt.text = GetPlayTimeString(saveData.totalPlayTimeSeconds);
        }
        else if (timeTxt != null)
        {
            timeTxt.text = "空存档";
        }
    }

    /// <summary>
    /// 更新所有槽位的高亮状态
    /// </summary>
    private void UpdateAllSlotHighlights()
    {
        for (int i = 0; i < 3; i++)
        {
            UpdateSlotHighlight(i);
        }
    }

    /// <summary>
    /// 更新指定槽位的高亮状态
    /// </summary>
    private void UpdateSlotHighlight(int slotIndex)
    {
        string borderKey = GetBorderKey(slotIndex);
        print("borderKey" + borderKey);
        var borderImg = GetControl<Image>(borderKey);
        print("borderName" + borderImg.name);
        if (borderImg == null)
            return;

        if (slotIndex == _selectedSlot)
        {
            // 选中状态
            borderImg.enabled = true;
        }
        else
        {
            // 普通状态
            borderImg.enabled = false;
        }
    }

    private string GetBorderKey(int slotIndex)
    {
        return slotIndex switch
        {
            0 => IMG_SLOT_0_BORDER,
            1 => IMG_SLOT_1_BORDER,
            2 => IMG_SLOT_2_BORDER,
            _ => IMG_SLOT_0_BORDER
        };
    }

    private string GetPlayTimeString(long totalSeconds)
    {
        if (totalSeconds <= 0)
            return "已游玩 0s";

        var ts = System.TimeSpan.FromSeconds(totalSeconds);
        if (ts.TotalHours >= 1)
        {
            return $"已游玩 {(int)ts.TotalHours}h {ts.Minutes}m";
        }
        if (ts.TotalMinutes >= 1)
        {
            return $"已游玩 {(int)ts.TotalMinutes}m {ts.Seconds}s";
        }
        return $"已游玩 {ts.Seconds}s";
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_SLOT_0:
                OnSlotClicked(0);
                break;
            case BTN_SLOT_1:
                OnSlotClicked(1);
                break;
            case BTN_SLOT_2:
                OnSlotClicked(2);
                break;
            case BTN_BACK:
                OnBackClicked();
                break;
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        var saveData = _slotData[slotIndex];
        bool hasData = saveData != null;

        if (hasData)
        {
            // 已有存档
            if (_lastClickedSlot == slotIndex)
            {
                // 重复点击同一非空存档，弹出确认面板
                ManagerHub.UI.ShowPanel<SaveConfirmPanel>(UILayerType.Top, (panel) =>
                {
                    panel.Initialize(slotIndex, OnConfirmAction);
                });
            }
            else
            {
                // 首次点击非空存档，切换到该槽位（更新高亮）
                OnSlotSelected(slotIndex);
            }
        }
        else
        {
            // 空存档，点击弹出新建确认面板
            ManagerHub.UI.ShowPanel<NewSaveConfirmPanel>(UILayerType.Top, (panel) =>
            {
                panel.Initialize(slotIndex, OnNewSaveConfirmed);
            });
        }

        _lastClickedSlot = slotIndex;
    }

    private void OnNewSaveConfirmed(int slotIndex)
    {
        // 立即创建新存档
        SaveLoadManager.Instance.CreateNewSave(slotIndex);
        _slotData[slotIndex] = SaveLoadManager.Instance.LoadSlotInfo(slotIndex);
        RefreshSlotUI(slotIndex);
        OnSlotSelected(slotIndex);
    }

    private void OnSlotSelected(int slotIndex)
    {
        // 选择存档槽位（不隐藏面板，以便实现再次点击弹出确认面板）
        _selectedSlot = slotIndex;
        SaveLoadManager.Instance.SelectSlot(slotIndex);
        UpdateAllSlotHighlights();
        // 面板保持打开，由 LoginPanel 的开始按钮触发进入大厅
    }

    private void OnConfirmAction(int slotIndex, ConfirmAction action)
    {
        switch (action)
        {
            case ConfirmAction.Overwrite:
                SaveLoadManager.Instance.DeleteSlot(slotIndex);
                _slotData[slotIndex] = null;
                RefreshSlotUI(slotIndex);
                OnSlotSelected(slotIndex);
                break;
            case ConfirmAction.Delete:
                SaveLoadManager.Instance.DeleteSlot(slotIndex);
                _slotData[slotIndex] = null;
                RefreshSlotUI(slotIndex);
                UpdateAllSlotHighlights();
                break;
            case ConfirmAction.Cancel:
                break;
        }
    }

    private void OnBackClicked()
    {
        ManagerHub.UI.HidePanel<SaveSlotPanel>();
    }
}

public enum ConfirmAction
{
    Overwrite,
    Delete,
    Cancel
}
