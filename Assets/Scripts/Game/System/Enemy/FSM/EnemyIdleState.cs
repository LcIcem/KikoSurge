using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 待机状态：原地巡逻或静止。
/// Animator 参数：无（idle 由 isMoving=false 隐含）。
/// </summary>
public class EnemyIdleState : StateBase
{
    public override void Enter()
    {
        var enemy = Owner<EnemyBase>();
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        // FSM 驱动动画：停止移动
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
    }

    public override void Exec()
    {
        // 待机时可添加轻微巡逻逻辑
    }

    public override void Exit()
    {
    }
}