using LcIcemFramework.Core;
using LcIcemFramework.FSM;
using LcIcemFramework.Managers.Mono;
using UnityEngine;

/// <summary>
/// 装填状态
/// </summary>
public class PlayerReloadState : StateBase
{
    public override void Enter()
    {
        Debug.Log("开始装填");
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isReload", true);
    }

    public override void Exec()
    {
        
    }

    public override void Exit()
    {
        Debug.Log("结束装填");
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isReload", false);
    }
}