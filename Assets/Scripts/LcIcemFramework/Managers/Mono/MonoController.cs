using UnityEngine;
using UnityEngine.Events;

namespace LcIcemFramework.Managers.Mono
{
/// <summary>
/// Unity 生命周期的内部驱动类。
/// <para>
/// 功能：
/// <list type="bullet">
///   <item>承担 Update 帧循环的实际驱动</item>
///   <item>通过事件机制分发给外部注册者</item>
///   <item>挂载在 DontDestroyOnLoad 的 GameObject 上，保证全局生命周期不中断</item>
/// </list>
/// </para>
/// <para>
/// 注意：此类为内部实现类，外部代码应通过 MonoManager 访问，禁止直接实例化或调用。
/// </para>
/// </summary>
internal class MonoController : MonoBehaviour
{
    private event UnityAction _updateEvent;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        _updateEvent?.Invoke();
    }

    /// <summary>注册 Update 帧回调。</summary>
    public void AddUpdateListener(UnityAction action) => _updateEvent += action;

    /// <summary>取消注册 Update 帧回调。</summary>
    public void RemoveUpdateListener(UnityAction action) => _updateEvent -= action;
}
}
