using System.Collections;
using UnityEngine;
using UnityEngine.Events;

using LcIcemFramework.Core;
using LcIcemFramework;

namespace LcIcemFramework
{

/// <summary>
/// 资源管理器，提供 Resources 同步/异步加载接口。
/// <para>功能：</para>
/// <list type="bullet">
///   <item>同步加载资源，GameObject 自动实例化后返回，Asset 直接返回</item>
///   <item>异步加载资源，配合回调使用，同步/异步行为同上</item>
/// </list>
/// <para>注意：当前实现基于 Resources 目录，生产环境建议替换为 Addressables 或 AssetBundle。</para>
/// </summary>
public class ResManager : Singleton<ResManager>
{
    protected override void Init() { }
    /// <summary>
    /// 同步加载资源。
    /// </summary>
    /// <typeparam name="T">资源类型（Object 子类）。</typeparam>
    /// <param name="name">Resources 路径。</param>
    /// <returns>若为 GameObject 则返回实例化后的对象，否则返回资源本身。</returns>
    public T Load<T>(string name) where T : Object
    {
        T res = Resources.Load<T>(name);
        if (res == null)
        {
            LogWarning($"资源加载失败: {name}");
            return null;
        }
        if (res is GameObject)
            return Object.Instantiate(res);
        else
            return res;
    }

    /// <summary>
    /// 异步加载资源，加载完成后通过回调返回。
    /// </summary>
    /// <typeparam name="T">资源类型（Object 子类）。</typeparam>
    /// <param name="name">Resources 路径。</param>
    /// <param name="callback">加载完成回调。</param>
    public void LoadAsync<T>(string name, UnityAction<T> callback) where T : Object
    {
        MonoManager.Instance.StartCoroutine(LoadAsyncCoroutine<T>(name, callback));
    }

    /// <summary>
    /// 异步加载协程。使用 ResourceRequest.asset 判断是否实例化，完成后触发回调。
    /// </summary>
    private IEnumerator LoadAsyncCoroutine<T>(string name, UnityAction<T> callback) where T : Object
    {
        ResourceRequest r = Resources.LoadAsync<T>(name);
        yield return r;

        if (r.asset == null)
        {
            LogWarning($"异步资源加载失败: {name}");
            callback?.Invoke(null);
            yield break;
        }
        if (r.asset is GameObject)
            callback?.Invoke(Object.Instantiate(r.asset) as T);
        else
            callback?.Invoke(r.asset as T);
    }

    #region 日志
    private void Log(string msg) => Debug.Log($"[ResManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[ResManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[ResManager] {msg}");
    #endregion
}
}
