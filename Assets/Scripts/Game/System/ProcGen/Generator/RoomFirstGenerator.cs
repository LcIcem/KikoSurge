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

        /// <summary>已生成的走廊 key 集合（用于防止 MST 和额外走廊重复生成同一对房间的走廊）</summary>
        private HashSet<string> _generatedCorridorPairs;

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
            _generatedCorridorPairs = new HashSet<string>();
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

            // Step 4.5: 额外走廊（在 MST 基础上随机添加回路，丰富地图连通性）
            GenerateExtraCorridors();

            // Step 5: 确定终点 + Elite 概率补充
            FinalizeRoomTypes();

            // Step 6: 生成走廊
            GenerateCorridors();

            // Step 7: 建立房间与走廊的关联
            PopulateCorridorIds();

            // Step 8: 构建返回结果
            var graph = BuildGraph();
            graph.mapBounds = _mapBounds; // 传递实际使用的地图范围（可能因扩容而扩大）
            return (graph, new DungeonTileData(graph));
        }

        // ==================== Step 1: 放置起点房间 ====================

        private void PlaceStartRoom()
        {
            var startData = _config.GetRoomConfigData(RoomType.Start);
            int border = _config.borderSize;
            int halfW = _mapBounds.width / 2;
            int halfH = _mapBounds.height / 2;
            // 起点放在左上 quadrant，左下角约束到 [xMin+border, xMin+halfW) 和 [yMin+halfH, yMax-border)
            // 房间右/上边缘也需在 border 内：xMax = xMin+halfW-border-size.x+1, yMax = yMax-border-size.y+1
            Vector2Int size = startData == null
                ? new Vector2Int(8, 8)
                : startData.GetRandomSize(_rng);
            // 起点房间放在左下 quadrant
            int x = _rng.Range(_mapBounds.xMin + border, _mapBounds.xMin + halfW - border - size.x + 1);
            int y = _rng.Range(_mapBounds.yMin + border, _mapBounds.yMax - halfH + border - size.y + 1);

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

            // Boss房间在 PlaceGoalRoom() 中统一生成

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

            int border = _config.borderSize;
            // border 约束：房间的左/右/下/上四个边缘都必须落在 border 区域内
            int xMin = _mapBounds.xMin + border;
            int xMax = _mapBounds.xMax - border;
            int yMin = _mapBounds.yMin + border;
            int yMax = _mapBounds.yMax - border;

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
                    // gridPos + size - 1 为房间右/上边缘，需 ≤ xMax-1/yMax-1
                    int x = _rng.Range(xMin, xMax - size.x + 1);
                    int y = _rng.Range(yMin, yMax - size.y + 1);

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

            // 地图范围扩展为新的完整尺寸
            var newBounds = new RectInt(
                -newWidth / 2,
                -newHeight / 2,
                newWidth,
                newHeight
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

            int border = _config.borderSize;
            int xMin = _mapBounds.xMin + border;
            int xMax = _mapBounds.xMax - border;
            int yMin = _mapBounds.yMin + border;
            int yMax = _mapBounds.yMax - border;

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
                    // gridPos + size - 1 为房间右/上边缘，需 ≤ xMax-1/yMax-1
                    int x = _rng.Range(xMin, xMax - size.x + 1);
                    int y = _rng.Range(yMin, yMax - size.y + 1);

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

        /// <summary>重置地图范围为 config 中的原始尺寸（border 仅约束房间 placement，不缩小地图范围）</summary>
        private void ResetMapBounds()
        {
            int width = _baseMapWidth;
            int height = _baseMapHeight;

            // 地图范围固定为 [-W/2, W/2)，border 约束的是房间不能贴边
            _mapBounds = new RectInt(
                -width / 2,
                -height / 2,
                width,
                height
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

        // ==================== Step 4.5: 额外走廊 ====================

        /// <summary>
        /// 在 MST 基础上，以一定概率为尚未直接相连且距离较近的房间对添加额外走廊，
        /// 增加地图的回路，提升玩家路径选择多样性
        /// </summary>
        private void GenerateExtraCorridors()
        {
            if (_config.extraCorridorChance <= 0)
                return;

            int maxDist = _config.extraCorridorMaxDistance;

            for (int i = 0; i < _rooms.Count; i++)
            {
                for (int j = i + 1; j < _rooms.Count; j++)
                {
                    Room a = _rooms[i];
                    Room b = _rooms[j];

                    string pair = MakePairKey(a.id, b.id);

                    // 跳过已通过 MST 生成的走廊
                    if (_generatedCorridorPairs.Contains(pair))
                        continue;

                    // 跳过已直接相连的房间对（避免重复门）
                    if (a.connectedRoomIds.Contains(b.id))
                        continue;

                    int dist = GetManhattanDistance(a.Center, b.Center);
                    if (dist > maxDist)
                        continue;

                    if (_rng.Range(0, 100) < _config.extraCorridorChance)
                    {
                        GenerateCorridorBetween(a, b);
                        _generatedCorridorPairs.Add(pair);
                    }
                }
            }
        }

        // ==================== Step 5: 确定终点 + Elite 概率补充 ====================

        private void FinalizeRoomTypes()
        {
            if (_rooms.Count == 0)
                return;

            // 生成新的终点房间（不再直接复用最远房间）
            PlaceGoalRoom();

            if (_config.eliteRoomCount == 0 && _config.eliteExtraChance > 0)
            {
                foreach (var room in _rooms)
                {
                    // 只将 Normal 房间（且非起点/终点/Boss）转为 Elite
                    if (room.roomType == RoomType.Normal &&
                        _rng.Range(0, 100) < _config.eliteExtraChance)
                    {
                        room.roomType = RoomType.Elite;
                    }
                }
            }
        }

        /// <summary>
        /// 查找 Boss 房间
        /// </summary>
        private Room FindBossRoom()
        {
            foreach (var room in _rooms)
            {
                if (room.roomType == RoomType.Boss)
                    return room;
            }
            return null;
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

        // ==================== 终点房间生成 ====================

        /// <summary>
        /// 生成终点房间
        /// 如果有Boss房间，以Boss为依附点生成终点
        /// 如果没有Boss房间但需要Boss，在最远叶子节点旁生成Boss，再以Boss为依附点生成终点
        /// </summary>
        private void PlaceGoalRoom()
        {
            int startId = FindStartRoomId();

            // 找到最远的叶子节点
            var leafRooms = GetLeafRooms(startId);
            Room farthestLeaf = FindFarthestAmong(leafRooms, startId);

            // 如果需要Boss，在最远叶子节点旁生成Boss房间
            Room bossRoom = null;
            if (_config.bossRoomCount > 0)
            {
                // 在最远叶子节点旁生成Boss
                if (farthestLeaf != null && farthestLeaf.id != startId)
                {
                    if (!TryPlaceBossRoom(farthestLeaf))
                    {
                        ExpandMap();
                        TryPlaceBossRoom(farthestLeaf);
                    }
                    bossRoom = FindBossRoom();
                }
            }

            // 以Boss房间或最远叶子节点为参考生成终点
            Room referenceRoom = bossRoom ?? farthestLeaf;

            if (referenceRoom != null && referenceRoom.id != startId)
            {
                if (!TryPlaceGoalRoom(referenceRoom))
                {
                    ExpandMap();
                    TryPlaceGoalRoom(referenceRoom);
                }
            }
        }

        /// <summary>
        /// 获取所有叶子节点（连接数为1的房间，排除起点）
        /// </summary>
        private List<Room> GetLeafRooms(int startId)
        {
            var leafRooms = new List<Room>();
            foreach (var room in _rooms)
            {
                if (room.roomType == RoomType.Start)
                    continue;
                if (room.connectedRoomIds.Count == 1)
                    leafRooms.Add(room);
            }
            return leafRooms;
        }

        /// <summary>
        /// 从房间列表中找到距离起点最远的房间
        /// </summary>
        private Room FindFarthestAmong(List<Room> rooms, int startId)
        {
            var distances = new Dictionary<int, int>();
            var queue = new Queue<int>();
            var visited = new HashSet<int>();

            queue.Enqueue(startId);
            visited.Add(startId);
            distances[startId] = 0;

            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                int currentDist = distances[currentId];

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

            Room farthest = null;
            int maxDist = -1;
            foreach (var room in rooms)
            {
                // 跳过起点本身
                if (room.id == startId)
                    continue;
                if (distances.TryGetValue(room.id, out int dist) && dist >= maxDist)
                {
                    maxDist = dist;
                    farthest = room;
                }
            }
            return farthest ?? rooms[0];
        }

        /// <summary>
        /// 在参考房间附近生成指定类型的房间
        /// 由近到远、从八个方向尝试放置
        /// </summary>
        private bool TryPlaceRoomNear(Room referenceRoom, RoomType roomType)
        {
            var configs = _config.GetTemplates(roomType);
            if (configs == null || configs.Count == 0)
                return false;

            // 八个方向
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),    // 上
                new Vector2Int(0, -1),    // 下
                new Vector2Int(1, 0),     // 右
                new Vector2Int(-1, 0),    // 左
                new Vector2Int(1, 1),     // 右上
                new Vector2Int(-1, 1),    // 左上
                new Vector2Int(1, -1),    // 右下
                new Vector2Int(-1, -1)    // 左下
            };

            // 获取目标房间尺寸
            var config = configs[0];
            Vector2Int size = config.GetRandomSize(_rng);

            // 由近到远尝试：距离从1开始，逐步增加
            int maxDistance = Mathf.Max(_mapBounds.width, _mapBounds.height);
            for (int distance = 1; distance <= maxDistance; distance++)
            {
                foreach (var dir in directions)
                {
                    // 计算目标位置
                    Vector2Int targetCenter = referenceRoom.Center + dir * distance;
                    Vector2Int gridPos = new Vector2Int(targetCenter.x - size.x / 2, targetCenter.y - size.y / 2);

                    var candidate = new Room
                    {
                        id = _nextRoomId++,
                        gridPos = gridPos,
                        size = size,
                        roomType = roomType
                    };

                    // 检查房间是否有效且不覆盖走廊
                    if (!IsOverlapping(candidate) && IsWithinBounds(candidate) && !IsCorridorOverlappingRoom(candidate))
                    {
                        // 房间有效，放置房间
                        _rooms.Add(candidate);
                        AddRoomFloorTiles(candidate);

                        // 将参考房间与房间连接（双向）
                        referenceRoom.connectedRoomIds.Add(candidate.id);
                        candidate.connectedRoomIds.Add(referenceRoom.id);
                        return true;
                    }

                    // 重置id，因为这个候选位置不可以用
                    _nextRoomId--;
                }
            }

            return false;
        }

        /// <summary>
        /// 在参考房间附近生成终点房间
        /// </summary>
        private bool TryPlaceGoalRoom(Room referenceRoom)
        {
            return TryPlaceRoomNear(referenceRoom, RoomType.Goal);
        }

        /// <summary>
        /// 在参考房间附近生成Boss房间
        /// </summary>
        private bool TryPlaceBossRoom(Room referenceRoom)
        {
            return TryPlaceRoomNear(referenceRoom, RoomType.Boss);
        }

        /// <summary>
        /// 检查从参考房间到候选房间的走廊路径是否通畅（不会被其他房间阻断）
        /// </summary>
        private bool IsCorridorPathClear(Room from, Room to)
        {
            Vector2Int edgeA = GetClosestEdgePoint(from, to.Center);
            Vector2Int edgeB = GetClosestEdgePoint(to, from.Center);

            // 模拟L型走廊路径，检查是否经过其他房间
            int x = edgeA.x;
            while (x != edgeB.x)
            {
                for (int dy = -_corridorWidth / 2; dy <= _corridorWidth / 2; dy++)
                {
                    Vector2Int tile = new Vector2Int(x, edgeA.y + dy);
                    if (IsTileInsideAnyRoom(tile))
                        return false;
                }
                x += (edgeB.x > x) ? 1 : -1;
            }
            int y = edgeA.y;
            while (y != edgeB.y)
            {
                for (int dx = -_corridorWidth / 2; dx <= _corridorWidth / 2; dx++)
                {
                    Vector2Int tile = new Vector2Int(edgeB.x + dx, y);
                    if (IsTileInsideAnyRoom(tile))
                        return false;
                }
                y += (edgeB.y > y) ? 1 : -1;
            }
            return true;
        }

        /// <summary>
        /// 检查指定格子是否在任何房间内部（排除起点房间）
        /// </summary>
        private bool IsTileInsideAnyRoom(Vector2Int tile)
        {
            foreach (var room in _rooms)
            {
                if (room.roomType == RoomType.Start)
                    continue;
                if (room.Bounds.Contains(tile))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 计算from房间相对于to的外侧方向（背向to的方向）
        /// </summary>
        private Vector2Int GetOutwardDirection(Room from, Room to)
        {
            Vector2Int dir = from.Center - to.Center;
            if (dir.x > 0) dir.x = 1;
            else if (dir.x < 0) dir.x = -1;
            else dir.x = 0;

            if (dir.y > 0) dir.y = 1;
            else if (dir.y < 0) dir.y = -1;
            else dir.y = 0;

            // 如果方向为(0,0)，使用随机方向
            if (dir.x == 0 && dir.y == 0)
            {
                int randomDir = _rng.Range(0, 4);
                switch (randomDir)
                {
                    case 0: dir = new Vector2Int(1, 0); break;
                    case 1: dir = new Vector2Int(-1, 0); break;
                    case 2: dir = new Vector2Int(0, 1); break;
                    case 3: dir = new Vector2Int(0, -1); break;
                }
            }

            // 外侧是背向起点的方向
            return new Vector2Int(-dir.x, -dir.y);
        }

        /// <summary>
        /// 检查房间是否在地图边界内
        /// </summary>
        private bool IsWithinBounds(Room room)
        {
            return room.Bounds.xMin >= _mapBounds.xMin &&
                   room.Bounds.xMax <= _mapBounds.xMax &&
                   room.Bounds.yMin >= _mapBounds.yMin &&
                   room.Bounds.yMax <= _mapBounds.yMax;
        }

        /// <summary>
        /// 检查房间是否与任何现有走廊重叠
        /// </summary>
        private bool IsCorridorOverlappingRoom(Room room)
        {
            // 在走廊生成前，检查是否会与现有房间的连接路径冲突
            // 走廊是从房间边界点出发的L型路径
            foreach (var existingRoom in _rooms)
            {
                if (existingRoom.id == room.id)
                    continue;

                foreach (int connectedId in existingRoom.connectedRoomIds)
                {
                    if (connectedId >= _nextRoomId) // 跳过尚未分配的连接（终点房间的连接）
                        continue;

                    Room connected = FindRoom(connectedId);
                    if (connected == null)
                        continue;

                    // 获取L型走廊路径上的格子
                    Vector2Int edgeA = GetClosestEdgePoint(existingRoom, connected.Center);
                    Vector2Int edgeB = GetClosestEdgePoint(connected, existingRoom.Center);

                    // 检查走廊路径是否经过候选房间
                    int x = edgeA.x;
                    while (x != edgeB.x)
                    {
                        for (int dy = -_corridorWidth / 2; dy <= _corridorWidth / 2; dy++)
                        {
                            if (room.Bounds.Contains(new Vector2Int(x, edgeA.y + dy)))
                                return true;
                        }
                        x += (edgeB.x > x) ? 1 : -1;
                    }
                    int y = edgeA.y;
                    while (y != edgeB.y)
                    {
                        for (int dx = -_corridorWidth / 2; dx <= _corridorWidth / 2; dx++)
                        {
                            if (room.Bounds.Contains(new Vector2Int(edgeB.x + dx, y)))
                                return true;
                        }
                        y += (edgeB.y > y) ? 1 : -1;
                    }
                }
            }
            return false;
        }

        // ==================== Step 6: 生成走廊 ====================

        private void GenerateCorridors()
        {
            foreach (var room in _rooms)
            {
                foreach (int neighborId in room.connectedRoomIds)
                {
                    string pair = MakePairKey(room.id, neighborId);
                    if (_generatedCorridorPairs.Contains(pair))
                        continue;
                    _generatedCorridorPairs.Add(pair);

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
