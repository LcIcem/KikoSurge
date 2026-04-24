using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 死亡状态：停止所有行为。
/// </summary>
public class EnemyDeadState : StateBase
{
    public override void Enter()
    {
        var enemy = Owner<EnemyBase>();
        enemy.GetComponent<Collider2D>().enabled = false;
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
        enemyFSM.SetAnimatorBool("dead", true);

        // 禁用 pathfinder，防止死亡后异步路径计算完成继续移动
        var pathfinder = enemy.GetComponent<EnemyPathfinder>();
        if (pathfinder != null)
            pathfinder.enabled = false;
    }

    public override void Exec()
    {
    }
}
