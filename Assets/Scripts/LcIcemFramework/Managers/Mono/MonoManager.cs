using System.Collections;
using UnityEngine;
using UnityEngine.Events;

using LcIcemFramework.Core;

namespace LcIcemFramework.Managers.Mono
{
/// <summary>
/// 非 MonoBehaviour 类的生命周期管理器（单例）。
/// <para>
/// 功能：
/// <list type="bullet">
///   <item>为普通类提供 Update 帧回调注册能力，无需继承 MonoBehaviour</item>
///   <item>托管协程生命周期，非 Mono 类可直接调用 StartCoroutine / StopCoroutine</item>
/// </list>
/// </para>
/// <para>
/// 设计：内部持有 MonoController 实例，所有操作均委托至该实例。
/// </para>
/// </summary>
public class MonoManager : Singleton<MonoManager>
{
    private MonoController _controller;

    protected override void Init()
    {
        GameObject go = new GameObject("MonoController");
        _controller = go.AddComponent<MonoController>();
    }

    /// <summary>注册 Update 帧回调。</summary>
    /// <param name="action">无参无返回的回调方法。</param>
    public void AddUpdateListener(UnityAction action)
    {
        _controller.AddUpdateListener(action);
    }

    /// <summary>取消注册 Update 帧回调，需与 AddUpdateListener 配对使用。</summary>
    /// <param name="action">已注册的回调方法引用。</param>
    public void RemoveUpdateListener(UnityAction action)
    {
        _controller.RemoveUpdateListener(action);
    }

    /// <summary>注册 FixedUpdate 帧回调。</summary>
    /// <param name="action">无参无返回的回调方法。</param>
    public void AddFixedUpdateListener(UnityAction action)
    {
        _controller.AddFixedUpdateListener(action);
    }

    /// <summary>取消注册 FixedUpdate 帧回调，需与 AddFixedUpdateListener 配对使用。</summary>
    /// <param name="action">已注册的回调方法引用。</param>
    public void RemoveFixedUpdateListener(UnityAction action)
    {
        _controller.RemoveFixedUpdateListener(action);
    }

    /// <summary>启动协程。</summary>
    /// <param name="routine">协程方法。</param>
    /// <returns>Coroutine 实例，可用于 StopCoroutine。</returns>
    public Coroutine StartCoroutine(IEnumerator routine) => _controller.StartCoroutine(routine);

    /// <summary>通过 IEnumerator 引用停止协程。</summary>
    /// <param name="routine">启动协程时传入的同一 IEnumerator 引用。</param>
    public void StopCoroutine(IEnumerator routine) => _controller.StopCoroutine(routine);

    /// <summary>通过 Coroutine 实例停止协程。</summary>
    /// <param name="routine">StartCoroutine 返回值。</param>
    public void StopCoroutine(Coroutine routine) => _controller.StopCoroutine(routine);
}
}
