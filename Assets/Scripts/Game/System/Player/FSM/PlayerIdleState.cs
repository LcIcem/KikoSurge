using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 待机状态：设置 isMoving=false，无其他逻辑。
/// 动画由 AnimatorController 根据 isMoving=false 自动播放待机动画。
/// </summary>
public class PlayerIdleState : StateBase
{
    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;

        playerFSM.SetAnimatorBool("isIdle", true);
    }

    public override void Exec()
    {
        // 无额外逻辑，状态转换由 FSM 条件驱动
    }

    public override void Exit()
    {   
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isIdle", false);
    }
}