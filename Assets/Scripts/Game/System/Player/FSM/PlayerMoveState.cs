using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 移动状态：设置 isMoving=true，持续更新移动向量。
/// 动画由 AnimatorController 根据 isMoving=true 自动播放行走动画。
/// </summary>
public class PlayerMoveState : StateBase
{
    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isMoving", true);
    }

    public override void Exec()
    {
        var player = Owner<Player>();
        _fsm.SetBool("isMoving", player.MoveDir.magnitude > 0.1f);
    }

    public override void Exit()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isMoving", false);
    }
}