/// <summary>
/// 状态基类。继承此类实现具体状态，通过 Owner&lt;T&gt;() 获取持有者对象。
/// </summary>
public abstract class StateBase : IFSMState
{
    protected FSM _fsm; // 所属状态机引用（AddState 时自动注入）

    /// <summary>注入所属 FSM</summary>
    public void SetFSM(FSM fsm) => _fsm = fsm;

    /// <summary>获取 FSM 的 Owner（需声明具体类型）</summary>
    protected T Owner<T>() => (T)_fsm.Owner;

    /// <summary>进入状态时调用</summary>
    public virtual void Enter() { }

    /// <summary>每帧执行</summary>
    public virtual void Exec() { }

    /// <summary>退出状态时调用</summary>
    public virtual void Exit() { }
}
