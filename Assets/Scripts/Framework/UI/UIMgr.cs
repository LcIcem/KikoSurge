using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// UI 层级枚举，对应 Canvas 下预设的四个分区。
/// </summary>
public enum UILayerType
{
    /// <summary>底层，用于底层背景等 UI。</summary>
    Bottom,
    /// <summary>中间层，默认面板层级。</summary>
    Middle,
    /// <summary>顶层，用于弹窗等高优先级 UI。</summary>
    Top,
    /// <summary>系统层，用于 Toast、提示框等最顶层显示。</summary>
    SystemLayer
}


/// <summary>
/// UI 管理器（单例）。
/// <para>功能：</para>
/// <list type="bullet">
///   <item>加载并显示面板至指定层级，自动处理 RectTransform 布局参数</item>
///   <item>通过字典管理已加载的面板实例，防止重复加载</item>
///   <item>提供面板隐藏、销毁及查询接口</item>
///   <item>持有 Canvas 根节点，实现跨场景持久化</item>
/// </list>
/// </summary>
public class UIMgr : Singleton<UIMgr>
{
    /// <summary>已加载面板字典。key 为面板名称，value 为面板实例，防止同一面板被重复加载。</summary>
    private Dictionary<string, BasePanel> _panelDic = new Dictionary<string, BasePanel>();

    /// <summary>Canvas 各层级根节点。</summary>
    private Transform _bottom;
    private Transform _middle;
    private Transform _top;
    private Transform _system;

    /// <summary>Canvas 的 RectTransform 引用，供外部获取 Canvas 尺寸等使用。</summary>
    public RectTransform Canvas { get; }

    public UIMgr()
    {
        // 同步加载 Canvas 并强转为 RectTransform
        Canvas = ResMgr.Instance.Load<GameObject>("UI/Canvas").transform as RectTransform;
        Object.DontDestroyOnLoad(Canvas.gameObject);

        // 缓存四个层级的 Transform 引用，避免每次加载面板时重复 Find
        _bottom = Canvas.Find("Bottom");
        _middle = Canvas.Find("Middle");
        _top = Canvas.Find("Top");
        _system = Canvas.Find("System");

        // EventSystem 也需持久化，否则 UI 点击事件无法响应
        Object.DontDestroyOnLoad(ResMgr.Instance.Load<GameObject>("UI/EventSystem"));
    }

    /// <summary>
    /// 根据层级枚举获取对应的父级 Transform。
    /// </summary>
    /// <param name="layer">目标层级。</param>
    /// <returns>该层级对应的 Transform，未知层级默认返回 Middle。</returns>
    public Transform GetLayerFather(UILayerType layer)
    {
        Transform trans = layer switch
        {
            UILayerType.Bottom => _bottom,
            UILayerType.Middle => _middle,
            UILayerType.Top => _top,
            UILayerType.SystemLayer => _system,
            _ => _middle  // 无效枚举值兜底，避免 father 未赋值
        };
        return trans;
    }

    /// <summary>
    /// 显示指定名称的面板，加载完成后自动添加到对应层级并显示。
    /// 已在字典中的面板不会重复加载，直接显示并触发回调。
    /// </summary>
    /// <typeparam name="T">面板类型，约束为 BasePanel 子类。</typeparam>
    /// <param name="panelName">面板资源名称（相对于 Assets/Resources/UI/）。</param>
    /// <param name="layer">目标层级，默认为 Middle。</param>
    /// <param name="callBack">加载完成回调，参数为面板实例。</param>
    public void ShowPanel<T>(string panelName, UILayerType layer = UILayerType.Middle, UnityAction<T> callBack = null)
        where T : BasePanel
    {
        // 已存在则直接显示，不再重复加载
        if (_panelDic.ContainsKey(panelName))
        {
            _panelDic[panelName].Show();
            callBack?.Invoke(_panelDic[panelName] as T);
            return;
        }

        // 异步加载面板 Prefab
        ResMgr.Instance.LoadAsync<GameObject>("UI/" + panelName, (obj) =>
        {
            // 根据层级获取父级 Transform
            Transform father = GetLayerFather(layer);

            // 挂载到对应层级，SetParent 后立即设置布局参数
            obj.transform.SetParent(father);

            // 重置本地坐标，使 Rect 左下角对齐锚点中心
            obj.transform.localPosition = Vector3.zero;
            // 重置缩放，避免 Prefab 缩放不为 1 的问题
            obj.transform.localScale = Vector3.one;

            // 重置 RectTransform 的左右上下边距，使其撑满父级
            RectTransform rect = obj.transform as RectTransform;
            rect.offsetMax = Vector2.zero;  // 右、上边距归零
            rect.offsetMin = Vector2.zero;  // 左、下边距归零

            // 获取面板组件，调用 Show 并触发回调
            T panel = obj.GetComponent<T>();
            panel.Show();
            callBack?.Invoke(panel);

            // 注册到字典中，防止重复加载
            _panelDic.Add(panelName, panel);
        });
    }

    /// <summary>
    /// 隐藏并销毁指定名称的面板。
    /// 仅对通过 ShowPanel 加载的面板有效。
    /// </summary>
    /// <param name="panelName">面板名称。</param>
    public void HidePanel(string panelName)
    {
        if (_panelDic.ContainsKey(panelName))
        {
            // 调用面板的 Hide 逻辑（如退场动画），然后销毁 GameObject
            _panelDic[panelName].Hide();
            Object.Destroy(_panelDic[panelName].gameObject);
            // 从字典移除引用
            _panelDic.Remove(panelName);
        }
    }

    /// <summary>
    /// 根据名称查询已加载的面板实例。
    /// </summary>
    /// <typeparam name="T">面板类型。</typeparam>
    /// <param name="panelName">面板名称。</param>
    /// <returns>面板实例，未加载返回 null。</returns>
    public T GetPanel<T>(string panelName) where T : BasePanel
    {
        if (_panelDic.ContainsKey(panelName))
            return _panelDic[panelName] as T;
        return null;
    }

    /// <summary>
    /// 为任意 UIBehaviour 组件添加自定义 EventTrigger 事件监听。
    /// 适用于除 Button、Toggle 等内置交互组件以外的事件（如 PointerEnter、PointerExit、BeginDrag 等）。
    /// </summary>
    /// <param name="control">要添加监听的 UI 组件。</param>
    /// <param name="type">事件类型（如 PointerEnter、BeginDrag 等）。</param>
    /// <param name="callBack">事件触发时的回调。</param>
    public static void AddCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> callBack)
    {
        // 获取或添加 EventTrigger 组件，一个组件可添加多个 Entry
        EventTrigger trigger = control.GetComponent<EventTrigger>() ?? control.AddComponent<EventTrigger>();

        // 创建新的事件条目，设置事件ID并注册回调
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(callBack);

        // 添加到 trigger 的列表中
        trigger.triggers.Add(entry);
    }

    #region 日志
    private void Log(string msg) => Debug.Log($"[UIMgr] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[UIMgr] {msg}");
    private void LogError(string msg) => Debug.LogError($"[UIMgr] {msg}");
    #endregion
}
