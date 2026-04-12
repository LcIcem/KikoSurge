using System.Collections.Generic;
using LcIcemFramework.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

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

    #region 日志
    private void Log(string msg) => Debug.Log("[InputManager] " + msg);
    private void LogWarning(string msg) => Debug.LogWarning("[InputManager] " + msg);
    private void LogError(string msg) => Debug.LogError("[InputManager] " + msg);
    #endregion
}