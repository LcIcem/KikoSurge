using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 受击状态：无敌帧计时器。
/// </summary>
public class PlayerHurtState : StateBase
{
    private float _timer;

    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorTrigger("Hurt");
        _timer = 0f;
    }

    public override void Exec()
    {
        _timer += Time.deltaTime;
        _fsm.SetFloat("hurtTimer", _timer);
    }

    public override void Exit()
    {
        _fsm.SetFloat("hurtTimer", 0f);
    }
}
