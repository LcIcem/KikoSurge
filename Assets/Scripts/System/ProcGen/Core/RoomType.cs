namespace ProcGen.Core
{
    /// <summary>地牢房间类型枚举</summary>
    public enum RoomType
    {
        Start,    // 起始房间（玩家出生点）
        Normal,   // 普通房间（战斗/资源探索）
        Goal,     // 终点房间（通往下一层）
        Treasure, // 宝藏间（高奖励）
        Shop,     // 商店
        Elite,    // 精英房（中等挑战）
        Rest,     // 休息室（恢复点）
        Event,    // 特殊事件房
        Boss      // Boss房
    }
}
