using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 射击状态（瞬时）：触发射击动画后立即切回 Move。
/// 动画由 AnimatorController 根据 Fire trigger 自动播放射击动画。
/// </summary>
public class PlayerShootState : StateBase
{
    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        var player = Owner<Player>();
        // 驱动射击动画
        playerFSM.SetAnimatorTrigger("Fire");
        // 触发武器开火
        player._weaponHandler.Fire(player.AimDir);
        _fsm.SetBool("isShooting", true);
    }

    public override void Exec()
    {
        // 射击是瞬时行为，Exec 中无额外逻辑
    }

    public override void Exit()
    {
        _fsm.SetBool("isShooting", false);
    }
}