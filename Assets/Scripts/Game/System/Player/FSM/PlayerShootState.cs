using LcIcemFramework.Core;
using LcIcemFramework.FSM;
using UnityEngine;
using UnityEngine.UIElements;

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
        playerFSM.SetAnimatorTrigger("shoot");

        // 使用当前武器进行射击
        player.weaponHandler.Fire(player.AimDir);

        // 摄像机抖动
        EventCenter.Instance.Publish(EventID.ShootPerformed);
    }

    public override void Exec() { }

    public override void Exit() { }
}