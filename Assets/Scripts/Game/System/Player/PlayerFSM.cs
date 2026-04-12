using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 玩家状态机（定义层）。
/// 持有 Animator 引用，通过 SetAnimatorBool/SetAnimatorTrigger/SetAnimatorFloat 驱动动画。
/// </summary>
public class PlayerFSM : FSM
{
    public PlayerIdleState Idle { get; private set; }
    public PlayerMoveState Move { get; private set; }
    public PlayerShootState Shoot { get; private set; }
    public PlayerDashState Dash { get; private set; }
    public PlayerHurtState Hurt { get; private set; }
    public PlayerDeadState Dead { get; private set; }

    /// <summary>持有者 Animator（驱动动画）</summary>
    private readonly Animator _animator;

    public PlayerFSM(Player player, Animator animator) : base(player)
    {
        _animator = animator;
    }

    protected override void OnSetup()
    {
        Idle = new PlayerIdleState();
        Move = new PlayerMoveState();
        Shoot = new PlayerShootState();
        Dash = new PlayerDashState();
        Hurt = new PlayerHurtState();
        Dead = new PlayerDeadState();

        AddState(Idle);
        AddState(Move);
        AddState(Shoot);
        AddState(Dash);
        AddState(Hurt);
        AddState(Dead);

        // 声明 Animator 参数 
        AddBool("isMoving");
        AddBool("isIdle");
        AddBool("isShooting");
        AddFloat("dashTimer");
        AddFloat("hurtTimer");
        AddTrigger("shoot");
        AddTrigger("dash");
        AddTrigger("hurt");
        AddTrigger("dead");

        // 配置转换条件 

        // Idle ↔ Move
        AddTransition(Idle, Move, () => GetBool("isMoving"));
        AddTransition(Move, Idle, () => !GetBool("isMoving"));

        // Move → Shoot（射击是瞬时状态，Trigger 触发后立即切回）
        AddTransition(Move, Shoot, () => CheckTrigger("shoot"));
        AddTransition(Shoot, Move, () => !GetBool("isShooting"));

        // Move → Dash
        AddTransition(Move, Dash, () => CheckTrigger("dash"));
        AddTransition(Dash, Move, () => GetFloat("dashTimer") >= GameDataManager.Instance.PlayerData.dashDuration);

        // Any → Hurt（任意状态可受击）
        AddAnyTransition(Hurt, () => CheckTrigger("hurt"));

        // Hurt → Idle（无敌帧结束）
        AddTransition(Hurt, Idle, () =>
            GetFloat("hurtTimer") >= GameDataManager.Instance.PlayerData.invincibleDuration);

        // Any → Dead（任意状态可死亡）
        AddAnyTransition(Dead, () => CheckTrigger("dead"));
    }

    /// 驱动 Animator Bool 参数
    public void SetAnimatorBool(string param, bool value)
    {
        if (_animator != null)
            _animator.SetBool(param, value);
    }

    /// 驱动 Animator Trigger 参数
    public void SetAnimatorTrigger(string param)
    {
        if (_animator != null)
            _animator.SetTrigger(param);
    }

    /// 驱动 Animator Float 参数
    public void SetAnimatorFloat(string param, float value)
    {
        if (_animator != null)
            _animator.SetFloat(param, value);
    }
}