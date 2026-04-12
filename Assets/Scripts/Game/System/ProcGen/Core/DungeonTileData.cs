using System.Collections.Generic;
using UnityEngine;

namespace ProcGen.Core
{
    /// <summary>
    /// 地牢瓦片数据预计算层。
    /// 构造时一次性计算所有数据，生成后完全只读。
    /// 为外部游戏代码（玩家控制器、门系统、碰撞等）提供 O(1) 查询 API。
    ///
    /// 墙壁归属规则：
    /// - 房间墙壁 = 房间地面 8方向膨胀 - 连通走廊地面
    /// - 走廊墙壁 = 走廊地面 8方向膨胀 - 连通房间地面 8方向膨胀
    /// - 两者互斥，无交集
    ///
    /// 门定义：
    /// - 房间的门 = 房间地面 8方向膨胀 ∩ 连通走廊地面
    /// </summary>
    public class DungeonTileData
    {
        // ==================== 图引用（只读） ====================
        private readonly DungeonGraph _graph;

        // ==================== 预计算：每实体瓦片字典 ====================
        private readonly Dictionary<int, HashSet<Vector2Int>> _roomFloorTiles;
        private readonly Dictionary<int, HashSet<Vector2Int>> _roomWallTiles;
        private readonly Dictionary<int, HashSet<Vector2Int>> _roomDoorTiles;
        private readonly Dictionary<int, HashSet<Vector2Int>> _corridorFloorTiles;
        private readonly Dictionary<int, HashSet<Vector2Int>> _corridorWallTiles;

        // ==================== 预计算：8方向膨胀缓存（复用，减少重复遍历） ====================
        private readonly Dictionary<int, HashSet<Vector2Int>> _roomFloorNeighbors8;
        private readonly Dictionary<int, HashSet<Vector2Int>> _corridorFloorNeighbors8;

        // ==================== 聚合数据 ====================
        public HashSet<Vector2Int> allFloorTiles { get; }
        public HashSet<Vector2Int> allWallTiles { get; }
        public HashSet<Vector2Int> allDoorTiles { get; }
        public Dictionary<RoomType, HashSet<Vector2Int>> floorTilesByRoomType { get; }
        public Dictionary<RoomType, HashSet<Vector2Int>> wallTilesByRoomType { get; }

        /// <summary>生成时实际使用的地图范围（可能因 ExpandMap 扩容而大于 config 原始值）</summary>
        public RectInt mapBounds => _graph.mapBounds;


        // ==================== 空间哈希 ====================
        // >= 0: 坐标落在对应 room ID 的 Bounds 内
        // -1: 坐标落在走廊地面
        // missing: 空地
        private readonly Dictionary<Vector2Int, int> _posToRoomId;
        private readonly Dictionary<Vector2Int, int> _posToCorridorId;

        // ==================== 常量 ====================
        private static readonly Vector2Int[] _dirs8 = new Vector2Int[]
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1),
            new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        // ==================== 构造 ====================

        public DungeonTileData(DungeonGraph graph)
        {
            _graph = graph;
            _roomFloorTiles = new Dictionary<int, HashSet<Vector2Int>>();
            _roomWallTiles = new Dictionary<int, HashSet<Vector2Int>>();
            _roomDoorTiles = new Dictionary<int, HashSet<Vector2Int>>();
            _corridorFloorTiles = new Dictionary<int, HashSet<Vector2Int>>();
            _corridorWallTiles = new Dictionary<int, HashSet<Vector2Int>>();
            _roomFloorNeighbors8 = new Dictionary<int, HashSet<Vector2Int>>();
            _corridorFloorNeighbors8 = new Dictionary<int, HashSet<Vector2Int>>();
            _posToRoomId = new Dictionary<Vector2Int, int>();
            _posToCorridorId = new Dictionary<Vector2Int, int>();
            allFloorTiles = new HashSet<Vector2Int>();
            allWallTiles = new HashSet<Vector2Int>();
            allDoorTiles = new HashSet<Vector2Int>();
            floorTilesByRoomType = new Dictionary<RoomType, HashSet<Vector2Int>>();
            wallTilesByRoomType = new Dictionary<RoomType, HashSet<Vector2Int>>();

            ComputeAll();
        }

