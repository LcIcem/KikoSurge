using System.Collections.Generic;
using LcIcemFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 键位设置页面（拖拽：容器 + 模板预制体 + 重置按钮）
/// </summary>
public class KeysSettingsPage : ISettingsPage
{
    // 设置页面编辑的目标 Map
    private const string TargetMapName = "Player";

    // Action 名称常量（Player Map 中的 Actions）
    private static readonly string[] ActionNames = new[]
    {
        "Move", "Look", "Interact", "Shoot", "Dash", "SwitchWeapon", "OpenInventory"
    };

    // Action 显示名称映射
    private static readonly Dictionary<string, string> ActionDisplayNames = new Dictionary<string, string>
    {
        { "Move", "移动" },
        { "Look", "视角" },
        { "Interact", "交互"},
        { "Shoot", "射击" },
        { "Dash", "闪避" },
        { "SwitchWeapon", "切换武器" },
        { "OpenInventory", "打开背包" },
    };

    // 拖拽配置（由 SettingsPanel 传入）
    private RectTransform _contentContainer;
    private GameObject _itemTemplate;
    private Button _btnResetAll;

    // key 格式: "actionName" (简单绑定) 或 "actionName_bindingIndex" (组合子按键)
    private readonly Dictionary<string, Button> _rebindButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, Text> _bindingLabels = new Dictionary<string, Text>();

    public string PageKey => "panel_keys";

    /// <summary>
    /// 注入拖拽配置
    /// </summary>
    public void SetDraggedRefs(RectTransform container, GameObject template, Button resetBtn)
    {
        _contentContainer = container;
        _itemTemplate = template;
        _btnResetAll = resetBtn;
    }

    public void Init(BasePanel ownerPanel)
    {
    }

    public void OnEnter()
    {
        BuildKeybindingList();
        RefreshAllLabels();
        RegisterResetButton();
    }

    public void OnExit()
    {
        GameDataManager.Instance.SaveKeybindings(TargetMapName);
        ClearKeybindingList();
    }

    public void OnTogValueChanged(string togName, bool value)
    {
    }

    public void OnSliderValueChanged(string sliderName, float value)
    {
    }

    public void OnClick(string btnName)
    {
        if (_rebindButtons.TryGetValue(btnName, out var btn))
        {
            OnRebindClicked(btnName);
        }
    }

    private void RegisterResetButton()
    {
        if (_btnResetAll != null)
        {
            _btnResetAll.onClick.RemoveAllListeners();
            _btnResetAll.onClick.AddListener(OnResetAllClicked);
        }
    }

    private void BuildKeybindingList()
    {
        if (_contentContainer == null || _itemTemplate == null)
            return;
        ClearKeybindingList();

        foreach (var actionName in ActionNames)
        {
            EnumerateActionBindings(actionName);
        }
    }

    /// <summary>
    /// 遍历指定 Action 的所有可绑定项，为每个创建 UI Item
    /// </summary>
    private void EnumerateActionBindings(string actionName)
    {
        var action = ManagerHub.Input.GetInputActionFromMap(TargetMapName, actionName);
        if (action == null)
            return;

        var actionDisplayName = ActionDisplayNames.TryGetValue(actionName, out var dn) ? dn : actionName;

        int i = 0;
        while (i < action.bindings.Count)
        {
            var binding = action.bindings[i];

            if (binding.isPartOfComposite)
            {
                i++;
                continue;
            }

            if (binding.isComposite)
            {
                // 组合绑定：遍历其所有连续 PartOfComposite 子级，每个创建一个 Item
                for (int j = i + 1; j < action.bindings.Count; j++)
                {
                    var child = action.bindings[j];
                    if (!child.isPartOfComposite)
                        break;
                    string partDisplay = InputActionRebindingExtensions.GetBindingDisplayString(action, j, default);
                    string itemDisplayName = string.IsNullOrEmpty(MapPartDisplay(partDisplay))
                        ? actionDisplayName
                        : $"{actionDisplayName} ({MapPartDisplay(partDisplay)})";
                    CreateKeybindingItem($"{actionName}_{j}", itemDisplayName, actionName, j);
                }
            }
            else
            {
                // 普通绑定：key 格式为 "actionName_bindingIndex"
                CreateKeybindingItem($"{actionName}_{i}", actionDisplayName, actionName, i);
            }
            i++;
        }
    }

