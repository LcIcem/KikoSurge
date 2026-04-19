using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LcIcemFramework
{
/// <summary>
/// UI 面板基类，提供通用的子控件查找与缓存机制。
/// <para>功能：</para>
/// <list type="bullet">
///   <item>Awake 时自动查找并缓存所有子级的常见 UI 组件（Button、Image、Text 等）</item>
///   <item>通过 GetControl 方法根据控件名称和类型获取对应组件，无需手动拖拽赋值</item>
///   <item>子类重写 Show/Hide 方法实现面板的显示与隐藏逻辑</item>
///   <item>自动为 Button 和 Toggle 注册点击/值变化事件，统一回调至子类虚方法</item>
/// </list>
/// <para>使用约定：子控件的 GameObject 名称即为其在字典中的 key，同一名称可有多个类型不同的控件。</para>
/// </summary>
public class BasePanel : MonoBehaviour
{
    /// <summary>
    /// 控件字典。key 为子控件 GameObject 名称，value 为该名称下所有 UIBehaviour 组件列表。
    /// 同一名称可能对应多种类型（Button + Image），按类型查询时返回第一个匹配项。
    /// </summary>
    private Dictionary<string, List<UIBehaviour>> _controlDic = new Dictionary<string, List<UIBehaviour>>();

    /// <summary>
    /// 面板初始化时自动调用，查找并缓存常见类型的子控件。
    /// 同时为 Button 和 Toggle 自动注册事件监听。
    /// </summary>
    protected virtual void Awake()
    {
        FindChildControls<Button>();
        FindChildControls<Image>();
        FindChildControls<Text>();
        FindChildControls<Toggle>();
        FindChildControls<Slider>();
        FindChildControls<ScrollRect>();
        FindChildControls<InputField>();
    }

    /// <summary>
    /// 显示面板时调用。子类重写以实现入场动画、数据刷新等逻辑。
    /// </summary>
    public virtual void Show() { }

    /// <summary>
    /// 隐藏面板时调用。子类重写以实现退场动画、资源释放等逻辑。
    /// </summary>
    public virtual void Hide() { }

    /// <summary>
    /// 该面板是否能通过 ClosePanel（ESC）关闭。
    /// 默认为 true，需要特殊处理的面板（如 LoginPanel、GameOverPanel）可重写返回 false。
    /// </summary>
    public virtual bool CanBeClosedByClosePanel => true;

    /// <summary>
    /// 面板关闭前回调（通过 ClosePanel 关闭时调用，通过 HidePanel 关闭不调用）。
    /// 子类可重写以执行清理逻辑。
    /// </summary>
    public virtual void OnBeforeClose() { }

    /// <summary>
    /// Button 点击事件的统一回调入口。子类重写以响应按钮点击。
    /// </summary>
    /// <param name="btnName">被点击按钮的 GameObject 名称。</param>
    protected virtual void OnClick(string btnName) { }

    /// <summary>
    /// Toggle 值变化事件的统一回调入口。子类重写以响应开关切换。
    /// </summary>
    /// <param name="togName">状态变化的 Toggle 的 GameObject 名称。</param>
    /// <param name="value">Toggle 当前值（true = 选中）。</param>
    protected virtual void OnTogValueChanged(string togName, bool value) { }

    /// <summary>
    /// Slider 值变化事件的统一回调入口。子类重写以响应滑块值变化。
    /// </summary>
    /// <param name="sliderName">值变化的 Slider 的 GameObject 名称。</param>
    /// <param name="value">Slider 当前值（0~1）。</param>
    protected virtual void OnSliderValueChanged(string sliderName, float value) { }

    /// <summary>
    /// 根据控件名称和类型获取对应的 UI 组件。
    /// 查找范围仅限当前面板的子级，不含自身。
    /// </summary>
    /// <typeparam name="T">UIBehaviour 子类类型，如 Button、Image、Text 等。</typeparam>
    /// <param name="controlName">子控件 GameObject 的名称。</param>
    /// <returns>匹配的组件，未找到返回 null。</returns>
    public T GetControl<T>(string controlName) where T : UIBehaviour
    {
        // 先按名称找到对应的列表，再从中筛选出目标类型
        if (_controlDic.ContainsKey(controlName))
        {
            foreach (var control in _controlDic[controlName])
            {
                // 同一名称可能存在多个类型不同的组件（如同名 Button 和 Image），
                // 按类型匹配，返回第一个命中项
                if (control is T)
                    return control as T;
            }
        }
        return null;
    }

    /// <summary>
    /// 查找并注册指定类型的所有子控件，按 GameObject 名称归入字典。
    /// 同时自动为 Button 和 Toggle 注册事件监听，事件统一路由至子类虚方法。
    /// 同一名称下可能存在多个不同类型的组件，均存入同一个列表中。
    /// </summary>
    /// <typeparam name="T">要查找的 UIBehaviour 子类类型。</typeparam>
    private void FindChildControls<T>() where T : UIBehaviour
    {
        // 递归获取所有子级中的目标类型组件（含自身）
        T[] controls = GetComponentsInChildren<T>();
        foreach (T control in controls)
        {
            // 按 GameObject 名称归并，同名不同类型的组件存入同一列表
            if (_controlDic.ContainsKey(control.gameObject.name))
                _controlDic[control.gameObject.name].Add(control);
            else
                _controlDic.Add(control.gameObject.name, new List<UIBehaviour>() { control });

            // 为交互组件自动注册事件，无需子类手动绑定
            // 将 control.name 提取为局部变量，避免 lambda 捕获循环变量导致闭包问题
            if (control is Button)
            {
                string btnName = control.name;
                (control as Button).onClick.AddListener(() =>
                {
                    OnClick(btnName);
                });
            }
            else if (control is Toggle)
            {
                string togName = control.name;
                (control as Toggle).onValueChanged.AddListener((value) =>
                {
                    OnTogValueChanged(togName, value);
                });
            }
            else if (control is Slider)
            {
                string sldName = control.name;
                (control as Slider).onValueChanged.AddListener((value) =>
                {
                    OnSliderValueChanged(sldName, value);
                });
            }
        }
    }
}
}
