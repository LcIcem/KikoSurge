using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个计时任务的数据结构
/// </summary>
public class TimerTask
{
    /// <summary> 是否在运行中 </summary>
    public bool IsRunning { get; private set; }
    /// <summary> 是否为重复计时器 </summary>
    public bool IsRepeating { get; }

    private readonly float _duration; // 总时长（秒），定时任务的运行_duration秒后结束
    private float _remaining; // 剩余时间（秒）
    private readonly Action _callback; // 定时任务结束后调用的回调


    public TimerTask(float duration, Action callback, bool repeating)
    {
        _duration = duration;
        _callback = callback;
        _remaining = duration;
        IsRepeating = repeating;
        IsRunning = true;
    }

    /// <summary> 查询剩余时间（秒） </summary>
    public float GetRemaining() => _remaining;

    /// <summary>
    /// 推进计时器
    /// </summary>
    /// <param name="dt">时间增量（秒）</param>
    public void Tick(float dt)
    {
        // 如果计时器不在运行中 直接返回
        if (!IsRunning) return;

        // 按照时间增量递减剩余时间
        _remaining -= dt;

        // 如果计时器已经到达终点，调用回调
        if (_remaining <= 0f)
        {
            try
            {
                _callback?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            // 如果是重复计时器则重置剩余时间
            if (IsRepeating)
                _remaining = _duration;
            // 否则该计时器任务结束，将IsRunning设为false
            else
                IsRunning = false;
        }
    }
}
