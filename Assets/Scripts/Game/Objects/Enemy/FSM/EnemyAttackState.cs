using LcIcemFramework.FSM;
using Pathfinding;
using UnityEngine;

/// <summary>
/// 攻击状态：攻击玩家。
/// Animator 参数：isMoving=false，Attack trigger（进入状态时一次性触发）。
/// </summary>
public class EnemyAttackState : StateBase
{
    public override void Enter()
    {
        var enemy = Owner<EnemyBase>();
        enemy.GetComponent<AIDestinationSetter>().enabled = false;
        // FSM 驱动动画：停止移动，触发攻击动画
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
        enemyFSM.SetAnimatorTrigger("Attack");
    }

    public override void Exec()
    {
        var enemy = Owner<EnemyBase>();
        enemy.FacePlayer();
        // AttackTarget 处理伤害和冷却，不直接操作 Animator
        enemy.AttackTarget();
    }

    public override void Exit()
    {
    }
}