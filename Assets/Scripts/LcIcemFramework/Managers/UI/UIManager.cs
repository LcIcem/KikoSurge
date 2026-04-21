using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using LcIcemFramework.Core;
using LcIcemFramework;
using LcIcemFramework.Util.Const;

namespace LcIcemFramework
{

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
///   <item>通过 Addressables 异步加载并显示面板至指定层级，自动处理 RectTransform 布局参数</item>
///   <item>通过字典管理已加载的面板实例，防止重复加载</item>
///   <item>提供面板隐藏、销毁及查询接口</item>
///   <item>持有 Canvas 根节点，实现跨场景持久化</item>
///   <item>支持 Canvas 未就绪时的请求队列缓冲</item>
/// </list>
/// </summary>
public class UIManager : Singleton<UIManager>
{
    /// <summary>已加载面板字典。key 为面板名称，value 为面板实例，防止同一面板被重复加载。</summary>
    private Dictionary<string, BasePanel> _panelDic = new Dictionary<string, BasePanel>();

    /// <summary>正在加载中的面板名称集合，防止异步加载期间重复触发。</summary>
    private HashSet<string> _loadingPanels = new HashSet<string>();

    /// <summary>Canvas 各层级根节点。</summary>
    private Transform _bottom;
    private Transform _middle;
    private Transform _top;
    private Transform _system;

    /// <summary>Canvas 的 RectTransform 引用，供外部获取 Canvas 尺寸等使用。</summary>
    public RectTransform Canvas { get; private set; }

    /// <summary>Canvas 是否已加载完成。</summary>
    public bool IsReady => _isReady;
    private bool _isReady = false;

    /// <summary>Canvas 未就绪时，积压的 ShowPanel 请求队列。</summary>
    private Queue<Action> _pendingPanelLoads = new Queue<Action>();

    protected override void Init()
    {
        ManagerHub.Addressables.InstantiateAsync(Constants.UI_PATH + "Canvas", null, OnCanvasLoaded);
    }

    private void OnCanvasLoaded(GameObject canvasObj)
    {
        if (canvasObj == null)
        {
            LogError("Canvas 加载失败: UI/Canvas");
            return;
        }

        canvasObj.transform.SetParent(null);
        UnityEngine.Object.DontDestroyOnLoad(canvasObj);
        Canvas = canvasObj.transform as RectTransform;

        _bottom = Canvas.Find("Bottom");
        _middle = Canvas.Find("Middle");
        _top    = Canvas.Find("Top");
        _system = Canvas.Find("System");

        // 异步加载 EventSystem
        ManagerHub.Addressables.InstantiateAsync(Constants.UI_PATH + "EventSystem", null, OnEventSystemLoaded);
    }

    private void OnEventSystemLoaded(GameObject esObj)
    {
        if (esObj != null)
        {
            UnityEngine.Object.DontDestroyOnLoad(esObj);
        }

        _isReady = true;
        Log("Canvas 加载完成，已就绪。");

        // 处理积压的 ShowPanel 请求
        while (_pendingPanelLoads.Count > 0)
        {
            var action = _pendingPanelLoads.Dequeue();
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                LogError($"处理积压面板请求时出错: {e.Message}");
            }
        }
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
    /// 显示指定类型面板，加载完成后自动添加到对应层级并显示。
    /// 已在字典中的面板不会重复加载，直接显示并触发回调。
    /// Canvas 未就绪时，请求会被加入队列等待。
    /// 面板名称从泛型类型名推断（与 Addressables address 命名一致）。
    /// </summary>
    /// <typeparam name="T">面板类型，约束为 BasePanel 子类。</typeparam>
    /// <param name="layer">目标层级，默认为 Middle。</param>
    /// <param name="callBack">加载完成回调，参数为面板实例。</param>
    public void ShowPanel<T>(UILayerType layer = UILayerType.Middle, UnityAction<T> callBack = null)
        where T : BasePanel
    {
        string panelName = typeof(T).Name;

        // Canvas 未就绪时入队等待
        if (!_isReady)
        {
            _pendingPanelLoads.Enqueue(() => ShowPanel(layer, callBack));
            Log($"Canvas 未就绪，{panelName} 已加入加载队列。");
            return;
        }

        // 已存在则直接显示，不再重复加载
        if (_panelDic.ContainsKey(panelName))
        {
            var panel = _panelDic[panelName] as T;
            // 确保面板初始透明
            if (panel.CanvasGroup != null)
            {
                panel.CanvasGroup.alpha = 0;
                panel.CanvasGroup.blocksRaycasts = false;
            }
            panel.Show();
            callBack?.Invoke(panel);
            // 渐显面板
            if (panel.CanvasGroup != null)
            {
                FadeIn(panel.CanvasGroup, 0.3f);
            }
            return;
        }

        // 正在加载中，防止重复触发
        if (_loadingPanels.Contains(panelName))
        {
            Log($"{panelName} 正在加载中，忽略重复请求。");
            return;
        }

        // 异步加载面板 Prefab
        LoadPanelAsync<T>(layer, callBack);
    }

