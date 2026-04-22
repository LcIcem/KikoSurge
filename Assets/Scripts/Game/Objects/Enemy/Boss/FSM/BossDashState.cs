using LcIcemFramework.FSM;
using UnityEngine;

public class BossDashState : StateBase
{
    private enum Phase
    {
        Dash,
        Stun
    }

    private Phase _phase;
    private float _timer;
    private Vector3 _dashTarget;

    public override void Enter()
    {
        var enemy = Owner<BossEnemyBase>();
        var bossFSM = _fsm as BossFSM;

        // 锁定目标点（玩家当前位置），停止寻路
        _dashTarget = enemy._player.position;
        enemy.StopChaseTarget();

        // 触发冲刺动画（使用 isDashing bool 保持循环）
        bossFSM.SetAnimatorBool("isMoving", false);
        bossFSM.SetAnimatorBool("isDashing", true);

        _phase = Phase.Dash;
        _timer = 0f;
    }

    public override void Exec()
    {
        var enemy = Owner<BossEnemyBase>();
        var bossFSM = _fsm as BossFSM;

        if (_phase == Phase.Dash)
        {
            // 直线冲刺向锁定目标点（不使用A*寻路）
            Vector2 dir = (_dashTarget - enemy.transform.position).normalized;
            enemy.DashRigidbody.MovePosition(
                enemy.DashRigidbody.position + dir * enemy.DashSpeed * Time.fixedDeltaTime);
            _timer += Time.deltaTime;

            // 更新朝向
            enemy.FacePlayer();

            // 到达目标点附近 或 超过冲刺时间，进入硬直
            float dist = Vector3.Distance(enemy.transform.position, _dashTarget);
            if (dist < 0.5f || _timer >= enemy.DashDuration)
            {
                _phase = Phase.Stun;
                _timer = 0f;
                bossFSM.SetAnimatorBool("isMoving", false);
                bossFSM.SetAnimatorBool("isDashing", false);

                // 开始冲刺冷却
                enemy.StartCooldown();
            }
        }
        else if (_phase == Phase.Stun)
        {
            _timer += Time.deltaTime;

            // 代码控制硬直时间（不依赖动画事件）
            if (_timer >= enemy.StunDuration)
            {
                // 硬直结束，返回 Chase 状态
                bossFSM.TransitionToChase();
            }
        }
    }

    public override void Exit()
    {
        var enemy = Owner<BossEnemyBase>();
        enemy.DashRigidbody.linearVelocity = Vector2.zero;
    }
}
