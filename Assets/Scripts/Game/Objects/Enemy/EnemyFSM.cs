using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 敌人状态机（定义层）。
/// 持有 Animator 引用，通过 SetAnimatorBool/SetAnimatorTrigger 驱动动画。
/// </summary>
public class EnemyFSM : FSM
{
    public EnemyIdleState Idle { get; private set; }
    public EnemyChaseState Chase { get; private set; }
    public EnemyAttackState Attack { get; private set; }
    public EnemyDeadState Dead { get; private set; }

    /// 持有者 Animator（驱动动画）
    private readonly Animator _animator;

    public EnemyFSM(EnemyBase enemy, Animator animator) : base(enemy)
    {
        _animator = animator;
    }

    protected override void OnSetup()
    {
        Idle = new EnemyIdleState();
        Chase = new EnemyChaseState();
        Attack = new EnemyAttackState();
        Dead = new EnemyDeadState();

        AddState(Idle);
        AddState(Chase);
        AddState(Attack);
        AddState(Dead);

        // 配置转换条件
        EnemyBase enemy = Owner as EnemyBase;

        // Entry → Idle
        AddTransition(EntryState, Idle, () => true);

        // Idle → Chase
        AddTransition(Idle, Chase, () => enemy.DistanceToPlayer < enemy.DetectRange);

        // // Chase → Attack
        // AddTransition(Chase, Attack, () =>
        //     enemy.DistanceToPlayer < enemy.AttackRange);

        // // Chase → Idle（脱离追踪范围）
        // AddTransition(Chase, Idle, () =>
        //     enemy.DistanceToPlayer > enemy.LoseRange);

        // // Attack → Chase（脱离攻击范围）
        // AddTransition(Attack, Chase, () =>
        //     enemy.DistanceToPlayer >= enemy.AttackRange);

        // // Any → Dead（任意状态可死亡）
        // AddAnyTransition(Dead, () => CheckTrigger("Dead"));
    }

    // 驱动 Animator Bool 参数
    public void SetAnimatorBool(string param, bool value)
    {
        if (_animator != null)
            _animator.SetBool(param, value);
    }

    // 驱动 Animator Trigger 参数
    public void SetAnimatorTrigger(string param)
    {
        if (_animator != null)
            _animator.SetTrigger(param);
    }
}