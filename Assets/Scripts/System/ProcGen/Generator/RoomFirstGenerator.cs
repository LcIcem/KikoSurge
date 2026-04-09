using System.Collections.Generic;
using UnityEngine;
using ProcGen.Config;
using ProcGen.Core;
using ProcGen.Seed;
using Game.Util.Const;

namespace ProcGen.Generator
{
    /// <summary>Room-First + MST 走廊地牢生成器
    /// 流程：生成起点 → 生成特殊房间 → 生成普通房间 → MST连通 → 确定终点 → 生成走廊 → 构建图结构
    /// 支持地图自动扩容：当保证数量无法生成时，根据扩容因子触发地图扩展
    /// 所有随机操作均通过 GameRandom，确保同一种子产生相同地牢
    /// </summary>
    public class RoomFirstGenerator : IDungeonGenerator
    {
        // ==================== 私有字段 ====================
        private int _nextRoomId;
        private int _nextCorridorId;
        private List<Room> _rooms;
        private List<Corridor> _corridors;
        private RectInt _mapBounds;                   // 当前地图范围（可随扩容变化）
        private int _corridorWidth;
        private HashSet<Vector2Int> _roomFloorTiles;
        private DungeonModel_SO _config;
        private GameRandom _rng;                     // 种子驱动的随机数生成器
        private Room _startRoom;                     // 起点房间缓存（放置后不再改变，避免每次 O(n) 查找）

        // ==================== 扩容状态字段 ====================
        private int _baseMapWidth;                    // 地图原始宽度（来自 config）
        private int _baseMapHeight;                  // 地图原始高度（来自 config）
        private int _currentMaxAttempts;             // 当前最大尝试次数（可随扩容翻倍）
        private float _currentExpandFactor;           // 当前扩容因子（0-1，可随尝试逐渐增长）

        // ==================== 常量 ====================
        private const float EXPANSION_THRESHOLD = GameConstants.DUNGEON_EXPANSION_THRESHOLD;
        private const int INITIAL_MAX_ATTEMPTS = 30;

        // ==================== 接口实现 ====================

        public (DungeonGraph graph, DungeonTileData tileData) Generate(DungeonModel_SO config, GameRandom rng = null)
        {
            _config = config;
            _rng = rng ?? new GameRandom(System.Environment.TickCount);
            _nextRoomId = 0;
            _nextCorridorId = 0;
            _rooms = new List<Room>();
            _corridors = new List<Corridor>();
            _corridorWidth = config.corridorWidth;
            _roomFloorTiles = new HashSet<Vector2Int>();

            // 记录原始地图尺寸
            _baseMapWidth = config.mapWidth;
            _baseMapHeight = config.mapHeight;

            // 重置扩容状态
            ResetMapBounds();
            _currentMaxAttempts = INITIAL_MAX_ATTEMPTS;
            _currentExpandFactor = EXPANSION_THRESHOLD;

            // Step 1: 放置起点房间
            PlaceStartRoom();

            // Step 2: 生成特殊类型房间（保证数量 + 概率额外）
            GenerateSpecialRooms();

            // Step 3: 生成普通房间（保证数量 + 概率额外）
            GenerateNormalRooms();

            // Step 4: MST 连接所有房间
            BuildMST();

            // Step 5: 确定终点 + Elite 概率补充
            FinalizeRoomTypes();

            // Step 6: 生成走廊
            GenerateCorridors();

            // Step 7: 建立房间与走廊的关联
            PopulateCorridorIds();

            // Step 8: 构建返回结果
            var graph = BuildGraph();
            return (graph, new DungeonTileData(graph));
        }

        // ==================== Step 1: 放置起点房间 ====================

        private void PlaceStartRoom()
        {
            var startData = _config.GetRoomConfigData(RoomType.Start);
            int halfW = _mapBounds.width / 2;
            int halfH = _mapBounds.height / 2;
            int x = _rng.Range(_mapBounds.xMin, _mapBounds.xMin + halfW);
            int y = _rng.Range(_mapBounds.yMin, _mapBounds.yMin + halfH);
            var size = startData == null
                ? new Vector2Int(8, 8)
                : startData.GetRandomSize(_rng);

            var room = new Room
            {
                id = _nextRoomId++,
                gridPos = new Vector2Int(x, y),
                size = size,
                roomType = RoomType.Start
            };

            _rooms.Add(room);
            _startRoom = room; // 缓存起点引用，后续 CheckDistanceConstraint 直接使用，无需每次遍历查找
            AddRoomFloorTiles(room);
        }

