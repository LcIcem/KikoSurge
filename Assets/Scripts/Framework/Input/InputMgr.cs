using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入管理器
/// <para>单例模式，通过 Update 轮询键位状态，缓存到公开属性供其他模块读取</para>
/// </summary>
public class InputMgr : MonoSingleton<InputMgr>
{
    #region 字段
    // 键位绑定字典  键：输入动作类型枚举  值：具体按键
    private Dictionary<InputActionType, KeyCode> keyBindings = new Dictionary<InputActionType, KeyCode>();
    private Vector2 moveInput; // 移动输入
    private Vector3 mouseWorldPosition; // 鼠标世界坐标
    private bool isInputEnabled = true;  // 输入是否开启标识
    private Camera mainCam; // 主摄像机
    private InputConfig_SO defaultConfig; // 默认配置（SO）
    private string keyBindingsPath; // 键位绑定文件的路径
    private bool isLoaded = false; // 是否已经加载键位标识
    #endregion

    #region 公开属性
    /// <summary> 当前 WASD 移动向量（规格化后，Pause 时为 Vector2.zero） </summary>
    public Vector2 MoveInput => moveInput;

    /// <summary> 鼠标世界坐标（主摄像机平面交点，Pause 时不更新） </summary>
    public Vector3 MouseWorldPosition => mouseWorldPosition;

    /// <summary> 输入开关（设置 false 暂停游戏输入，ESC 暂停本身不受影响） </summary>
    public bool IsInputEnabled
    {
        get => isInputEnabled;
        set => isInputEnabled = value;
    }
    #endregion

    #region Unity 生命周期
    void Awake()
    {
        mainCam = Camera.main;
        // 设置 键位绑定文件 保存路径
        keyBindingsPath = $"{Application.persistentDataPath}/{FrameworkConst.KEY_BINDINGS_PATH}";
    }

    void Start()
    {
        // 加载键位绑定信息
        LoadKeyBindings();
    }
    #endregion

    void Update()
    {
        if (!isInputEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }
        UpdateMoveInput();
        UpdateMouseWorldPosition();
    }

    #region 输入轮询
    /// <summary>
    /// 更新移动输入
    /// </summary>
    public void UpdateMoveInput()
    {
        // 如果输入关闭 直接返回
        if (!isInputEnabled) return;

        // WASD 移动
        float h = 0f, v = 0f;
        if (Input.GetKey(keyBindings[InputActionType.MoveUp]))    v += 1f;
        if (Input.GetKey(keyBindings[InputActionType.MoveDown]))  v -= 1f;
        if (Input.GetKey(keyBindings[InputActionType.MoveLeft]))  h -= 1f;
        if (Input.GetKey(keyBindings[InputActionType.MoveRight])) h += 1f;
        moveInput = new Vector2(h, v).normalized;
    }

    /// <summary>
    /// 更新鼠标对应的世界坐标位置
    /// </summary>
    private void UpdateMouseWorldPosition()
    {
        if (mainCam == null) return;
        // 得到一个从屏幕视点到鼠标的射线
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        // 得到一个地面平面（XOY平面）
        Plane groundPlane = new Plane(Vector3.forward, Vector3.zero);
        // 从摄像机开始对该平面进行射线检测 根据返回的值来更新鼠标世界坐标
        if (groundPlane.Raycast(ray, out float distance))
            mouseWorldPosition = ray.GetPoint(distance);
    }
    #endregion

    #region 键位管理
    /// <summary>
    /// 加载键位配置（优先级：JSON > SO 默认值 > 硬编码兜底）
    /// </summary>
    public void LoadKeyBindings()
    {
        if (isLoaded) return;

        LoadDefaultKeyBindings();
        LoadKeyBindingsFromJson();

        isLoaded = true;
    }

    /// <summary>
    /// 从 SO 或硬编码加载默认键位
    /// </summary>
    private void LoadDefaultKeyBindings()
    {
        // 加载默认键位配置文件
        defaultConfig = Resources.Load<InputConfig_SO>(FrameworkConst.SO_DEFAULT_PATH);
        // 如果该文件不为空 将键位绑定设置为默认配置
        if (defaultConfig != null)
        {
            keyBindings = defaultConfig.ToDictionary();
            Log("从 SO 加载默认键位配置。");
        }
        else
        {
            // 否则 使用硬编码来配置
            keyBindings = InputConfig_SO.GetHardcodedDefaults();
            LogWarning($"未找到 {FrameworkConst.SO_DEFAULT_PATH} ，使用硬编码默认键位。");
        }
    }

    /// <summary>
    /// 从 JSON 文件覆盖键位（运行时用户配置）
    /// </summary>
    private void LoadKeyBindingsFromJson()
    {
        var saved = JsonUtil.LoadFromFile<Dictionary<InputActionType, KeyCode>>(keyBindingsPath);
        if (saved == null || saved.Count == 0) return;

        keyBindings = saved;
        Log("从 JSON 覆盖键位配置。");
    }

    /// <summary>
    /// 保存当前键位到 JSON 文件
    /// </summary>
    public void SaveKeyBindings()
    {
        try
        {
            JsonUtil.SaveToFile(keyBindingsPath, keyBindings);
            Log($"键位已保存至 {keyBindingsPath}");
        }
        catch (Exception e)
        {
            LogError($"保存 keybindings.json 失败：{e.Message}");
        }
    }

    /// <summary>
    /// 修改单个键位绑定
    /// </summary>
    public void SetKeyBinding(InputActionType action, KeyCode key)
    {
        if (keyBindings.ContainsKey(action))
        {
            keyBindings[action] = key;
            Log($"键位修改：{action} → {key}");
        }
        else LogWarning($"未知的 InputActionType：{action}");
    }

    /// <summary>
    /// 重置为 SO 默认值
    /// </summary>
    public void ResetToDefault()
    {
        keyBindings = defaultConfig != null
            ? defaultConfig.ToDictionary()
            : InputConfig_SO.GetHardcodedDefaults();
        Log(defaultConfig != null ? "键位已重置为 SO 默认值。" : "键位已重置为硬编码默认值。");
    }

    /// <summary>
    /// 获取当前键位映射（用于 UI 键位设置界面）
    /// </summary>
    public Dictionary<InputActionType, KeyCode> GetAllKeyBindings()
        => new Dictionary<InputActionType, KeyCode>(keyBindings);
    #endregion


    #region 日志
    private void Log(string msg) => Debug.Log($"[{GetType().Name}] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[{GetType().Name}] {msg}");
    private void LogError(string msg) => Debug.LogError($"[{GetType().Name}] {msg}");
    #endregion
}