    private string MapPartDisplay(string partDisplay)
    {
        return partDisplay switch
        {
            "W" => "Up",
            "A" => "Left",
            "S" => "Down",
            "D" => "Right",
            _ => ""
        };
    }

    private void CreateKeybindingItem(string itemKey, string displayName, string actionName, int bindingIndex)
    {
        var itemObj = Object.Instantiate(_itemTemplate, _contentContainer);
        itemObj.name = $"KeyItem_{itemKey}";
        itemObj.SetActive(true);

        var labelText = itemObj.transform.Find("txt_action")?.GetComponent<Text>();
        var bindingText = itemObj.transform.Find("txt_binding")?.GetComponent<Text>();
        var btnRebind = itemObj.transform.Find("btn_rebind")?.GetComponent<Button>();

        if (labelText != null)
            labelText.text = displayName;

        if (bindingText != null)
        {
            bindingText.text = ManagerHub.Input.GetBindingDisplayNameFromMap(TargetMapName, actionName, bindingIndex);
            _bindingLabels[itemKey] = bindingText;
        }

        if (btnRebind != null)
        {
            btnRebind.name = $"btn_rebind_{itemKey}";
            _rebindButtons[itemKey] = btnRebind;
            // 闭包捕获 itemKey，点击时触发对应按键的重绑定
            btnRebind.onClick.AddListener(() => OnRebindClicked(itemKey));
        }
    }

    private void ClearKeybindingList()
    {
        if (_contentContainer != null)
        {
            foreach (Transform child in _contentContainer)
            {
                if (child.name.StartsWith("KeyItem_"))
                    Object.Destroy(child.gameObject);
            }
        }
        _rebindButtons.Clear();
        _bindingLabels.Clear();
    }

    private void RefreshAllLabels()
    {
        foreach (var kvp in _bindingLabels)
        {
            // key 格式: "actionName" 或 "actionName_bindingIndex"
            string actionName = kvp.Key;
            int bindingIndex = -1;
            if (kvp.Key.Contains("_"))
            {
                var parts = kvp.Key.Split('_');
                actionName = parts[0];
                int.TryParse(parts[1], out bindingIndex);
            }
            kvp.Value.text = ManagerHub.Input.GetBindingDisplayNameFromMap(TargetMapName, actionName, bindingIndex);
        }
    }

    private void OnRebindClicked(string itemKey)
    {
        string actionName = itemKey;
        int bindingIndex = -1;
        if (itemKey.Contains("_"))
        {
            var parts = itemKey.Split('_');
            actionName = parts[0];
            int.TryParse(parts[1], out bindingIndex);
        }

        if (_bindingLabels.TryGetValue(itemKey, out var label))
        {
            label.text = "按键...";
            label.color = Color.cyan;
        }
        if (_rebindButtons.TryGetValue(itemKey, out var btn))
        {
            btn.interactable = false;
        }

        ManagerHub.Input.RebindActionFromMap(TargetMapName, actionName, bindingIndex,
            () => OnRebindComplete(itemKey, actionName, bindingIndex),
            () => OnRebindCancel(itemKey, actionName, bindingIndex));
    }

    private void OnRebindComplete(string itemKey, string actionName, int bindingIndex)
    {
        if (_bindingLabels.TryGetValue(itemKey, out var label))
        {
            label.text = ManagerHub.Input.GetBindingDisplayNameFromMap(TargetMapName, actionName, bindingIndex);
            label.color = Color.yellow;
        }
        if (_rebindButtons.TryGetValue(itemKey, out var btn))
        {
            btn.interactable = true;
        }

        // OpenInventory 绑定修改时，同步到 UIMap 的 CloseInventory
        if (actionName == "OpenInventory")
        {
            ManagerHub.Input.SyncBindingToMap("Player", "OpenInventory", "UI", "CloseInventory", bindingIndex);
            GameDataManager.Instance.SaveKeybindings("UI");
        }
    }

    private void OnRebindCancel(string itemKey, string actionName, int bindingIndex)
    {
        if (_bindingLabels.TryGetValue(itemKey, out var label))
        {
            label.text = ManagerHub.Input.GetBindingDisplayNameFromMap(TargetMapName, actionName, bindingIndex);
            label.color = Color.yellow;
        }
        if (_rebindButtons.TryGetValue(itemKey, out var btn))
        {
            btn.interactable = true;
        }
    }

    private void OnResetAllClicked()
    {
        GameDataManager.Instance.ResetKeybindings();
        RefreshAllLabels();
    }
}
