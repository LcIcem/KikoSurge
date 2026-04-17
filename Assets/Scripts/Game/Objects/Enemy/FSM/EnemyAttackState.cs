using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 攻击状态：攻击玩家。
/// 攻击流程：进入状态 → 攻击动画 → 攻击生效时间点触发伤害 → 动画结束 → 进入冷却 → 冷却结束可再次攻击
/// CD独立计算，在EnemyBase.Update()中始终递减，不管当前什么状态。
/// 如果在冷却期间离开攻击范围，退出攻击状态但不重置CD，下次进入会继续CD进度。
/// </summary>
public class EnemyAttackState : StateBase
{
    // 内部阶段
    private enum Phase
    {
        Attack,   // 攻击动画阶段
        Cooldown  // 冷却阶段
    }
    private Phase _phase = Phase.Attack;

    public override void Enter()
    {
        var enemy = Owner<EnemyBase>();
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        EnemyFSM enemyFSM = _fsm as EnemyFSM;
        enemyFSM.SetAnimatorBool("isMoving", false);

        // 重置攻击计时器和触发标记，但不重置CD（CD独立）
        enemy.ResetAttackStateNoCooldown();

        // 根据CD状态决定阶段
        if (enemy.IsCooldownFinished())
        {
            // CD已结束，进入攻击阶段并触发攻击动画
            enemyFSM.SetAnimatorTrigger("attack");
            _phase = Phase.Attack;
        }
        else
        {
            // CD未结束，进入冷却阶段等待
            _phase = Phase.Cooldown;
        }
    }

    public override void Exec()
    {
        var enemy = Owner<EnemyBase>();
        enemy.FacePlayer();

        if (_phase == Phase.Attack)
        {
            // 更新攻击计时器
            enemy.UpdateAttackTimer(Time.deltaTime);

            float timer = enemy.GetAttackTimer();
            float hitTime = enemy.AttackHitTime;
            float duration = enemy.AttackDuration;

            if (timer >= hitTime && !enemy.IsAttackHitTriggered())
            {
                // 到达攻击生效时间点，触发伤害
                enemy.TriggerAttackHit();
            }

            if (timer >= duration)
            {
                // 攻击动画结束，进入冷却阶段
                enemy.StartCooldown();
                _phase = Phase.Cooldown;
            }
        }
        else if (_phase == Phase.Cooldown)
        {
            // 冷却期间检查是否离开攻击范围
            if (!enemy.IsInAttackRange())
            {
                // 离开攻击范围，标记攻击完成后退出（不重置CD，下次进入攻击状态会继续当前CD进度）
                enemy.ShouldExitAfterAttack = true;
                return;
            }

            if (enemy.IsCooldownFinished())
            {
                // 冷却结束，可以再次攻击
                // 重新触发攻击动画
                EnemyFSM enemyFSM = _fsm as EnemyFSM;
                enemyFSM.SetAnimatorTrigger("attack");
                enemy.ResetAttackState();
                _phase = Phase.Attack;
            }
        }
    }

    public override void Exit()
    {
        // 退出时不重置CD，CD独立于攻击状态
        // 下次进入攻击状态时会继续当前的CD进度
    }
}