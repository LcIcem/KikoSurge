/// <summary>
/// FSM 状态接口
/// </summary>
public interface IFSMState
{
    /// <summary>进入状态时调用</summary>
    void Enter();

    /// <summary>每帧执行本状态行为</summary>
    void Exec();

    /// <summary>退出状态时调用</summary>
    void Exit();
}
