using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 追踪状态：向玩家移动。
/// Animator 参数：isMoving=true（进入时设置，退出时清除）。
/// </summary>
public class EnemyChaseState : StateBase
{
    public override void Enter()
    {
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", true);
    }

    public override void Exec()
    {
        var enemy = Owner<EnemyBase>();
        if (enemy._player != null)
        {
            enemy.ChaseTarget();
            enemy.FacePlayer();
        }
    }

    public override void Exit()
    {
        var enemy = Owner<EnemyBase>();
        enemy.StopChaseTarget();
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
    }
}