using LcIcemFramework.FSM;
using UnityEngine;

public class BossFSM : EnemyFSM
{
    // 使用 new 关键字隐藏基类属性
    public new EnemyIdleState Idle { get; private set; }
    public new EnemyChaseState Chase { get; private set; }
    public new EnemyDeadState Dead { get; private set; }
    public BossDashState Dash { get; private set; }

    private BossEnemyBase _bossEnemy => Owner as BossEnemyBase;

    public BossFSM(EnemyBase enemy, Animator animator) : base(enemy, animator)
    {
    }

    /// <summary>
    /// 切换到 Chase 状态
    /// </summary>
    public void TransitionToChase()
    {
        ChangeState(Chase);
    }

    public override void Update()
    {
        if (!_isRunning) return;

        _bossEnemy.TickBirthStagger();
        Debug.Log($"[BossFSM] IsInBirthStagger={_bossEnemy.IsInBirthStagger}, Timer={_bossEnemy.BirthStaggerTimerDebug:F2}, State={CurrentStateName}");

        if (_bossEnemy.IsInBirthStagger)
        {
            // 出生硬直期间：只允许死亡
            if (CheckTrigger("dead"))
            {
                Debug.Log("[BossFSM] Death triggered during stagger!");
                ChangeState(Dead);
            }
            return;
        }

        // 硬直结束：复用基类 Update
        base.Update();
    }

    protected override void OnSetup()
    {
        // 创建状态实例
        Idle = new EnemyIdleState();
        Chase = new EnemyChaseState();
        Dash = new BossDashState();
        Dead = new EnemyDeadState();

        AddState(Idle);
        AddState(Chase);
        AddState(Dash);
        AddState(Dead);

        AddTrigger("dead");
        AddBool("isDashing");

        EnemyBase enemy = Owner as EnemyBase;

        // Entry → Idle
        AddTransition(EntryState, Idle, () => true);

        // Idle → Chase
        AddTransition(Idle, Chase, () => enemy.DistanceToPlayer < enemy.DetectRange);

        // Chase → Dash
        AddTransition(Chase, Dash, () => enemy.DistanceToPlayer < enemy.AttackRange);

        // Chase → Idle
        AddTransition(Chase, Idle, () => enemy.DistanceToPlayer > enemy.LoseRange);

        // Any → Dead
        AddAnyTransition(Dead, () => CheckTrigger("dead"));
    }
}
