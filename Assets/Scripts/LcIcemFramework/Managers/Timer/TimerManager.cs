using System;
using System.Collections.Generic;
using UnityEngine;

using LcIcemFramework.Core;
using LcIcemFramework;

namespace LcIcemFramework
{

/// <summary>
/// 全局计时器管理器
/// </summary>
public class TimerManager : Singleton<TimerManager>
{
    // 计时器字典  键：计时器id  值：计时器对象
    private Dictionary<int, TimerTask> _tasks = new Dictionary<int, TimerTask>();
    private int _nextId = 0; // 新增的计时器id 每次有一个新的计时器就 +1

    // BuffTick监听器列表
    private List<Action<float>> _buffTickListeners = new List<Action<float>>();

    /// <summary> 暂停标识 </summary>
    public bool IsPaused { get; private set; }

    protected override void Init()
    {
        // 注册到MonoManager，通过里面的 MonoController 驱动 Update 帧循环
        MonoManager.Instance.AddUpdateListener(OnUpdate);
    }

    private void OnUpdate()
    {
        // 如果 暂停 就停止计时
        if (IsPaused) return;

        // 获得当帧的时间增量
        float unscaledDt = Time.unscaledDeltaTime;
        float scaledDt = Time.deltaTime;
        // 克隆一份key列表 避免foreach遍历字典时直接修改字典导致异常
        var ids = new List<int>(_tasks.Keys);

        // 推进所有计时器
        foreach (var id in ids)
        {
            // 如果当前遍历的的计时器正在运行 就推进该计时器
            if (_tasks[id].IsRunning)
            {
                // 根据计时器类型选用对应的 deltaTime
                float dt = _tasks[id].IsUnscaled ? unscaledDt : scaledDt;
                _tasks[id].Tick(dt);
            }
            else
            {
                // 否则直接删除该计时器
                _tasks.Remove(id);
            }
        }

        // 驱动Buff Tick
        foreach (var listener in _buffTickListeners)
        {
            listener.Invoke(scaledDt);
        }
    }

    /// <summary>
    /// 添加延迟计时器（受 Time.timeScale 影响）
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="callback">到期回调</param>
    /// <returns>计时器 id，可用于取消计时器</returns>
    public int AddTimeOut(float delay, Action callback)
    {
        TimerTask task = new TimerTask(delay, callback, false, false);
        _tasks[++_nextId] = task;
        return _nextId;
    }

    /// <summary>
    /// 添加延迟计时器（不受 Time.timeScale 影响）
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="callback">到期回调</param>
    /// <returns>计时器 id，可用于取消计时器</returns>
    public int AddTimeOutUnscaled(float delay, Action callback)
    {
        TimerTask task = new TimerTask(delay, callback, false, true);
        _tasks[++_nextId] = task;
        return _nextId;
    }

    /// <summary>
    /// 添加重复计时器（受 Time.timeScale 影响）
    /// </summary>
    /// <param name="interval">重复间隔（秒）</param>
    /// <param name="callback">每次到期回调</param>
    /// <returns>计时器 id，可用于取消计时器</returns>
    public int AddRepeating(float interval, Action callback)
    {
        TimerTask task = new TimerTask(interval, callback, true, false);
        _tasks[++_nextId] = task;
        return _nextId;
    }

    /// <summary>
    /// 添加重复计时器（不受 Time.timeScale 影响）
    /// </summary>
    /// <param name="interval">重复间隔（秒）</param>
    /// <param name="callback">每次到期回调</param>
    /// <returns>计时器 id，可用于取消计时器</returns>
    public int AddRepeatingUnscaled(float interval, Action callback)
    {
        TimerTask task = new TimerTask(interval, callback, true, true);
        _tasks[++_nextId] = task;
        return _nextId;
    }

    /// <summary>
    /// 暂停 所有计时器
    /// </summary>
    public void Pause()
    {
        IsPaused = true;
    }

    /// <summary>
    /// 取消暂停 所有计时器
    /// </summary>
    public void Resume()
    {
        IsPaused = false;
    }

    /// <summary>
    /// 取消计时器
    /// </summary>
    /// <param name="timerId"></param>
    public void Clear(int timerId)
    {
        _tasks.Remove(timerId);
    }

    /// <summary>
    /// 清除所有计时器
    /// </summary>
    public void ClearAll()
    {
        _tasks.Clear();
    }

    /// <summary>
    /// 获取指定计时器剩余时间
    /// </summary>
    public float GetRemainingTime(int timerId)
    {
        if (_tasks.TryGetValue(timerId, out var task))
            return task.GetRemaining();
        return -1f;
    }

    /// <summary>
    /// 获取当前活动计时器数量
    /// </summary>
    public int GetActiveTimerCount() => _tasks.Count;

    #region BuffTick

    /// <summary>
    /// 注册BuffTick监听器（由BuffManager调用）
    /// </summary>
    public void AddBuffTickListener(Action<float> listener)
    {
        if (!_buffTickListeners.Contains(listener))
            _buffTickListeners.Add(listener);
    }

    /// <summary>
    /// 移除BuffTick监听器
    /// </summary>
    public void RemoveBuffTickListener(Action<float> listener)
    {
        _buffTickListeners.Remove(listener);
    }

    #endregion

    #region 日志
    private void Log(string msg) => Debug.Log($"[TimerManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[TimerManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[TimerManager] {msg}");
    #endregion
}
}
