using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 攻击状态：攻击玩家。
/// Animator 参数：isMoving=false，Attack trigger（进入状态时一次性触发）。
/// </summary>
public class EnemyAttackState : StateBase
{
    public override void Enter()
    {
        Debug.Log("进入Attack");

        EnemyBase enemy = Owner<EnemyBase>();
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        // 停止移动，触发攻击动画
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
        enemyFSM.SetAnimatorTrigger("attack");
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
        Debug.Log("退出Attack");
    }
}