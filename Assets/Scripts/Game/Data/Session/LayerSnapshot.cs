using System;
using System.Collections.Generic;

/// <summary>
/// 地牢层快照（用于存档恢复）
/// </summary>
[Serializable]
public class LayerSnapshot
{
    /// <summary>
    /// 层索引
    /// </summary>
    public int floorIndex;

    /// <summary>
    /// 该层的随机种子（用于恢复该层的随机状态）
    /// </summary>
    public long seed;

    /// <summary>
    /// 该层所有房间的状态快照
    /// </summary>
    public List<RoomSaveState> roomStates;

    /// <summary>
    /// 当前房间ID
    /// </summary>
    public int currentRoomId;

    /// <summary>
    /// 玩家在世界中的位置（进入该层时的位置）
    /// </summary>
    public float playerWorldPosX;
    public float playerWorldPosY;

    /// <summary>
    /// 当前生命值（用于检查点恢复）
    /// </summary>
    public float currentHealth;

    /// <summary>
    /// 起点房间ID
    /// </summary>
    public int startRoomId;

    /// <summary>
    /// 终点房间ID
    /// </summary>
    public int bossRoomId;

    public LayerSnapshot()
    {
        roomStates = new List<RoomSaveState>();
    }

    public LayerSnapshot(int floorIndex, long seed)
    {
        this.floorIndex = floorIndex;
        this.seed = seed;
        this.roomStates = new List<RoomSaveState>();
        this.currentRoomId = -1;
        this.startRoomId = -1;
        this.bossRoomId = -1;
    }

    /// <summary>
    /// 获取房间状态
    /// </summary>
    public RoomSaveState GetRoomState(int roomId)
    {
        return roomStates.Find(r => r.roomId == roomId);
    }

    /// <summary>
    /// 添加或更新房间状态
    /// </summary>
    public void SetRoomState(RoomSaveState state)
    {
        int index = roomStates.FindIndex(r => r.roomId == state.roomId);
        if (index >= 0)
        {
            roomStates[index] = state;
        }
        else
        {
            roomStates.Add(state);
        }
    }

    /// <summary>
    /// 获取玩家位置向量
    /// </summary>
    public UnityEngine.Vector2 GetPlayerWorldPos()
    {
        return new UnityEngine.Vector2(playerWorldPosX, playerWorldPosY);
    }

    /// <summary>
    /// 设置玩家位置
    /// </summary>
    public void SetPlayerWorldPos(UnityEngine.Vector2 pos)
    {
        playerWorldPosX = pos.x;
        playerWorldPosY = pos.y;
    }
}
