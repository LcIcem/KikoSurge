using System.Collections.Generic;
using LcIcemFramework.Core;
using LcIcemFramework.Util.Data;
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

    /// <summary>
    /// 当前激活的 Action Map 中所有 Action 的引用
    /// 每次 SwitchActionMap 时会重建此字典
    /// </summary>
    public Dictionary<string, InputAction> Actions { get; private set; } = new();

    // 当前激活的 Action Map 名称
    private string _currentActionMap = "MainMenu";

    // 每个 Map 的键位覆盖 JSON（分开存储）
    private readonly Dictionary<string, string> _bindingOverridesByMap = new();

    // 游戏状态到 Action Map 的映射
    private static readonly Dictionary<GameState, string> StateToActionMap = new()
    {
        { GameState.MainMenu, "MainMenu" },
        { GameState.Lobby, "MainMenu" },
        { GameState.Playing, "Player" },
        { GameState.Paused, "UI" },
        { GameState.GameOver, "UI" }
    };

    protected override void Init()
    {
        _playerInput = GetComponent<PlayerInput>() ?? gameObject.AddComponent<PlayerInput>();

        // 初始化 Actions 字典
        RebuildActionsDictionary();

        // 默认启用 UI Map
        SwitchActionMap("UI");
    }

    // 开启或关闭输入系统
    public void TurnOn(bool on)
    {
        if (on)
            _playerInput.ActivateInput();
        else
            _playerInput.DeactivateInput();
    }

    #region Action Map 切换

    /// <summary>
    /// 切换到指定的 Action Map
    /// </summary>
    /// <param name="mapName">目标 Map 名称 (MainMenu/Player/UI)</param>
    public void SwitchActionMap(string mapName)
    {
        if (_currentActionMap == mapName)
            return;

        // 切换 Action Map 时不保存覆盖（SaveCurrentMapBindingOverrides 只在 rebind 完成时调用）
        var actionMap = _playerInput.actions.FindActionMap(mapName);
        if (actionMap == null)
        {
            LogError($"SwitchActionMap: ActionMap '{mapName}' not found!");
            return;
        }

        // 禁用所有 Map，再启用目标 Map
        foreach (var map in _playerInput.actions.actionMaps)
        {
            map.Disable();
        }
        actionMap.Enable();
        _currentActionMap = mapName;

        // 3. 重建 Actions 字典
        RebuildActionsDictionary();

        // 4. 恢复新 Map 的键位覆盖
        RestoreMapBindingOverrides(mapName);

    }

    /// <summary>
    /// 根据游戏状态自动切换 Action Map
    /// </summary>
    public void SwitchActionMapByGameState(GameState state)
    {
        if (StateToActionMap.TryGetValue(state, out var mapName))
        {
            SwitchActionMap(mapName);
        }
        else
        {
            LogError($"SwitchActionMapByGameState: No ActionMap mapping for state {state}");
        }
    }

    /// <summary>
    /// 获取当前 Action Map 名称
    /// </summary>
    public string GetCurrentActionMapName() => _currentActionMap;

    /// <summary>
    /// 重建 Actions 字典（用当前 Map 的 Actions 填充）
    /// </summary>
    private void RebuildActionsDictionary()
    {
        Actions.Clear();
        var currentMap = _playerInput.actions.FindActionMap(_currentActionMap);
        if (currentMap != null)
        {
            foreach (var action in currentMap)
            {
                Actions.Add(action.name, action);
            }
        }
    }

    /// <summary>
    /// 保存当前 Map 的键位覆盖
    /// </summary>
    private void SaveCurrentMapBindingOverrides()
    {
        SaveBindingOverridesForMap(_currentActionMap);
    }

    /// <summary>
    /// 恢复指定 Map 的键位覆盖
    /// </summary>
    private void RestoreMapBindingOverrides(string mapName)
    {
        if (_bindingOverridesByMap.TryGetValue(mapName, out var json) && !string.IsNullOrEmpty(json))
        {
            RestoreBindingOverridesFromJson(mapName, json);
        }
    }

    /// <summary>
    /// 使用 ApplyBindingOverride 直接应用覆盖（绕过 LoadBindingOverridesFromJson 的问题）
    /// </summary>
    private void RestoreBindingOverridesFromJson(string mapName, string json)
    {
        try
        {
            var overrideDict = JsonUtil.FromJson<Dictionary<string, string>>(json);
            if (overrideDict == null || overrideDict.Count == 0) return;

            var actionMap = _playerInput.actions.FindActionMap(mapName);
            if (actionMap == null) return;

            foreach (var kvp in overrideDict)
            {
                // key 格式: "ActionName|BindingIndex"
                int sepIdx = kvp.Key.LastIndexOf('|');
                if (sepIdx < 0) continue;

                string actionName = kvp.Key.Substring(0, sepIdx);
                if (!int.TryParse(kvp.Key.Substring(sepIdx + 1), out int bindingIndex)) continue;

                var action = actionMap.FindAction(actionName);
                if (action == null) continue;
                if (bindingIndex < 0 || bindingIndex >= action.bindings.Count) continue;

                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    action.ApplyBindingOverride(bindingIndex, new InputBinding { overridePath = kvp.Value });
                }
                else
                {
                    action.ApplyBindingOverride(bindingIndex, default(InputBinding));
                }
            }
        }
        catch (System.Exception e)
        {
            LogError($"RestoreBindingOverridesFromJson failed: {e.Message}");
        }
    }

    #endregion

    #region 键位管理

    /// <summary>
    /// 保存当前键位覆盖为 JSON 字符串（包含所有 Map 的覆盖）
    /// </summary>
    public string SaveBindingOverrides()
    {
        // 先保存当前 Map 的覆盖
        SaveCurrentMapBindingOverrides();

        // 使用 LitJson 序列化（支持 Dictionary）
        string json = JsonUtil.ToJson(_bindingOverridesByMap);
        return json;
    }

    /// <summary>
    /// 保存指定 Map 的键位覆盖到内存字典（手动提取每个 binding 的 overridePath）
    /// </summary>
    public void SaveBindingOverridesForMap(string mapName)
    {
        if (string.IsNullOrEmpty(mapName)) return;
        var actionMap = _playerInput.actions.FindActionMap(mapName);
        if (actionMap == null) return;

        // 手动收集所有有 overridePath 的 binding
        var overrideDict = new Dictionary<string, string>();
        foreach (var action in actionMap)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!string.IsNullOrEmpty(binding.overridePath))
                {
                    string key = $"{action.name}|{i}";
                    overrideDict[key] = binding.overridePath;
                }
            }
        }

        if (overrideDict.Count == 0)
            return;

        string json = JsonUtil.ToJson(overrideDict);
        _bindingOverridesByMap[mapName] = json;
    }

    /// <summary>
    /// 合并保存：把已有 JSON 数据与当前内存覆盖合并后返回
    /// 已有数据作为基础，当前内存数据覆盖之（保证本次 session 修改的覆盖不被丢失）
    /// </summary>
    /// <param name="existingJson">磁盘上已有的 JSON 数据</param>
    public string SaveBindingOverridesWithMerge(string existingJson)
    {
        return SaveBindingOverridesWithMerge(_currentActionMap, existingJson);
    }

    /// <summary>
    /// 合并保存（指定 Map）：把已有 JSON 数据与当前内存覆盖合并后返回
    /// </summary>
    /// <param name="mapName">指定要保存的 Map 名称</param>
    /// <param name="existingJson">磁盘上已有的 JSON 数据</param>
    public string SaveBindingOverridesWithMerge(string mapName, string existingJson)
    {
        // 先保存指定 Map 的覆盖到内存字典（不改变 _currentActionMap）
        SaveBindingOverridesForMap(mapName);

        // 如果有已有数据，先合并
        if (!string.IsNullOrEmpty(existingJson))
        {
            try
            {
                var existingDict = JsonUtil.FromJson<Dictionary<string, string>>(existingJson);
                if (existingDict != null)
                {
                    // 以内存字典为主（覆盖同名 key），合并已有数据
                    foreach (var kvp in existingDict)
                    {
                        if (!_bindingOverridesByMap.ContainsKey(kvp.Key))
                            _bindingOverridesByMap[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (System.Exception e)
            {
                LogError($"SaveBindingOverridesWithMerge: failed to parse existing JSON - {e.Message}");
            }
        }

        string json = JsonUtil.ToJson(_bindingOverridesByMap);
        return json;
    }

    /// <summary>
    /// 从 JSON 加载键位覆盖（包含所有 Map 的覆盖）
    /// </summary>
    public void LoadBindingOverrides(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var dict = JsonUtil.FromJson<Dictionary<string, string>>(json);
            if (dict != null)
            {
                _bindingOverridesByMap.Clear();
                foreach (var kvp in dict)
                {
                    _bindingOverridesByMap[kvp.Key] = kvp.Value;
                }
            }

            // 立即应用到所有有覆盖的 Map（而非仅当前 Map，避免 Player 覆盖加载后未应用）
            foreach (var kvp in _bindingOverridesByMap)
            {
                RestoreMapBindingOverrides(kvp.Key);
            }
        }
        catch (System.Exception e)
        {
            LogError($"LoadBindingOverrides: Failed to parse JSON - {e.Message}");
        }
    }

    /// <summary>
    /// 重置所有键位覆盖
    /// </summary>
    public void ResetBindingOverrides()
    {
        _bindingOverridesByMap.Clear();
        _playerInput.actions.LoadBindingOverridesFromJson("");
    }

    /// <summary>
    /// 获取指定名称的 InputAction 引用（从当前 Map）
    /// </summary>
    public InputAction GetInputAction(string actionName)
    {
        return _playerInput.actions.FindAction(actionName);
    }

    /// <summary>
    /// 获取指定 Map 中指定名称的 InputAction 引用
    /// </summary>
    public InputAction GetInputActionFromMap(string mapName, string actionName)
    {
        var map = _playerInput.actions.FindActionMap(mapName);
        if (map == null)
        {
            LogError($"GetInputActionFromMap: ActionMap '{mapName}' not found!");
            return null;
        }
        return map.FindAction(actionName);
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
    /// 获取指定 Map 中指定 Action 的单个绑定显示名
    /// </summary>
    public string GetBindingDisplayNameFromMap(string mapName, string actionName, int bindingIndex)
    {
        var action = GetInputActionFromMap(mapName, actionName);
        if (action == null) return "None";
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count) return "None";

        var binding = action.bindings[bindingIndex];

        // 如果有覆盖路径，直接从 overridePath 解析显示名（绕过 GetBindingDisplayString 的缓存问题）
        if (!string.IsNullOrEmpty(binding.overridePath))
        {
            return GetDisplayNameFromPath(binding.overridePath);
        }

        return InputActionRebindingExtensions.GetBindingDisplayString(action, bindingIndex, default);
    }

    /// <summary>
    /// 从 InputBinding.overridePath 解析显示名（如 "Keyboard/r" -> "R"）
    /// </summary>
    private string GetDisplayNameFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "None";

        // 路径格式: "<Keyboard>/r", "<Mouse>/leftButton", etc.
        int slashIndex = path.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < path.Length - 1)
        {
            string key = path.Substring(slashIndex + 1);

            // 鼠标按键映射
            if (key == "leftButton") return "LMB";
            if (key == "rightButton") return "RMB";
            if (key == "middleButton") return "MMB";

            // 修饰键
            if (key == "shift") return "Shift";
            if (key == "ctrl") return "Ctrl";
            if (key == "alt") return "Alt";

            // 其他直接返回（大部分键盘按键）
            // 第一个字符大写，其余小写
            if (key.Length == 1)
                return key.ToUpper();
            return char.ToUpper(key[0]) + key.Substring(1).ToLower();
        }
        return path;
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
    /// 启动交互式重绑定，重绑定指定 Map 中指定 Action 的指定绑定索引
    /// </summary>
    public void RebindActionFromMap(string mapName, string actionName, int bindingIndex, UnityEngine.Events.UnityAction onComplete, UnityEngine.Events.UnityAction onCancel)
    {
        var action = GetInputActionFromMap(mapName, actionName);
        if (action == null)
        {
            LogError($"RebindActionFromMap: Action '{actionName}' not found in map '{mapName}'!");
            return;
        }
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            LogError($"RebindActionFromMap: Invalid binding index {bindingIndex} for action '{actionName}' in map '{mapName}'");
            return;
        }


        action.Disable();
        action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation => {
                action.Enable();
                GameDataManager.Instance.SaveKeybindings(mapName);
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

    #region 鼠标/瞄准

    /// <summary>
    /// 获取鼠标的世界坐标（需要 Main Camera）
    /// </summary>
    public Vector3? GetMouseWorldPosition()
    {
        if (UnityEngine.Camera.main == null)
            return null;
        return UnityEngine.Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    /// <summary>
    /// 获取从指定位置指向鼠标的瞄准方向
    /// </summary>
    public Vector2 GetAimDirection(Vector3 fromPosition)
    {
        var mousePos = GetMouseWorldPosition();
        if (!mousePos.HasValue)
            return Vector2.right;

        Vector2 dir = ((Vector2)mousePos.Value - (Vector2)fromPosition).normalized;
        return dir.magnitude > 0.01f ? dir : Vector2.right;
    }

    #endregion

    #region 日志
    private void Log(string msg) => Debug.Log($"[InputManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[InputManager] {msg}");
    #endregion
}
}