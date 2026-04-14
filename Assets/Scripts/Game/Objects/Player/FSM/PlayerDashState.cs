using System.Collections.Generic;
using LcIcemFramework.FSM;
using LcIcemFramework.Managers;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 闪避状态：定时器驱动，冲刺移动。
/// </summary>
public class PlayerDashState : StateBase
{
    private float _timer;   // 冲刺持续时间计时器
    private Vector2 _dir;   // 进入冲刺状态时的方向
    private const float FACTOR = 0.4f;  // 玩家在冲刺过程中 输入方向的影响系数

    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        var player = Owner<Player>();

        playerFSM.SetAnimatorTrigger("dash");
        _timer = 0f;
        _dir = player.MoveDir;  // 记录进入冲刺状态时的方向

        // 残影特效
        // ShadowController shadow = ManagerHub.Pool.Get<ShadowController>("Shadow", Vector3.one, Quaternion.identity);
        // shadow.Init(player.GetComponent<SpriteRenderer>());
    }

    public override void Exec()
    {
        _timer += Time.deltaTime;
        _fsm.SetFloat("dashTimer", _timer);

        var player = Owner<Player>();
        // 根据 当前输入方向 与 进入冲刺时的方向 计算出总方向；
        var totalDir = (player.MoveDir * FACTOR + _dir).normalized;
        // 根据总方向、冲刺速度来决定冲刺位置
        player._rigidbody.MovePosition(
            player._rigidbody.position + totalDir * GameDataManager.Instance.PlayerData.DashSpeed * Time.fixedDeltaTime);
    }

    public override void Exit()
    {
        // 冲刺状态结束时 重置冲刺计时器和冲刺cd计时器
        _fsm.SetFloat("dashTimer", 0f);
        _fsm.SetFloat("dashGapTimer", GameDataManager.Instance.PlayerData.dashGap);
    }
}