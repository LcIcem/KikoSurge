using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 死亡状态：停止所有行为。
/// </summary>
public class PlayerDeadState : StateBase
{
    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorTrigger("Dead");
        playerFSM.SetAnimatorBool("isMoving", false);
    }

    public override void Exec()
    {
        // 死亡后不做任何事
    }
}