using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局计时器管理器
/// </summary>
public class TimerMgr : Singleton<TimerMgr>
{
    // 计时器字典  键：计时器id  值：计时器对象
    private Dictionary<int, TimerTask> tasks = new Dictionary<int, TimerTask>();
    private int nextId = 0; // 新增的计时器id 每次有一个新的计时器就 +1

    /// <summary> 暂停标识 </summary>
    public bool IsPaused { get; private set; }


    public TimerMgr()
    {
        // 注册到MonoMgr，通过里面的 MonoController 驱动 Update 帧循环
        MonoMgr.Instance.AddUpdateListener(OnUpdate);
    }

    private void OnUpdate()
    {
        // 如果 暂停 就停止计时
        if (IsPaused) return;

        // 获得当帧的时间增量 (unscaled)
        float dt = Time.unscaledDeltaTime;
        // 克隆一份key列表 避免foreach遍历字典时直接修改字典导致异常
        var ids = new List<int>(tasks.Keys);

        // 推进所有计时器
        foreach (var id in ids)
        {
            // 如果当前遍历的的计时器正在运行 就推进该计时器
            if (tasks[id].IsRunning)
            {
                tasks[id].Tick(dt);
            }
            else
            {
                // 否则直接删除该计时器
                tasks.Remove(id);
            }
        }
    }

    /// <summary>
    /// 添加延迟计时器
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="callback">到期回调</param>
    /// <returns>计时器 id，可用于取消计时器</returns>
    public int AddTimeOut(float delay, Action callback)
    {
        TimerTask task = new TimerTask(delay, callback, false);
        tasks[++nextId] = task;
        return nextId;
    }

    /// <summary>
    /// 添加重复计时器
    /// </summary>
    /// <param name="interval">重复间隔（秒）</param>
    /// <param name="callback">每次到期回调</param>
    /// <returns>计时器 id，可用于取消计时器</returns>
    public int AddRepeating(float interval, Action callback)
    {
        TimerTask task = new TimerTask(interval, callback, true);
        tasks[++nextId] = task;
        return nextId;
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
        tasks.Remove(timerId);
    }

    /// <summary>
    /// 清除所有计时器
    /// </summary>
    public void ClearAll()
    {
        tasks.Clear();
    }

    /// <summary>
    /// 获取指定计时器剩余时间
    /// </summary>
    public float GetRemainingTime(int timerId)
    {
        if (tasks.TryGetValue(timerId, out var task))
            return task.GetRemaining();
        return -1f;
    }

    /// <summary>
    /// 获取当前活动计时器数量
    /// </summary>
    public int GetActiveTimerCount() => tasks.Count;

    #region 日志
    private void Log(string msg) => Debug.Log($"[{GetType().Name}] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[{GetType().Name}] {msg}");
    private void LogError(string msg) => Debug.LogError($"[{GetType().Name}] {msg}");
    #endregion
}
