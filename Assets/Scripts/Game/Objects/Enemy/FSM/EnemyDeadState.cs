using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 死亡状态：停止所有行为。
/// Animator 参数：isDead=true，Dead trigger。
/// </summary>
public class EnemyDeadState : StateBase
{
    public override void Enter()
    {
        Debug.Log("进入Dead");
        var enemy = Owner<EnemyBase>();
        enemy.GetComponent<Collider2D>().enabled = false;
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        // FSM 驱动动画：播放死亡动画
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
        enemyFSM.SetAnimatorTrigger("dead");
    }

    public override void Exec()
    {
        // 死亡后不做任何事，等待对象池回收
    }
}