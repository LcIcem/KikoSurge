using System;
using System.Collections.Generic;
using UnityEngine;

namespace LcIcemFramework.FSM
{
/// <summary>
/// 状态机基类。
/// 继承此类，覆盖 OnSetup 添加状态和转换条件，外部通过 Start/Update/Stop + Set/Get 驱动。
/// </summary>
public abstract class FSM
{
    // ========== 公开属性 ==========
    public object Owner { get; } // 持有者对象，由构造传入
    public IFSMState CurrentState { get; protected set; } // 当前状态
    public IFSMState PreviousState { get; protected set; } // 上一个状态
    public IFSMState EntryState { get; } // 入口节点，Enter 为空
    public IFSMState ExitState { get; } // 出口节点，Enter 调用 Stop
    public IFSMState AnyState { get; } // 全局节点，任意状态均可跳转
    public string CurrentStateName => CurrentState?.GetType().Name ?? "None"; // 当前状态类型名
    public bool IsRunning => _isRunning; // 是否在运行

    // ========== 参数字典 ==========
    public Dictionary<string, int> IntParams { get; protected set; } = new(); // 整数参数
    public Dictionary<string, float> FloatParams { get; protected set; } = new(); // 浮点参数
    public Dictionary<string, bool> BoolParams { get; protected set; } = new(); // 布尔参数
    public Dictionary<string, bool> TriggerParams { get; protected set; } = new(); // 触发器参数（一次性）

    // ========== 私有字段 ==========
    private readonly Dictionary<string, IFSMState> _states = new(); // 状态注册表（类型名 → 实例）
    private readonly Dictionary<IFSMState, List<Transition>> _transitions = new(); // 转换条件表（来源状态 → 转换列表）
    protected bool _isRunning; // 是否在运行

    // ========== 构造 ==========
    protected FSM(object owner)
    {
        Owner = owner;

        EntryState = new FSMNode(() => { }, () => { }); // Enter 为空，Exec 为空
        ExitState  = new FSMNode(() => { }, () => Stop()); // Enter 为空，Exec 调用 Stop
        AnyState   = new FSMNode(() => { }, () => { }); // Enter 为空，Exec 为空

        _transitions[EntryState] = new List<Transition>();
        _transitions[ExitState] = new List<Transition>();
        _transitions[AnyState]  = new List<Transition>();

        OnSetup();
    }

    /// <summary>配置状态和转换（子类必须实现）</summary>
    protected abstract void OnSetup();

    // ========== 公开方法 ==========
    /// <summary>启动，从 Entry 自动跳转首个状态</summary>
    public virtual void Start()
    {
        _isRunning = true;
        ChangeState(EntryState);
    }

    /// <summary>每帧：检查 Any 转换 → 检查当前转换 → Exec</summary>
    public virtual void Update()
    {
        if (!_isRunning) return;

        if(TryTransitionFrom(AnyState))
        {
            CurrentState.Exec();
            return;
        }
        TryTransitionFrom(CurrentState);
        CurrentState.Exec();
    }

    /// <summary>切换到指定状态</summary>
    public void ChangeState(IFSMState newState)
    {
        if (newState == null || newState == CurrentState) return;

        CurrentState?.Exit();
        PreviousState = CurrentState;
        CurrentState = newState;
        newState.Enter();
    }

    /// <summary>停止状态机</summary>
    public virtual void Stop()
    {
        CurrentState?.Exit();
        CurrentState = null;
        _isRunning = false;
    }

    // ========== 公开参数方法 ==========
    /// <summary>设置整数参数</summary>
    public void SetInt(string name, int value)
    {
        if (IntParams.ContainsKey(name)) IntParams[name] = value;
    }

    /// <summary>获取整数参数，不存在返回 0</summary>
    public int GetInt(string name) => IntParams.TryGetValue(name, out int v) ? v : 0;

    /// <summary>设置浮点参数</summary>
    public void SetFloat(string name, float value)
    {
        if (FloatParams.ContainsKey(name)) FloatParams[name] = value;
    }

    /// <summary>获取浮点参数，不存在返回 0f</summary>
    public float GetFloat(string name) => FloatParams.TryGetValue(name, out float v) ? v : 0f;

    /// <summary>设置布尔参数</summary>
    public void SetBool(string name, bool value)
    {
        if (BoolParams.ContainsKey(name)) BoolParams[name] = value;
    }

    /// <summary>获取布尔参数，不存在返回 false</summary>
    public bool GetBool(string name) => BoolParams.TryGetValue(name, out bool v) && v;

    /// <summary>设置触发器</summary>
    public void SetTrigger(string name)
    {
        if (TriggerParams.ContainsKey(name)) TriggerParams[name] = true;
    }

