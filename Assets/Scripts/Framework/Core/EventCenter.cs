using System;
using System.Collections.Generic;
using UnityEngine;


public interface IEventCallback {}

public class ParamsCallback<T> : IEventCallback
{
    public List<Action<T>> callbacks = new List<Action<T>>();
}

public class VoidCallback : IEventCallback
{
    public List<Action> callbacks = new List<Action>();
}

/// <summary>
/// 事件中心
/// <para> 发布-订阅模式 </para>
/// </summary>
public class EventCenter : Singleton<EventCenter>
{
    // 键：事件ID  值：对应事件的回调列表
    // 之所以不用多播委托 是为了防止多播委托中 某个回调发生异常 导致 回调链中断的问题
    private readonly Dictionary<EventID, IEventCallback> listeners = new Dictionary<EventID, IEventCallback>();


    /// <summary>
    /// 发布事件(有参)
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="eventId">事件ID</param>
    /// <param name="body">事件体</param>
    public void Publish<T>(EventID eventId, T body)
    {
        // 如果能够在事件监听字典中找到该事件
        if (listeners.TryGetValue(eventId, out IEventCallback callbacks))
        {
            ParamsCallback<T> paramsCallback = callbacks as ParamsCallback<T>;
            // 遍历回调列表 调用每一个回调
            foreach (Action<T> callback in paramsCallback.callbacks)
            {
                // 异常捕获
                // 防止 某个回调 发生异常后 导致 整个回调链中断
                try
                {
                    callback?.Invoke(body);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

    /// <summary>
    /// 发布事件(无参)
    /// </summary>
    /// <param name="eventId">事件ID</param>
    public void Publish(EventID eventId)
    {
        // 如果能够在事件监听字典中找到该事件
        if (listeners.TryGetValue(eventId, out IEventCallback callbacks))
        {
            VoidCallback voidCallback = callbacks as VoidCallback;
            // 遍历回调列表 调用每一个回调
            foreach (Action callback in voidCallback.callbacks)
            {
                // 异常捕获
                // 防止 某个回调 发生异常后 导致 整个回调链中断
                try
                {
                    callback?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

    /// <summary>
    /// 订阅事件(有参)
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="eventId">事件ID</param>
    /// <param name="callback">回调函数</param>
    public void Subscribe<T>(EventID eventId, Action<T> callback)
    {
        // 如果事件监听字典里没有该事件 就创建该键
        if (!listeners.ContainsKey(eventId))
        {
            listeners[eventId] = new ParamsCallback<T>();
        }
        // 添加事件监听回调
        (listeners[eventId] as ParamsCallback<T>).callbacks.Add(callback);
    }

    /// <summary>
    /// 订阅事件(无参)
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="callback">回调函数</param>
    public void Subscribe(EventID eventId, Action callback)
    {
        // 如果事件监听字典里没有该事件 就创建该键
        if (!listeners.ContainsKey(eventId))
        {
            listeners[eventId] = new VoidCallback();
        }
        // 添加事件监听回调
        (listeners[eventId] as VoidCallback).callbacks.Add(callback);
    }

    /// <summary>
    /// 退订事件(有参)
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="eventId">事件ID</param>
    /// <param name="callback">回调函数</param>
    public void Unsubscribe<T>(EventID eventId, Action<T> callback)
    {
        // 如果事件监听字典里有该事件 就退订 否则 静默处理
        if (listeners.TryGetValue(eventId, out IEventCallback callbacks))
        {
            ParamsCallback<T> paramsCallback = callbacks as ParamsCallback<T>;
            paramsCallback.callbacks.Remove(callback);
            // 如果所有回调都退订了 就清理该字典键 防止积累无用键
            if (paramsCallback.callbacks.Count == 0)
                listeners.Remove(eventId);
        }
    }

    /// <summary>
    /// 退订事件(无参)
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="callback">回调函数</param>
    public void Unsubscribe(EventID eventId, Action callback)
    {
        // 如果事件监听字典里有该事件 就退订 否则 静默处理
        if (listeners.TryGetValue(eventId, out IEventCallback callbacks))
        {
            VoidCallback paramsCallback = callbacks as VoidCallback;
            paramsCallback.callbacks.Remove(callback);
            // 如果所有回调都退订了 就清理该字典键 防止积累无用键
            if (paramsCallback.callbacks.Count == 0)
                listeners.Remove(eventId);
        }
    }

    /// <summary>
    /// 清空指定事件
    /// </summary>
    /// <param name="eventId"></param>
    public void Clear(EventID eventId)
    {
        listeners.Remove(eventId);
    }

    /// <summary>
    /// 清空所有事件
    /// </summary>
    public void ClearAll()
    {
        listeners.Clear();
    }
}
