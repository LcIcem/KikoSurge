using System.Collections.Generic;
using LcIcemFramework.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LcIcemFramework
{
/// <summary>
/// 输入管理器
/// </summary>
public class InputManager : SingletonMono<InputManager>
{
    [SerializeField] private PlayerInput _playerInput;
    public Dictionary<string, InputAction> UIActions { get; private set; } = new();

    protected override void Init()
    {
        _playerInput = GetComponent<PlayerInput>() ?? gameObject.AddComponent<PlayerInput>();

        // 获取 UI Map 上的所有 Action 并添加进UIActions字典
        foreach (var action in _playerInput.actions.FindActionMap("UI"))
        {
            UIActions.Add(action.name, action);
        }
    }

    // 开启或关闭输入系统
    public void TurnOn(bool on)
    {
        if (on)
            _playerInput.ActivateInput();
        else
            _playerInput.DeactivateInput();
    }

    #region 键位管理

    /// <summary>
    /// 保存当前键位覆盖为 JSON 字符串
    /// </summary>
    public string SaveBindingOverrides()
    {
        return _playerInput.actions.SaveBindingOverridesAsJson();
    }

    /// <summary>
    /// 从 JSON 加载键位覆盖
    /// </summary>
    public void LoadBindingOverrides(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        _playerInput.actions.LoadBindingOverridesFromJson(json);
    }

    /// <summary>
    /// 重置所有键位覆盖
    /// </summary>
    public void ResetBindingOverrides()
    {
        _playerInput.actions.LoadBindingOverridesFromJson("");
    }

    /// <summary>
    /// 获取指定名称的 InputAction 引用
    /// </summary>
    public InputAction GetInputAction(string actionName)
    {
        return _playerInput.actions.FindAction(actionName);
    }

    /// <summary>
    /// 获取指定 Action 的单个绑定显示名
    /// </summary>
    public string GetBindingDisplayName(string actionName, int bindingIndex)
    {
        var action = _playerInput.actions.FindAction(actionName);
        if (action == null) return "None";
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count) return "None";
        return InputActionRebindingExtensions.GetBindingDisplayString(action, bindingIndex, default);
    }

    /// <summary>
    /// 获取指定 Action 的当前绑定显示名（组合绑定返回所有子绑定拼接）
    /// </summary>
    public string GetBindingDisplayName(string actionName)
    {
        var action = _playerInput.actions.FindAction(actionName);
        if (action == null) return "None";

        var parts = new List<string>();
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isComposite)
            {
                for (int j = 0; j < action.bindings.Count; j++)
                {
                    if (action.bindings[j].isPartOfComposite)
                    {
                        var display = action.GetBindingDisplayString(j);
                        if (!string.IsNullOrEmpty(display))
                            parts.Add(display);
                    }
                }
            }
            else if (!binding.isPartOfComposite)
            {
                var display = action.GetBindingDisplayString(i);
                if (!string.IsNullOrEmpty(display))
                    parts.Add(display);
            }
        }
        return parts.Count > 0 ? string.Join(" / ", parts) : "None";
    }

    /// <summary>
    /// 启动交互式重绑定，重绑定指定 Action 的指定绑定索引
    /// </summary>
    public void RebindAction(string actionName, int bindingIndex, UnityEngine.Events.UnityAction onComplete, UnityEngine.Events.UnityAction onCancel)
    {
        var action = _playerInput.actions.FindAction(actionName);
        if (action == null)
        {
            LogError($"RebindAction: Action not found - {actionName}");
            return;
        }
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            LogError($"RebindAction: Invalid binding index {bindingIndex} for action {actionName}");
            return;
        }

        action.Disable();
        action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation => {
                action.Enable();
                GameDataManager.Instance.SaveKeybindings();
                operation.Dispose();
                onComplete?.Invoke();
            })
            .OnCancel(operation => {
                action.Enable();
                operation.Dispose();
                onCancel?.Invoke();
            })
            .Start();
    }

    /// <summary>
    /// 启动交互式重绑定（组合绑定依次重绑定每个子按键）
    /// </summary>
    public void RebindAction(string actionName, UnityEngine.Events.UnityAction onComplete, UnityEngine.Events.UnityAction onCancel)
    {
        var action = _playerInput.actions.FindAction(actionName);
        if (action == null)
        {
            LogError($"RebindAction: Action not found - {actionName}");
            return;
        }

        var bindingIndices = new List<int>();
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isComposite)
            {
                // 组合绑定：依次收集其后的 PartOfComposite 子索引（连续段落）
                for (int j = i + 1; j < action.bindings.Count; j++)
                {
                    if (action.bindings[j].isPartOfComposite)
                        bindingIndices.Add(j);
                    else
                        break;
                }
            }
            else if (!binding.isPartOfComposite)
            {
                bindingIndices.Add(i);
            }
        }

        if (bindingIndices.Count == 0)
        {
            LogError($"RebindAction: No valid binding found for {actionName}");
            return;
        }

        RebindNext(action, bindingIndices, 0, onComplete, onCancel);
    }

    private void RebindNext(InputAction action, List<int> bindingIndices, int index, UnityEngine.Events.UnityAction onComplete, UnityEngine.Events.UnityAction onCancel)
    {
        if (index >= bindingIndices.Count)
        {
            GameDataManager.Instance.SaveKeybindings();
            onComplete?.Invoke();
            return;
        }

        int bindingIndex = bindingIndices[index];
        action.Disable();
        action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation => {
                action.Enable();
                operation.Dispose();
                RebindNext(action, bindingIndices, index + 1, onComplete, onCancel);
            })
            .OnCancel(operation => {
                action.Enable();
                operation.Dispose();
                GameDataManager.Instance.SaveKeybindings();
                onCancel?.Invoke();
            })
            .Start();
    }

    #endregion

    #region 日志
    private void LogError(string msg) => Debug.LogError("[InputManager] " + msg);
    #endregion
}
}