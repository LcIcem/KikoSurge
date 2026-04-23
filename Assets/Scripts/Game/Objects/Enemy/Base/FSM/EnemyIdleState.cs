using LcIcemFramework.FSM;
using UnityEngine;

using Random = UnityEngine.Random;

/// <summary>
/// 待机状态：原地巡逻或静止。
/// Animator 参数：isMoving（由状态机驱动）。
/// </summary>
public class EnemyIdleState : StateBase
{
    private float _waitTimer;
    private Vector3 _patrolOrigin;

    public override void Enter()
    {
        var enemy = Owner<EnemyBase>();
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);

        // 记录巡逻原点（进入待机时的位置）
        _patrolOrigin = enemy.transform.position;
        enemy.UpdatePatrolOrigin(_patrolOrigin);

        // 初始化等待状态
        _waitTimer = 0f;
        enemy.SetPatrolWaiting(false);

        // 立即选择第一个巡逻目标
        enemy.PickNewPatrolTarget();
    }

    public override void Exec()
    {
        var enemy = Owner<EnemyBase>();

        // 检测玩家是否进入检测范围（由状态转换处理，这里只管巡逻逻辑）
        if (enemy.DistanceToPlayer < enemy.DetectRange)
        {
            return; // 状态转换会处理
        }

        // 处于等待状态
        if (enemy.IsPatrolWaiting)
        {
            _waitTimer += Time.deltaTime;
            if (_waitTimer >= enemy.PatrolWaitTime)
            {
                // 等待结束，选择新的巡逻目标
                _waitTimer = 0f;
                enemy.SetPatrolWaiting(false);
                enemy.PickNewPatrolTarget();
            }
            return;
        }

        // 正在移动，检查是否到达目标点
        if (enemy.IsPathfinderMoving)
        {
            EnemyFSM enemyFSM = _fsm as EnemyFSM;
            enemyFSM.SetAnimatorBool("isMoving", true);
            enemy.FacePatrolDirection();
        }
        else
        {
            // 到达目标点，开始等待
            enemy.SetPatrolWaiting(true);
            _waitTimer = 0f;

            EnemyFSM enemyFSM = _fsm as EnemyFSM;
            enemyFSM.SetAnimatorBool("isMoving", false);
        }
    }

    public override void Exit()
    {
        var enemy = Owner<EnemyBase>();
        enemy.StopPatrol();

        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);
    }
}