        // ==================== Step 2: 生成特殊房间 ====================

        private void GenerateSpecialRooms()
        {
            TryPlaceWithHybrid(RoomType.Treasure, _config.treasureRoomCount, _config.treasureExtraChance,
                _config.GetTemplates(RoomType.Treasure));

            TryPlaceWithHybrid(RoomType.Shop, _config.shopRoomCount, _config.shopExtraChance,
                _config.GetTemplates(RoomType.Shop));

            TryPlaceWithHybrid(RoomType.Rest, _config.restRoomCount, _config.restExtraChance,
                _config.GetTemplates(RoomType.Rest));

            TryPlaceWithHybrid(RoomType.Event, _config.eventRoomCount, _config.eventExtraChance,
                _config.GetTemplates(RoomType.Event));

            TryPlaceWithHybrid(RoomType.Boss, _config.bossRoomCount, _config.bossExtraChance,
                _config.GetTemplates(RoomType.Boss));

            TryPlaceWithHybrid(RoomType.Elite, _config.eliteRoomCount, _config.eliteExtraChance,
                _config.GetTemplates(RoomType.Elite));
        }

        /// <summary>
        /// 以"保证数量 + 概率额外生成"的混合模式放置房间，支持自动扩容
        /// </summary>
        private void TryPlaceWithHybrid(RoomType type, int guaranteed, int extraChance, List<RoomConfigData> configs)
        {
            if (configs == null || configs.Count == 0)
                return;

            bool allGuaranteedPlaced = false;

            while (!allGuaranteedPlaced)
            {
                int placed = 0;

                for (int i = 0; i < guaranteed; i++)
                {
                    int attempts = 0;
                    while (attempts < _currentMaxAttempts)
                    {
                        if (TryPlaceSpecialRoom(type, configs))
                        {
                            placed++;
                            break;
                        }
                        attempts++;
                        _currentExpandFactor = (float)attempts / _currentMaxAttempts;
                        if (_currentExpandFactor > EXPANSION_THRESHOLD)
                        {
                            ExpandMap();
                            attempts = 0;
                        }
                    }
                }

                allGuaranteedPlaced = (placed == guaranteed);
                if (!allGuaranteedPlaced)
                    break;
            }

            if (extraChance > 0 && _rng.Range(0, 100) < extraChance)
                TryPlaceSpecialRoom(type, configs);
        }

