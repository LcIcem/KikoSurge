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
        var enemy = Owner<EnemyBase>();
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
        enemyFSM.SetAnimatorTrigger("attack");
    }

    public override void Exec()
    {
        var enemy = Owner<EnemyBase>();
        enemy.FacePlayer();
        enemy.AttackTarget();
    }
}