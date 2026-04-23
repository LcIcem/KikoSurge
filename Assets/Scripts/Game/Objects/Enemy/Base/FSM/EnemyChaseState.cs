using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 追踪状态：向玩家移动。
/// Animator 参数：isMoving=true（进入时设置，退出时清除）。
/// </summary>
public class EnemyChaseState : StateBase
{
    private const float WALK_SFX_INTERVAL = 0.42f;
    private float _walkSFXTimer;

    public override void Enter()
    {
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", true);

        _walkSFXTimer = 0f;
    }

    public override void Exec()
    {
        var enemy = Owner<EnemyBase>();
        if (enemy._player != null)
        {
            enemy.ChaseTarget();
            enemy.FacePlayer();

            _walkSFXTimer += Time.deltaTime;
            if (_walkSFXTimer >= WALK_SFX_INTERVAL)
            {
                _walkSFXTimer = 0f;
                enemy.PlaySFX(enemy.WalkSFX);
            }
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