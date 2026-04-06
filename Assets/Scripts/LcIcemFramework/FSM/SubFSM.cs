using System;

namespace LcIcemFramework.FSM
{
/// <summary>
/// 子状态机基类。
/// 继承此类，覆盖 OnSetup 添加状态和转换条件。进入父状态机时自动调用 OnSetup。
/// </summary>
public abstract class SubFSM : FSM, IFSMState
{
    private FSM _parentFSM; // 父状态机引用
    private IFSMState _returnTo; // 退出后返回的目标状态

    /// <summary>构造，传入父状态机</summary>
    protected SubFSM(FSM parent) : base(parent.Owner)
    {
        _parentFSM = parent;
    }

    /// <summary>配置状态和转换（子类必须实现）</summary>
    protected abstract override void OnSetup();

    /// <summary>子状态被父 Enter 时调用，共享父的参数字典</summary>
    public void Enter()
    {
        _returnTo = _parentFSM.PreviousState;
        IntParams = _parentFSM.IntParams;
        FloatParams = _parentFSM.FloatParams;
        BoolParams = _parentFSM.BoolParams;
        TriggerParams = _parentFSM.TriggerParams;
        Start();
    }

    /// <summary>子状态被父 Exec 时调用</summary>
    public void Exec() => Update();

    /// <summary>子状态被父 Exit 时调用，停止并通知父切换状态</summary>
    public void Exit()
    {
        Stop();
        _parentFSM.ChangeState(_returnTo);
    }
}
}
