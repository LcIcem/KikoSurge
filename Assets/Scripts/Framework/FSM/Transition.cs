using System;

/// <summary>
/// 转换条件：From 在 Condition 为 true 时跳转到 To。支持 And / Or 组合多个条件。
/// </summary>
public class Transition
{
    public IFSMState From { get; } // 来源状态
    public IFSMState To { get; internal set; } // 目标状态
    public Func<bool> Condition { get; private set; } // 跳转条件

    /// <summary>构造转换条件</summary>
    public Transition(IFSMState from, IFSMState to, Func<bool> condition)
    {
        From = from;
        To = to;
        Condition = condition;
    }

    /// <summary>And 组合：两个条件同时满足才跳转</summary>
    public Transition And(Func<bool> other)
    {
        Func<bool> prev = Condition;
        Condition = () => prev() && other();
        return this;
    }

    /// <summary>Or 组合：任一条件满足即跳转</summary>
    public Transition Or(Func<bool> other)
    {
        Func<bool> prev = Condition;
        Condition = () => prev() || other();
        return this;
    }
}
