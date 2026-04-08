using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGen.Core
{
    /// <summary>走廊数据类（纯数据，无MonoBehaviour依赖）</summary>
    public class Corridor
    {
        public int roomAId; // 走廊一端连接的房间ID
        public int roomBId; // 走廊另一端连接的房间ID
        public List<Vector2Int> pathTiles;  // 走廊经过的所有网格坐标（只包含空地格，不含房间格）
        public RectInt bounds;  // 走廊包围盒（左下角坐标 + 宽高）

        public Corridor()
        {
            pathTiles = new List<Vector2Int>();
            bounds = new RectInt(0, 0, 0, 0);
        }

        public Corridor(int aId, int bId) : this()
        {
            roomAId = aId;
            roomBId = bId;
        }
    }
}
