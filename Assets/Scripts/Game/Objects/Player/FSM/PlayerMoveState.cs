using LcIcemFramework.FSM;
using LcIcemFramework;
using UnityEngine;

/// <summary>
/// 移动状态：设置 isMoving=true，持续更新移动向量。
/// 动画由 AnimatorController 根据 isMoving=true 自动播放行走动画。
/// </summary>
public class PlayerMoveState : StateBase
{
    private const float WALK_SFX_INTERVAL = 0.44f;  // 脚步声间隔（秒）
    private float _walkSFXTimer;

    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isMoving", true);

        _walkSFXTimer = 0f;
    }

    public override void Exec()
    {
        _walkSFXTimer += Time.deltaTime;
        if (_walkSFXTimer >= WALK_SFX_INTERVAL)
        {
            _walkSFXTimer = 0f;
            var player = Owner<Player>();
            player.PlaySFX(player.WalkSFX);
        }
    }

    public override void Exit()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isMoving", false);
    }
}