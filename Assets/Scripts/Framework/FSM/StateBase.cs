/// <summary>
/// 状态基类。
/// FSM 在 AddState 时自动注入 fsm 引用，状态通过 Owner() 获取外部数据。
/// </summary>
public abstract class StateBase : IFSMState
{
    protected FSM _fsm;

    /// <summary>设置所属FSM</summary>
    public void SetFSM(FSM fsm) => _fsm = fsm;

    /// <summary>获取 FSM 的 Owner（需要子类声明具体类型）</summary>
    protected T Owner<T>() => (T)_fsm.Owner;

    public virtual void Enter() { }
    public virtual void Exec() { }
    public virtual void Exit() { }
}