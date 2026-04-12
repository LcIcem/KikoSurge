using LcIcemFramework.FSM;
using UnityEngine;

/// <summary>
/// 闪避状态：定时器驱动，冲刺移动。
/// </summary>
public class PlayerDashState : StateBase
{
    private float _timer;

    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorTrigger("Dash");
        _timer = 0f;
    }

    public override void Exec()
    {
        _timer += Time.deltaTime;
        _fsm.SetFloat("dashTimer", _timer);

        var player = Owner<Player>();
        Vector2 dashDir = player.MoveDir;  // 沿移动方向冲刺
        player._rigidbody.MovePosition(
            player._rigidbody.position + dashDir * GameDataManager.Instance.PlayerData.DashSpeed * Time.fixedDeltaTime);
    }

    public override void Exit()
    {
        _fsm.SetFloat("dashTimer", 0f);
    }
}