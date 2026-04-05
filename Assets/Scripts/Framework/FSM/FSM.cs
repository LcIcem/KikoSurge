using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态机基类。
/// 继承此类，覆盖 OnSetup 添加状态和转换条件，外部通过 SetBool/SetTrigger/Update 驱动。
/// </summary>
public abstract class FSM
{
    // 状态节点
    internal protected IFSMState _currentState;
    internal protected IFSMState _prevState;
    protected IFSMState _entryState;
    protected IFSMState _exitState;
    protected IFSMState _anyState;

    // 状态与跳转
    private readonly Dictionary<string, IFSMState> _states = new();
    private readonly Dictionary<IFSMState, List<Transition>> _transitions = new();

    // 运行状态
    protected bool _isRunning;

    // 参数字典（internal protected 供 SubFSM 共享访问）
    internal protected Dictionary<string, int> _intParams = new();
    internal protected Dictionary<string, float> _floatParams = new();
    internal protected Dictionary<string, bool> _boolParams = new();
    internal protected Dictionary<string, bool> _triggerParams = new();

    // 属性
    public object Owner { get; }
    public IFSMState CurrentState => _currentState;
    public string CurrentStateName => _currentState?.GetType().Name ?? "None";
    public bool HasExited => _currentState == null && !_isRunning;

    /// <summary>构造状态机，自动调用 OnSetup</summary>
    protected FSM(object owner)
    {
        Owner = owner;
        _entryState = new FSMNode(() => { }, () => { });
        _exitState = new FSMNode(() => { }, () => Stop());
        _anyState = new FSMNode(() => { }, () => { });
        _transitions[_entryState] = new List<Transition>();
        _transitions[_exitState] = new List<Transition>();
        _transitions[_anyState] = new List<Transition>();
        OnSetup();
    }

    /// <summary>配置状态和转换（子类必须实现）</summary>
    protected abstract void OnSetup();

    // 状态管理
    public void AddState(IFSMState state)
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
        if (state is SubFSM sub)
            sub.BindParent(this);

    }

    public IFSMState GetState(string name)
        => _states.TryGetValue(name, out var state) ? state : null;

    // 转换条件
    private void AddTransitionToList(IFSMState from, Transition t)
    {
        if (!_transitions.TryGetValue(from, out var list))
        {
            list = new List<Transition>();
            _transitions[from] = list;
        }
        list.Add(t);
    }

    public Transition AddTransition(IFSMState from, IFSMState to, Func<bool> condition)
    {
        var t = new Transition(from, to, condition);
        AddTransitionToList(from, t);
        return t;
    }

    public Transition AddAnyTransition(IFSMState to, Func<bool> condition)
    {
        var t = new Transition(_anyState, to, condition);
        AddTransitionToList(_anyState, t);
        return t;
    }

    // 参数管理
    public void SetInt(string name, int value) => _intParams[name] = value;
    public int GetInt(string name) => _intParams.TryGetValue(name, out var v) ? v : 0;

    public void SetFloat(string name, float value) => _floatParams[name] = value;
    public float GetFloat(string name) => _floatParams.TryGetValue(name, out var v) ? v : 0f;

    public void SetBool(string name, bool value) => _boolParams[name] = value;
    public bool GetBool(string name) => _boolParams.TryGetValue(name, out var v) && v;

    public void SetTrigger(string name) => _triggerParams[name] = true;
    public bool CheckTrigger(string name)
    {
        if (_triggerParams.TryGetValue(name, out var v) && v)
        {
            _triggerParams[name] = false;
            return true;
        }
        return false;
    }

    // 生命周期
    public virtual void Start()
    {
        _isRunning = true;
        ChangeState(_entryState);
    }

    public virtual void Update()
    {
        if (_currentState == null) return;

        if (_transitions.TryGetValue(_anyState, out var anyList))
        {
            foreach (var t in anyList)
            {
                if (t.Condition() && t.To != null)
                {
                    ChangeState(t.To);
                    return;
                }
            }
        }

        if (_transitions.TryGetValue(_currentState, out var list))
        {
            foreach (var t in list)
            {
                if (t.Condition() && t.To != null)
                {
                    ChangeState(t.To);
                    return;
                }
            }
        }

        _currentState.Exec();
    }

    public void ChangeState(IFSMState newState)
    {
        if (newState == null || newState == _currentState) return;

        _currentState?.Exit();
        _prevState = _currentState;
        _currentState = newState;

        newState.Enter();
    }

    public void GotoEntry()
    {
        if (_entryState != null) ChangeState(_entryState);
    }

    public virtual void Stop()
    {
        _currentState?.Exit();
        _currentState = null;
        _isRunning = false;
    }

    // 内置状态节点
    private class FSMNode : IFSMState
    {
        private readonly Action _onEnter;
        private readonly Action _onExec;
        private readonly Action _onExit;

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
