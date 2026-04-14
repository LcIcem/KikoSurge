using LcIcemFramework.FSM;
using Pathfinding;
using UnityEngine;

/// <summary>
/// 追踪状态：向玩家移动。
/// Animator 参数：isMoving=true（进入时设置，退出时清除）。
/// </summary>
public class EnemyChaseState : StateBase
{
    public override void Enter()
    {
        var enemy = Owner<EnemyBase>();
        enemy.GetComponent<AIDestinationSetter>().enabled = true;
        // FSM 驱动动画：播放行走动画
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", true);
    }

    public override void Exec()
    {
        var enemy = Owner<EnemyBase>();
        if (enemy._player != null)
        {
            enemy.MoveTo(enemy._player);
            enemy.FacePlayer();
        }
    }

    public override void Exit()
    {
        // FSM 驱动动画：停止行走动画
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
    }
}