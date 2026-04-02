using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Unity 生命周期的内部驱动类。
/// <para>功能：</para>
/// <list type="bullet">
///   <item>承担 Update 帧循环的实际驱动</item>
///   <item>通过事件机制分发给外部注册者</item>
///   <item>挂载在 DontDestroyOnLoad 的 GameObject 上，保证全局生命周期不中断</item>
/// </list>
/// <para>注意：此类为内部实现类，外部代码应通过 MonoMgr 访问，禁止直接实例化或调用。</para>
/// </summary>
public class MonoController : MonoBehaviour
{
    private event UnityAction updateEvent;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        updateEvent?.Invoke();
    }

    /// <summary>注册 Update 帧回调。</summary>
    public void AddUpdateListener(UnityAction action) => updateEvent += action;

    /// <summary>取消注册 Update 帧回调。</summary>
    public void RemoveUpdateListener(UnityAction action) => updateEvent -= action;
}