        private void ComputeAll()
        {
            // ---- Step 1: 房间地面 + 8方向邻居 ----
            foreach (var room in _graph.allRooms)
            {
                var floors = ComputeRoomFloor(room);
                _roomFloorTiles[room.id] = floors;
                _roomFloorNeighbors8[room.id] = Expand8(floors);

                foreach (var tile in floors)
                    _posToRoomId[tile] = room.id;

                allFloorTiles.UnionWith(floors);
                if (!floorTilesByRoomType.TryGetValue(room.roomType, out var ft))
                    floorTilesByRoomType[room.roomType] = ft = new HashSet<Vector2Int>();
                ft.UnionWith(floors);
            }

            // ---- Step 2: 走廊地面 + 8方向邻居 ----
            foreach (var corridor in _graph.corridors)
            {
                var floors = new HashSet<Vector2Int>(corridor.pathTiles);
                _corridorFloorTiles[corridor.id] = floors;
                _corridorFloorNeighbors8[corridor.id] = Expand8(floors);

                foreach (var tile in floors)
                    if (!_posToRoomId.ContainsKey(tile))
                        _posToCorridorId[tile] = corridor.id;

                allFloorTiles.UnionWith(floors);
            }

            // ---- Step 3: 房间墙壁和门 ----
            foreach (var room in _graph.allRooms)
            {
                var neighbors8 = _roomFloorNeighbors8[room.id];

                // 合并连通走廊的地面
                var connectedCorridorFloors = new HashSet<Vector2Int>();
                foreach (var cid in room.corridorIds)
                {
                    if (_corridorFloorTiles.TryGetValue(cid, out var cf))
                        connectedCorridorFloors.UnionWith(cf);
                }

                // 房间墙壁 = 房间地面8膨胀 - 连通走廊地面
                var walls = new HashSet<Vector2Int>(neighbors8);
                walls.ExceptWith(connectedCorridorFloors);
                _roomWallTiles[room.id] = walls;

                // 房间门 = 房间地面8膨胀 ∩ 连通走廊地面
                var doors = new HashSet<Vector2Int>(neighbors8);
                doors.IntersectWith(connectedCorridorFloors);
                _roomDoorTiles[room.id] = doors;
                allDoorTiles.UnionWith(doors);

                allWallTiles.UnionWith(walls);
                if (!wallTilesByRoomType.TryGetValue(room.roomType, out var wt))
                    wallTilesByRoomType[room.roomType] = wt = new HashSet<Vector2Int>();
                wt.UnionWith(walls);
            }

            // ---- Step 4: 走廊墙壁 ----
            foreach (var corridor in _graph.corridors)
            {
                var neighbors8 = _corridorFloorNeighbors8[corridor.id];

                // 合并连通房间的8方向膨胀
                var connectedRoomNeighbors = new HashSet<Vector2Int>();
                foreach (var rid in new[] { corridor.roomAId, corridor.roomBId })
                {
                    if (_roomFloorNeighbors8.TryGetValue(rid, out var rn))
                        connectedRoomNeighbors.UnionWith(rn);
                }

                // 走廊墙壁 = 走廊地面8膨胀 - 连通房间的8膨胀
                var walls = new HashSet<Vector2Int>(neighbors8);
                walls.ExceptWith(connectedRoomNeighbors);

                // 排除落入任何房间地面的格子（O(1) 空间哈希查找，无需遍历）
                walls.RemoveWhere(wall => _posToRoomId.ContainsKey(wall));

                _corridorWallTiles[corridor.id] = walls;
                allWallTiles.UnionWith(walls);
            }
        }

        // ==================== 私有工具 ====================

        private HashSet<Vector2Int> ComputeRoomFloor(Room room)
        {
            var tiles = new HashSet<Vector2Int>();
            for (int x = room.gridPos.x; x < room.gridPos.x + room.size.x; x++)
            for (int y = room.gridPos.y; y < room.gridPos.y + room.size.y; y++)
                tiles.Add(new Vector2Int(x, y));
            return tiles;
        }

        private HashSet<Vector2Int> Expand8(HashSet<Vector2Int> tiles)
        {
            var expanded = new HashSet<Vector2Int>();
            foreach (var t in tiles)
            foreach (var d in _dirs8)
                expanded.Add(t + d);
            // 膨胀结果只保留真正的邻居，不含自身
            expanded.ExceptWith(tiles);
            return expanded;
        }

        // ==================== 外部 API ====================

        /// <summary>O(1) 坐标查房间 ID（miss 返回 -1）</summary>
        public int GetRoomIdAt(Vector2Int pos) =>
            _posToRoomId.GetValueOrDefault(pos, -1);

        /// <summary>O(1) 坐标查走廊 ID（miss 返回 -1）</summary>
        public int GetCorridorIdAt(Vector2Int pos) =>
            _posToCorridorId.GetValueOrDefault(pos, -1);

        /// <summary>O(1) 按 ID 获取房间引用</summary>
        public Room GetRoom(int id) => _graph.GetRoom(id);

        /// <summary>OO(1) 按 ID 获取走廊引用</summary>
        public Corridor GetCorridor(int id) => _graph.GetCorridor(id);

        /// <summary>O(1) 房间地面瓦片</summary>
        public bool TryGetRoomFloorTiles(int roomId, out HashSet<Vector2Int> tiles) =>
            _roomFloorTiles.TryGetValue(roomId, out tiles);

        /// <summary>O(1) 房间墙壁瓦片</summary>
        public bool TryGetRoomWallTiles(int roomId, out HashSet<Vector2Int> tiles) =>
            _roomWallTiles.TryGetValue(roomId, out tiles);

        /// <summary>O(1) 房间门瓦片</summary>
        public bool TryGetRoomDoorTiles(int roomId, out HashSet<Vector2Int> tiles) =>
            _roomDoorTiles.TryGetValue(roomId, out tiles);

        /// <summary>O(1) 走廊地面瓦片</summary>
        public bool TryGetCorridorFloorTiles(int corridorId, out HashSet<Vector2Int> tiles) =>
            _corridorFloorTiles.TryGetValue(corridorId, out tiles);

        /// <summary>O(1) 走廊墙壁瓦片</summary>
        public bool TryGetCorridorWallTiles(int corridorId, out HashSet<Vector2Int> tiles) =>
            _corridorWallTiles.TryGetValue(corridorId, out tiles);
    }
}
