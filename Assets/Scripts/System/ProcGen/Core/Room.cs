using System.Collections.Generic;
using UnityEngine;

namespace ProcGen.Core
{
    /// <summary>地牢房间数据类（纯数据，无MonoBehaviour依赖）</summary>
    public class Room
    {
        public int id;  // 房间唯一标识
        public Vector2Int gridPos;          // 房间左下角网格坐标
        public Vector2Int size;             // 房间宽高（格）
        public RoomType roomType;           // 房间类型
        public List<int> connectedRoomIds;  // 直接相连的房间ID列表（图拓扑，用于导航）
        public List<int> corridorIds;        // 连通本房间的走廊 ID 列表

        // bounds 根据 gridPos 和 size 自动计算，便于碰撞检测
        public RectInt Bounds => new RectInt(gridPos, size);

        /// <summary>房间中心点网格坐标</summary>
        public Vector2Int Center => new Vector2Int(
            gridPos.x + size.x / 2,
            gridPos.y + size.y / 2
        );

        /// <summary>房间面积（格）</summary>
        public int Area => size.x * size.y;

        public Room()
        {
            connectedRoomIds = new List<int>();
            corridorIds = new List<int>();
        }

        /// <summary>检查是否与另一房间边界重叠</summary>
        public bool Intersects(Room other) => Bounds.Overlaps(other.Bounds);

        /// <summary>检查此房间是否包含指定网格坐标</summary>
        public bool ContainsGridPos(Vector2Int pos) => Bounds.Contains(pos);
    }
}