    /// <summary>
    /// 异步加载面板 Prefab。
    /// </summary>
    private void LoadPanelAsync<T>(UILayerType layer, UnityAction<T> callBack)
        where T : BasePanel
    {
        string panelName = typeof(T).Name;
        Transform father = GetLayerFather(layer);

        // 标记为正在加载
        _loadingPanels.Add(panelName);

        ManagerHub.Addressables.InstantiateAsync($"{Constants.UI_PATH}{panelName}", father, (obj) =>
        {
            // 无论成功失败，都要移除加载标记
            _loadingPanels.Remove(panelName);

            if (obj == null)
            if (obj == null)
            {
                LogError($"面板加载失败: {Constants.UI_PATH}{panelName}");
                return;
            }

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
            if (panel == null)
            {
                LogError($"面板 {panelName} 上未找到 {typeof(T).Name} 组件！");
                return;
            }

            // 确保面板初始透明，用户看不到 Show() 中的 UI 操作
            if (panel.CanvasGroup != null)
            {
                panel.CanvasGroup.alpha = 0;
                panel.CanvasGroup.blocksRaycasts = false;
            }

            panel.Show();
            callBack?.Invoke(panel);

            // 渐显面板（Show() 完成后触发）
            if (panel.CanvasGroup != null)
            {
                FadeIn(panel.CanvasGroup, 0.3f);
            }

            // 注册到字典中，防止重复加载
            if (_panelDic.ContainsKey(panelName))
            {
                Log($"面板 {panelName} 已存在，跳过重复添加");
                return;
            }
            _panelDic.Add(panelName, panel);
        });
    }

    /// <summary>
    /// 隐藏并销毁指定名称的面板。
    /// 仅对通过 ShowPanel 加载的面板有效。
    /// </summary>
    /// <param name="panelName">面板名称。</param>
    public void HidePanel<T>()
    {
        string panelName = typeof(T).Name;
        if (_panelDic.ContainsKey(panelName))
        {
            // 调用面板的 Hide 逻辑（如退场动画），然后销毁 GameObject
            _panelDic[panelName].Hide();
            UnityEngine.Object.Destroy(_panelDic[panelName].gameObject);
            // 从字典移除引用
            _panelDic.Remove(panelName);
        }
    }

    /// <summary>
    /// 面板关闭时回调，通知 GameLifecycleManager
    /// </summary>
    public event UnityEngine.Events.UnityAction<BasePanel> OnPanelClosed;

    /// <summary>
    /// 关闭当前最上层的面板
    /// </summary>
    /// <returns>是否成功关闭了面板</returns>
    public bool CloseTopPanel()
    {
        if (_panelDic.Count == 0)
            return false;

        // 找到 sibling index 最大的面板（最上层）
        BasePanel topPanel = null;
        int maxSiblingIndex = -1;

        foreach (var kvp in _panelDic)
        {
            int siblingIndex = kvp.Value.transform.GetSiblingIndex();
            if (siblingIndex > maxSiblingIndex)
            {
                maxSiblingIndex = siblingIndex;
                topPanel = kvp.Value;
            }
        }

        if (topPanel != null)
        {
            string panelName = topPanel.GetType().Name;
            topPanel.OnBeforeClose(); // 关闭前回调

            // 确保面板先设为透明，再调用 Hide
            if (topPanel.CanvasGroup != null)
            {
                topPanel.CanvasGroup.alpha = 0;
                topPanel.CanvasGroup.blocksRaycasts = false;
            }
            topPanel.Hide();
            UnityEngine.Object.Destroy(topPanel.gameObject);
            _panelDic.Remove(panelName);

            // 通知面板关闭
            OnPanelClosed?.Invoke(topPanel);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 根据名称查询已加载的面板实例。
    /// </summary>
    /// <typeparam name="T">面板类型。</typeparam>
    /// <param name="panelName">面板名称。</param>
    /// <returns>面板实例，未加载返回 null。</returns>
    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (_panelDic.ContainsKey(panelName))
            return _panelDic[panelName] as T;
        return null;
    }

    /// <summary>
    /// 获取当前最上层的面板
    /// </summary>
    public BasePanel GetTopPanel()
    {
        if (_panelDic.Count == 0)
            return null;

        BasePanel topPanel = null;
        int maxSiblingIndex = -1;

        foreach (var kvp in _panelDic)
        {
            int siblingIndex = kvp.Value.transform.GetSiblingIndex();
            if (siblingIndex > maxSiblingIndex)
            {
                maxSiblingIndex = siblingIndex;
                topPanel = kvp.Value;
            }
        }

        return topPanel;
    }

    /// <summary>
    /// 渐显面板（alpha 从 0 到 1）。
    /// </summary>
    private void FadeIn(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null) return;
        MonoManager.Instance.StartCoroutine(FadeInCoroutine(canvasGroup, duration));
    }

    private IEnumerator FadeInCoroutine(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
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
        EventTrigger trigger = control.GetComponent<EventTrigger>() ?? control.gameObject.AddComponent<EventTrigger>();

        // 创建新的事件条目，设置事件ID并注册回调
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(callBack);

        // 添加到 trigger 的列表中
        trigger.triggers.Add(entry);
    }

    #region 日志
    private void Log(string msg) => Debug.Log($"[UIManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[UIManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[UIManager] {msg}");
    #endregion
}
}
