using System;

/// <summary>
/// 房间存档状态（用于存档持久化，区别于运行时 RoomState 枚举）
/// </summary>
[Serializable]
public class RoomSaveState
{
    /// <summary>
    /// 房间唯一标识
    /// </summary>
    public int roomId;

    /// <summary>
    /// 房间类型
    /// </summary>
    public string roomType;

    /// <summary>
    /// 是否已清理（敌人全清）
    /// </summary>
    public bool isCleared;

    public RoomSaveState()
    {
    }

    public RoomSaveState(int roomId, string roomType, bool isCleared)
    {
        this.roomId = roomId;
        this.roomType = roomType;
        this.isCleared = isCleared;
    }
}