    /// <summary>检查触发器，满足后自动重置为 false</summary>
    public bool CheckTrigger(string name)
    {
        if (TriggerParams.TryGetValue(name, out bool v) && v)
        {
            TriggerParams[name] = false;
            return true;
        }
        return false;
    }

    // ========== 保护方法 ==========
    /// <summary>声明整数参数</summary>
    protected void AddInt(string name, int value = 0)
    {
        if (IntParams.ContainsKey(name)) { Debug.LogWarning($"[FSM] AddInt: 参数 {name} 已存在，跳过"); return; }
        IntParams[name] = value;
    }

    /// <summary>删除整数参数</summary>
    protected void RemoveInt(string name)
    {
        if (!IntParams.Remove(name)) Debug.LogWarning($"[FSM] RemoveInt: 参数 {name} 不存在");
    }

    /// <summary>声明浮点参数</summary>
    protected void AddFloat(string name, float value = 0f)
    {
        if (FloatParams.ContainsKey(name)) { Debug.LogWarning($"[FSM] AddFloat: 参数 {name} 已存在，跳过"); return; }
        FloatParams[name] = value;
    }

    /// <summary>删除浮点参数</summary>
    protected void RemoveFloat(string name)
    {
        if (!FloatParams.Remove(name)) Debug.LogWarning($"[FSM] RemoveFloat: 参数 {name} 不存在");
    }

    /// <summary>声明布尔参数</summary>
    protected void AddBool(string name, bool value = false)
    {
        if (BoolParams.ContainsKey(name)) { Debug.LogWarning($"[FSM] AddBool: 参数 {name} 已存在，跳过"); return; }
        BoolParams[name] = value;
    }

    /// <summary>删除布尔参数</summary>
    protected void RemoveBool(string name)
    {
        if (!BoolParams.Remove(name)) Debug.LogWarning($"[FSM] RemoveBool: 参数 {name} 不存在");
    }

    /// <summary>声明触发器参数</summary>
    protected void AddTrigger(string name)
    {
        if (TriggerParams.ContainsKey(name)) { Debug.LogWarning($"[FSM] AddTrigger: 参数 {name} 已存在，跳过"); return; }
        TriggerParams[name] = false;
    }

    /// <summary>删除触发器参数</summary>
    protected void RemoveTrigger(string name)
    {
        if (!TriggerParams.Remove(name)) Debug.LogWarning($"[FSM] RemoveTrigger: 参数 {name} 不存在");
    }

    /// <summary>注册状态，按类型名索引，自动注入 FSM 引用</summary>
    protected void AddState(IFSMState state)
    {
        if (state == null)
        {
            Debug.LogError("[FSM] AddState: state 不能为 null");
            return;
        }

        string name = state.GetType().Name;
        if (_states.ContainsKey(name))
        {
            Debug.LogWarning($"[FSM] AddState: 状态 {name} 已存在，跳过");
            return;
        }

        _states[name] = state;

        if (state is StateBase sb)
            sb.SetFSM(this);
    }

    /// <summary>按类型名查找状态，不存在返回 null</summary>
    protected IFSMState GetState(string name)
        => _states.TryGetValue(name, out IFSMState state) ? state : null;

    /// <summary>添加转换条件，返回 Transition</summary>
    protected Transition AddTransition(IFSMState from, IFSMState to, Func<bool> condition)
    {
        Transition t = new Transition(from, to, condition);

        if (!_transitions.TryGetValue(from, out List<Transition> list))
        {
            list = new List<Transition>();
            _transitions[from] = list;
        }

        list.Add(t);
        return t;
    }

    /// <summary>添加全局转换，从任意状态均可触发</summary>
    protected Transition AddAnyTransition(IFSMState to, Func<bool> condition)
        => AddTransition(AnyState, to, condition);

    // ========== 私有方法 ==========
    /// <summary>检查 from 的所有转换条件，找到第一个满足的则跳转并返回 true</summary>
    private bool TryTransitionFrom(IFSMState from)
    {
        if (!_transitions.TryGetValue(from, out List<Transition> list)) return false;

        foreach (Transition t in list)
        {
            if (t.Condition() && t.To != null)
            {
                ChangeState(t.To);
                return true;
            }
        }
        return false;
    }

    // ========== 内置状态节点 ==========
    private class FSMNode : IFSMState
    {
        private readonly Action _onEnter; // 进入回调
        private readonly Action _onExec; // 执行回调
        private readonly Action _onExit; // 退出回调

        public FSMNode(Action onEnter, Action onExit, Action onExec = null)
        {
            _onEnter = onEnter;
            _onExit = onExit;
            _onExec = onExec ?? (() => { });
        }

        public void Enter() => _onEnter();
        public void Exec() => _onExec();
        public void Exit() => _onExit();
    }
}
}
