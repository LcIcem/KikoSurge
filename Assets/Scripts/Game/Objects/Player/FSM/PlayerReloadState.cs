using LcIcemFramework.Core;
using LcIcemFramework.FSM;
using LcIcemFramework;
using UnityEngine;

/// <summary>
/// 装填状态
/// </summary>
public class PlayerReloadState : StateBase
{
    public override void Enter()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isReload", true);
    }

    public override void Exec()
    {
        
    }

    public override void Exit()
    {
        PlayerFSM playerFSM = _fsm as PlayerFSM;
        playerFSM.SetAnimatorBool("isReload", false);
    }
}