        /// <summary>
        /// 尝试将一个特殊房间放置到地图中（遍历同类型所有配置，支持距离约束）
        /// </summary>
        private bool TryPlaceSpecialRoom(RoomType type, List<RoomConfigData> configs)
        {
            var configList = new List<RoomConfigData>(configs);
            _rng.Shuffle(configList);

            foreach (var data in configList)
            {
                if (data == null)
                    continue;
                if (data.minSize == default && data.maxSize == default)
                    continue;

                int attempts = 0;
                while (attempts < _currentMaxAttempts)
                {
                    Vector2Int size = data.GetRandomSize(_rng);
                    int x = _rng.Range(_mapBounds.xMin, _mapBounds.xMax - size.x);
                    int y = _rng.Range(_mapBounds.yMin, _mapBounds.yMax - size.y);

                    var candidate = new Room
                    {
                        id = _nextRoomId++,
                        gridPos = new Vector2Int(x, y),
                        size = size,
                        roomType = type
                    };

                    if (!CheckDistanceConstraint(candidate, data.minDistFromStart, data.maxDistFromStart))
                    {
                        attempts++;
                        continue;
                    }

                    if (!IsOverlapping(candidate))
                    {
                        _rooms.Add(candidate);
                        AddRoomFloorTiles(candidate);
                        return true;
                    }
                    attempts++;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查房间候选是否满足距离起点房间的约束
        /// </summary>
        /// <param name="candidate">待检查的房间候选</param>
        /// <param name="minDist">距起点的最小曼哈顿距离（0 = 不限制下界）</param>
        /// <param name="maxDist">距起点的最大曼哈顿距离（-1 = 不限制上界）</param>
        private bool CheckDistanceConstraint(Room candidate, int minDist, int maxDist)
        {
            // 生成阶段尚未放置任何房间时，无起点可比较
            if (_startRoom == null)
                return true;

            int dist = GetManhattanDistance(candidate.Center, _startRoom.Center);

            // minDist = 0：不设下界（dist >= 0 永远成立，等同于无约束）；否则房间中心必须在 minDist 格外
            bool minOk = minDist <= 0 || dist >= minDist;
            // maxDist = -1：不设上界；否则房间中心必须在 maxDist 格内（maxDist = 0 时必须在起点处）
            bool maxOk = maxDist < 0 || dist <= maxDist;

            return minOk && maxOk;
        }

        /// <summary>触发地图扩容：最大尝试次数翻倍，地图宽高扩大 1.5 倍，并重新缩放已有房间位置</summary>
        private void ExpandMap()
        {
            _currentMaxAttempts *= 2;

            int newWidth = Mathf.CeilToInt(_baseMapWidth * 1.5f);
            int newHeight = Mathf.CeilToInt(_baseMapHeight * 1.5f);
            int newBorderSize = _config.borderSize;

            var newBounds = new RectInt(
                -newWidth / 2,
                -newHeight / 2,
                newWidth - newBorderSize * 2,
                newHeight - newBorderSize * 2
            );

            float scaleX = (float)newBounds.width / _mapBounds.width;
            float scaleY = (float)newBounds.height / _mapBounds.height;

            _roomFloorTiles.Clear();

            foreach (var room in _rooms)
            {
                room.gridPos = new Vector2Int(
                    Mathf.RoundToInt(room.gridPos.x * scaleX),
                    Mathf.RoundToInt(room.gridPos.y * scaleY)
                );
                AddRoomFloorTiles(room);
            }

            _mapBounds = newBounds;
        }

        // ==================== Step 3: 生成普通房间 ====================

        private void GenerateNormalRooms()
        {
            var normalConfigs = _config.normalData;

            if (normalConfigs == null || normalConfigs.Count == 0)
                return;

            int placed = 0;
            while (placed < _config.normalRoomCount)
            {
                int attempts = 0;
                while (attempts < _currentMaxAttempts)
                {
                    if (TryPlaceNormalRoom(normalConfigs))
                    {
                        placed++;
                        if (placed >= _config.normalRoomCount)
                            break;
                    }
                    attempts++;
                    _currentExpandFactor = (float)attempts / _currentMaxAttempts;
                    if (_currentExpandFactor > EXPANSION_THRESHOLD)
                    {
                        ExpandMap();
                        attempts = 0;
                    }
                }

                if (placed >= _config.normalRoomCount)
                    break;

                if (attempts >= _currentMaxAttempts)
                    break;
            }

            if (_config.normalExtraChance > 0 && _rng.Range(0, 100) < _config.normalExtraChance)
                TryPlaceNormalRoom(normalConfigs);
        }

        /// <summary>尝试放置一个普通房间（支持距离约束）</summary>
        private bool TryPlaceNormalRoom(List<RoomConfigData> configs)
        {
            var configList = new List<RoomConfigData>(configs);
            _rng.Shuffle(configList);

            foreach (var data in configList)
            {
                if (data == null)
                    continue;
                if (data.minSize == default && data.maxSize == default)
                    continue;

                int attempts = 0;
                while (attempts < _currentMaxAttempts)
                {
                    Vector2Int size = data.GetRandomSize(_rng);
                    int x = _rng.Range(_mapBounds.xMin, _mapBounds.xMax - size.x);
                    int y = _rng.Range(_mapBounds.yMin, _mapBounds.yMax - size.y);

                    var candidate = new Room
                    {
                        id = _nextRoomId++,
                        gridPos = new Vector2Int(x, y),
                        size = size,
                        roomType = RoomType.Normal
                    };

                    if (!CheckDistanceConstraint(candidate, data.minDistFromStart, data.maxDistFromStart))
                    {
                        attempts++;
                        continue;
                    }

                    if (!IsOverlapping(candidate))
                    {
                        _rooms.Add(candidate);
                        AddRoomFloorTiles(candidate);
                        return true;
                    }
                    attempts++;
                }
            }
            return false;
        }

        // ==================== 工具方法 ====================

        /// <summary>重置地图范围为 config 中的原始尺寸</summary>
        private void ResetMapBounds()
        {
            int width = _baseMapWidth;
            int height = _baseMapHeight;
            int border = _config.borderSize;

            _mapBounds = new RectInt(
                -width / 2,
                -height / 2,
                width - border * 2,
                height - border * 2
            );
        }

        private bool IsOverlapping(Room room)
        {
            if (room.Bounds.xMin < _mapBounds.xMin ||
                room.Bounds.xMax > _mapBounds.xMax ||
                room.Bounds.yMin < _mapBounds.yMin ||
                room.Bounds.yMax > _mapBounds.yMax)
            {
                return true;
            }

            foreach (var existing in _rooms)
            {
                var expandedBounds = new RectInt(
                    existing.gridPos - Vector2Int.one,
                    existing.size + new Vector2Int(2, 2)
                );
                if (expandedBounds.Overlaps(room.Bounds))
                    return true;
            }

            return false;
        }

        private void AddRoomFloorTiles(Room room)
        {
            for (int x = room.gridPos.x; x < room.gridPos.x + room.size.x; x++)
            {
                for (int y = room.gridPos.y; y < room.gridPos.y + room.size.y; y++)
                {
                    _roomFloorTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        // ==================== Step 4: MST 连通 ====================

        private void BuildMST()
        {
            if (_rooms.Count < 2)
                return;

            var inMST = new HashSet<int>();
            var allIds = new List<int>();

            foreach (var r in _rooms)
                allIds.Add(r.id);

            int startId = _rooms[0].id;
            inMST.Add(startId);

            while (inMST.Count < _rooms.Count)
            {
                float minDist = float.MaxValue;
                (int, int) bestEdge = (-1, -1);

                foreach (int mstId in inMST)
                {
                    Room mstRoom = FindRoom(mstId);

                    foreach (int otherId in allIds)
                    {
                        if (inMST.Contains(otherId))
                            continue;

                        Room otherRoom = FindRoom(otherId);
                        float dist = GetManhattanDistance(mstRoom.Center, otherRoom.Center);

                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestEdge = (mstId, otherId);
                        }
                    }
                }

                if (bestEdge.Item1 != -1)
                {
                    inMST.Add(bestEdge.Item2);

                    Room a = FindRoom(bestEdge.Item1);
                    Room b = FindRoom(bestEdge.Item2);
                    a.connectedRoomIds.Add(b.id);
                    b.connectedRoomIds.Add(a.id);
                }
            }
        }

        // ==================== Step 5: 确定终点 + Elite 概率补充 ====================

        private void FinalizeRoomTypes()
        {
            if (_rooms.Count == 0)
                return;

            int startId = FindStartRoomId();
            Room goalRoom = FindFarthestRoom(startId);
            goalRoom.roomType = RoomType.Goal;

            if (_config.eliteRoomCount == 0 && _config.eliteExtraChance > 0)
            {
                foreach (var room in _rooms)
                {
                    if (room.roomType == RoomType.Normal &&
                        _rng.Range(0, 100) < _config.eliteExtraChance)
                    {
                        room.roomType = RoomType.Elite;
                    }
                }
            }
        }

        private int FindStartRoomId()
        {
            foreach (var room in _rooms)
            {
                if (room.roomType == RoomType.Start)
                    return room.id;
            }
            return _rooms[0].id;
        }

        private Room FindFarthestRoom(int startId)
        {
            var distances = new Dictionary<int, int>();
            var queue = new Queue<int>();
            var visited = new HashSet<int>();

            queue.Enqueue(startId);
            visited.Add(startId);
            distances[startId] = 0;

            int farthestId = startId;
            int maxDist = 0;

            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                int currentDist = distances[currentId];

                if (currentDist > maxDist)
                {
                    maxDist = currentDist;
                    farthestId = currentId;
                }

                Room current = FindRoom(currentId);
                foreach (int neighborId in current.connectedRoomIds)
                {
                    if (!visited.Contains(neighborId))
                    {
                        visited.Add(neighborId);
                        distances[neighborId] = currentDist + 1;
                        queue.Enqueue(neighborId);
                    }
                }
            }

            return FindRoom(farthestId);
        }

        // ==================== Step 6: 生成走廊 ====================

        private void GenerateCorridors()
        {
            var generatedPairs = new HashSet<string>();

            foreach (var room in _rooms)
            {
                foreach (int neighborId in room.connectedRoomIds)
                {
                    string pair = MakePairKey(room.id, neighborId);
                    if (generatedPairs.Contains(pair))
                        continue;
                    generatedPairs.Add(pair);

                    GenerateCorridorBetween(room, FindRoom(neighborId));
                }
            }
        }

        private void GenerateCorridorBetween(Room a, Room b)
        {
            // 从房间边界点出发，而非中心，避免走廊覆盖房间内部
            Vector2Int edgeA = GetClosestEdgePoint(a, b.Center);
            Vector2Int edgeB = GetClosestEdgePoint(b, a.Center);

            var path = new List<Vector2Int>();
            int x = edgeA.x;
            while (x != edgeB.x)
            {
                AddCorridorTileIfFree(path, new Vector2Int(x, edgeA.y));
                x += (edgeB.x > x) ? 1 : -1;
            }

            int y = edgeA.y;
            while (y != edgeB.y)
            {
                AddCorridorTileIfFree(path, new Vector2Int(edgeB.x, y));
                y += (edgeB.y > y) ? 1 : -1;
            }

            RectInt bounds = CalculateBounds(path);
            _corridors.Add(new Corridor(a.id, b.id) { id = _nextCorridorId++, pathTiles = path, bounds = bounds });
        }

        /// <summary>获取 room 边界上距离 target 最近的一点（用于走廊起点/终点）</summary>
        private Vector2Int GetClosestEdgePoint(Room room, Vector2Int target)
        {
            int x = Mathf.Clamp(target.x, room.Bounds.xMin, room.Bounds.xMax - 1);
            int y = Mathf.Clamp(target.y, room.Bounds.yMin, room.Bounds.yMax - 1);
            return new Vector2Int(x, y);
        }

        /// <summary>只将不在房间已有区域的格子加入走廊</summary>
        private void AddCorridorTileIfFree(List<Vector2Int> path, Vector2Int center)
        {
            int half = _corridorWidth / 2;
            for (int dx = -half; dx <= half; dx++)
            {
                for (int dy = -half; dy <= half; dy++)
                {
                    var tile = new Vector2Int(center.x + dx, center.y + dy);
                    if (!_roomFloorTiles.Contains(tile))
                        path.Add(tile);
                }
            }
        }

        /// <summary>根据走廊格子列表计算包围盒</summary>
        private RectInt CalculateBounds(List<Vector2Int> tiles)
        {
            if (tiles.Count == 0)
                return new RectInt(0, 0, 0, 0);

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var t in tiles)
            {
                if (t.x < minX) minX = t.x;
                if (t.y < minY) minY = t.y;
                if (t.x > maxX) maxX = t.x;
                if (t.y > maxY) maxY = t.y;
            }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // ==================== Step 7: 构建返回结果 ====================

        private DungeonGraph BuildGraph()
        {
            var graph = new DungeonGraph();

            foreach (var room in _rooms)
            {
                graph.allRooms.Add(room);

                if (room.roomType == RoomType.Start)
                    graph.startRoomId = room.id;
                else if (room.roomType == RoomType.Goal)
                    graph.goalRoomId = room.id;
            }

            foreach (var corridor in _corridors)
                graph.corridors.Add(corridor);

            graph.BuildIndexes();
            return graph;
        }

        /// <summary>建立房间与走廊的关联（按 corridor.roomAId/roomBId 填各房间的 corridorIds）</summary>
        private void PopulateCorridorIds()
        {
            foreach (var corridor in _corridors)
            {
                FindRoom(corridor.roomAId).corridorIds.Add(corridor.id);
                FindRoom(corridor.roomBId).corridorIds.Add(corridor.id);
            }
        }

        // ==================== 工具方法 ====================

        private Room FindRoom(int id) => _rooms.Find(r => r.id == id);

        private int GetManhattanDistance(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private string MakePairKey(int a, int b) =>
            (a < b) ? $"{a}_{b}" : $"{b}_{a}";
    }
}
