using System;

/// <summary>
/// 子状态机基类。
/// 继承此类，在构造行数中覆盖 OnSetup 添加状态和转换条件。进入父状态机时自动调用 OnSetup。
/// </summary>
public abstract class SubFSM : FSM, IFSMState
{
    private FSM _parentFSM;
    private IFSMState _parentExitTarget;

    /// <summary>构造子状态机（Owner 继承自父 FSM）</summary>
    protected SubFSM(FSM parent)
        : base(parent.Owner)
    {
        _parentFSM = parent;
    }

    /// <summary>绑定父状态机（由父 FSM 的 AddState 调用）</summary>
    internal void BindParent(FSM parent)
    {
        _parentFSM = parent;
    }

    /// <summary>配置状态和转换（子类实现）</summary>
    protected override void OnSetup() { }

    /// <summary>子状态被父 Enter 时调用，自动设置退出目标并初始化</summary>
    public void Enter()
    {
        _parentExitTarget = _parentFSM._prevState;
        // 共享父的参数字典
        _intParams = _parentFSM._intParams;
        _floatParams = _parentFSM._floatParams;
        _boolParams = _parentFSM._boolParams;
        _triggerParams = _parentFSM._triggerParams;
        Start();
    }

    /// <summary>子状态被父 Exec 时自动更新</summary>
    public void Exec() => Update();

    /// <summary>子状态被父 Exit 时自动终止</summary>
    public void Exit()
    {
        Stop();
        _parentFSM?.ChangeState(_parentExitTarget);
    }

    /// <summary>退出子状态机，并通知父切换回 parentExitTarget</summary>
    public override void Stop()
    {
        _currentState?.Exit();
        _currentState = null;
        _isRunning = false;
    }
}
