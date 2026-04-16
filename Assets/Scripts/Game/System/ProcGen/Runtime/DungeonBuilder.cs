using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using ProcGen.Config;
using ProcGen.Core;
using ProcGen.Generator;
using ProcGen.Seed;
using Pathfinding;
using System.Collections;

namespace ProcGen.Runtime
{
    /// <summary>地牢场景构建器
    /// 将 DungeonGraph 数据结构实例化为 Unity 场景中的 Tilemap 对象
    /// </summary>
    public class DungeonBuilder : MonoBehaviour
    {
        // ==================== SerializeField 引用 ====================
        [Header("Tilemap 引用")]
        [SerializeField] private Tilemap _floorTilemap;  // 地面层瓦片地图
        [SerializeField] private Tilemap _wallTilemap;   // 墙壁层瓦片地图
        [SerializeField] private Tilemap _doorTilemap;   // 门层瓦片地图


        [Header("瓦片资源")]
        [SerializeField] private TileInfo_SO _tileInfo;

        // ==================== 私有字段 ====================
        private IDungeonGenerator _generator; // 生成器实例
        private DungeonGraph _currentGraph;   // 当前生成的地牢图（供外部访问）
        private DungeonTileData _currentTileData; // 当前瓦片预计算数据（供外部访问）

        // ==================== 公有属性 ====================
        /// <summary>获取当前生成的地牢图（Build 之后有效）</summary>
        public DungeonGraph GetGraph() => _currentGraph;

        /// <summary>获取当前瓦片预计算数据（Build 之后有效，外部游戏代码使用）</summary>
        public DungeonTileData GetTileData() => _currentTileData;

        /// <summary>地牢是否已构建完毕（Tilemap 填充完毕后为 true）</summary>
        public bool IsBuildCompleted { get; private set; }

        /// <summary>获取地面层 Tilemap</summary>
        public Tilemap FloorTilemap => _floorTilemap;

        /// <summary>获取墙壁层 Tilemap（供 RoomController 关门/开门使用）</summary>
        public Tilemap WallTilemap => _wallTilemap;

        /// <summary>获取瓦片信息配置</summary>
        public TileInfo_SO TileInfo => _tileInfo;

        // ==================== 运行时注入方法 ====================

        /// <summary>运行时设置 Tilemap 引用（用于 Prefab 模式）</summary>
        public void SetTilemapReferences(Tilemap floor, Tilemap wall, Tilemap door)
        {
            _floorTilemap = floor;
            _wallTilemap = wall;
            _doorTilemap = door;
        }

        /// <summary>运行时设置 TileInfo（用于 Prefab 模式）</summary>
        public void SetTileInfo(TileInfo_SO tileInfo)
        {
            _tileInfo = tileInfo;
        }

        // ==================== Unity 生命周期 ====================

        private void Awake()
        {
            // 使用 RoomFirst 生成器来生成地图
            // 后续想要更改生成器 只需要重写IDungeonGenerator接口 然后赋值给_generator就好了
            // 如果想要 多个生成器 可供选择，也可以用IDungeonGenerator列表来存储
            _generator = new RoomFirstGenerator();
        }

        // ==================== 公共方法 ====================

        /// <summary>生成并构建地牢场景（使用指定游戏种子）</summary>
        /// <param name="config">地牢配置（为空则使用 Inspector 引用）</param>
        /// <param name="rng">随机数生成器</param>
        public void Build(DungeonModel_SO config, GameRandom rng)
        {
            if (config == null)
            {
                LogError("地牢配置为空，请传入 DungeonModel_SO 或在 Inspector 中指定。");
                return;
            }

            if (_floorTilemap == null || _wallTilemap == null)
            {
                LogError("Tilemap 引用未设置，请在 Inspector 中配置。");
                return;
            }

            // Step 1: 清空现有地图
            Clear();

            // Step 2: 生成地牢图数据和瓦片预计算数据
            Log($"开始生成地牢... (Seed: {rng.SeedString})");
            var (graph, tileData) = _generator.Generate(config, rng);

            if (graph.allRooms.Count == 0)
            {
                LogError("生成失败：未生成任何有效房间。");
                return;
            }

            // Step 3: 缓存数据供外部访问
            _currentGraph = graph;
            _currentTileData = tileData;

            // Step 4: 构建 TileType → TileBase 字典
            var tileDict = BuildTileDict(_tileInfo);

            // Step 5: 填充地面 Tilemap
            FillRoom(_floorTilemap, tileData, tileDict);

            // Step 6: 填充墙壁 Tilemap（每个房间类型单独处理）
            FillRoomWall(_wallTilemap, tileData, tileDict);

            // Step 7: 填充空白 Tilemap（地图范围内非地面非墙壁的格子）
            FillVoid(_wallTilemap, tileData, tileDict);

            // Step 8: 填充门 Tilemap
            FillDoor(_doorTilemap, tileData, tileDict);

            // Step 9: 标记构建完毕
            IsBuildCompleted = true;

            Log($"地牢生成完成：{graph.allRooms.Count} 个房间，{graph.corridors.Count} 条走廊，" +
                $"地面 {tileData.allFloorTiles.Count} 格，墙壁 {tileData.allWallTiles.Count} 格，门 {tileData.allDoorTiles.Count} 格。");

            // 打印房间类型分布
            LogRoomTypeSummary(graph);

            // 生成AStarPath
            var astar = AstarPath.active;
            var grid = astar.data.gridGraph;

            // 设置寻路网格尺寸和原点（center 必须对齐 mapBounds 中心，否则网格和地牢错位）
            int gridW = _currentGraph.mapBounds.width;
            int gridH = _currentGraph.mapBounds.height;
            Vector3 gridCenter = new Vector3(
                _currentGraph.mapBounds.xMin + gridW * 0.5f,
                _currentGraph.mapBounds.yMin + gridH * 0.5f,
                0.1f
            );

            grid.SetDimensions(gridW, gridH, 1);
            grid.center = gridCenter;
            StartCoroutine(ScanAfterPhysicsUpdate(astar));
        }

        private IEnumerator ScanAfterPhysicsUpdate(AstarPath astar)
        {
            yield return null; // 等待一帧，让 TilemapCollider2D 完成物理更新
            yield return new WaitForFixedUpdate(); // 等待物理系统处理碰撞
            astar.Scan();
        }

        /// <summary>清空所有 Tilemap</summary>
        public void Clear()
        {
            _floorTilemap?.ClearAllTiles();
            _wallTilemap?.ClearAllTiles();
            _doorTilemap?.ClearAllTiles();
            IsBuildCompleted = false;
            _currentGraph = null;
            _currentTileData = null;
            Log("Tilemap 已清空。");
        }

        // ==================== 私有方法 ====================

        /// <summary>从 TileInfo 构建 TileType → TileBase 查找字典</summary>
        private Dictionary<TileType, TileBase> BuildTileDict(TileInfo_SO tileInfo)
        {
            var dict = new Dictionary<TileType, TileBase>();
            if (tileInfo?.tiles == null)
                return dict;
            foreach (var tile in tileInfo.tiles)
            {
                if (tile.tile != null)
                    dict[tile.type] = tile.tile;
            }
            return dict;
        }

        /// <summary>根据字典遍历所有 RoomType，填充地面 Tilemap</summary>
        private void FillRoom(Tilemap tilemap, DungeonTileData tileData, Dictionary<TileType, TileBase> tileDict)
        {
            foreach (RoomType roomType in System.Enum.GetValues(typeof(RoomType)))
            {
                TileType floorType = RoomTypeToFloorTile(roomType);
                // 优先用具体类型，没有则回退到通用 FloorTile
                TileBase tile = tileDict.GetValueOrDefault(floorType, null)
                    ?? tileDict.GetValueOrDefault(TileType.FloorTile, null);
                if (tile == null)
                    continue;

                if (tileData.floorTilesByRoomType.TryGetValue(roomType, out var tiles) && tiles.Count > 0)
                    FillTilemap(tilemap, tile, tiles);
            }

            // 走廊单独处理，优先 FloorCorridorTile，回退到 FloorTile
            TileBase corridorTile = tileDict.GetValueOrDefault(TileType.FloorCorridorTile, null)
                ?? tileDict.GetValueOrDefault(TileType.FloorTile, null);
            if (corridorTile != null)
            {
                var corridorFloors = new HashSet<Vector2Int>();
                foreach (var corridor in _currentGraph.corridors)
                {
                    if (tileData.TryGetCorridorFloorTiles(corridor.id, out var tiles))
                        corridorFloors.UnionWith(tiles);
                }
                if (corridorFloors.Count > 0)
                    FillTilemap(tilemap, corridorTile, corridorFloors);
            }
        }

        /// <summary>根据字典遍历所有 RoomType，填充墙壁 Tilemap（每个房间单独处理墙壁）</summary>
        private void FillRoomWall(Tilemap tilemap, DungeonTileData tileData, Dictionary<TileType, TileBase> tileDict)
        {
            foreach (RoomType roomType in System.Enum.GetValues(typeof(RoomType)))
            {
                TileType wallType = RoomTypeToWallTile(roomType);
                // 优先用具体类型，没有则回退到通用 WallTile
                TileBase tile = tileDict.GetValueOrDefault(wallType, null)
                    ?? tileDict.GetValueOrDefault(TileType.WallTile, null);
                if (tile == null)
                    continue;

                if (tileData.wallTilesByRoomType.TryGetValue(roomType, out var tiles) && tiles.Count > 0)
                    FillTilemap(tilemap, tile, tiles);
            }

            // 走廊墙壁单独处理，优先 WallCorridorTile，回退到 WallTile
            TileBase corridorWallTile = tileDict.GetValueOrDefault(TileType.WallCorridorTile, null)
                ?? tileDict.GetValueOrDefault(TileType.WallTile, null);
            if (corridorWallTile != null)
            {
                var corridorWalls = new HashSet<Vector2Int>();
                foreach (var corridor in _currentGraph.corridors)
                {
                    if (tileData.TryGetCorridorWallTiles(corridor.id, out var tiles))
                        corridorWalls.UnionWith(tiles);
                }
                if (corridorWalls.Count > 0)
                    FillTilemap(tilemap, corridorWallTile, corridorWalls);
            }
        }

        /// <summary>填充地图范围内的空白区域（既非地面也非墙壁的格子）</summary>
        private void FillVoid(Tilemap tilemap, DungeonTileData tileData, Dictionary<TileType, TileBase> tileDict)
        {
            TileBase emptyTile = tileDict.GetValueOrDefault(TileType.EmptyTile, null);
            if (emptyTile == null)
                return;

            // 使用生成时实际使用的地图范围（可能因 ExpandMap 扩容而大于 config 原始值）
            RectInt bounds = tileData.mapBounds;

            var voidTiles = new HashSet<Vector2Int>();
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (!tileData.allFloorTiles.Contains(pos) && !tileData.allWallTiles.Contains(pos))
                        voidTiles.Add(pos);
                }
            }

            if (voidTiles.Count > 0)
                FillTilemap(tilemap, emptyTile, voidTiles);
        }

        /// <summary>根据字典遍历所有房间，填充门 Tilemap（覆盖地面瓦片）</summary>
        private void FillDoor(Tilemap tilemap, DungeonTileData tileData, Dictionary<TileType, TileBase> tileDict)
        {
            TileBase doorTile = tileDict.GetValueOrDefault(TileType.DoorTile, null);
            if (doorTile == null)
                return;

            if (tileData.allDoorTiles.Count > 0)
                FillTilemap(tilemap, doorTile, tileData.allDoorTiles);
        }

        /// <summary>将 RoomType 映射为对应的 Wall TileType</summary>
        private TileType RoomTypeToWallTile(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Start => TileType.WallStartTile,
                RoomType.Normal => TileType.WallNormalTile,
                RoomType.Goal => TileType.WallGoalTile,
                RoomType.Elite => TileType.WallEliteTile,
                RoomType.Treasure => TileType.WallTreasureTile,
                RoomType.Boss => TileType.WallBossTile,
                RoomType.Shop => TileType.WallShopTile,
                RoomType.Event => TileType.WallEventTile,
                RoomType.Rest => TileType.WallEventTile,
                _ => TileType.WallNormalTile,
            };
        }

        /// <summary>将 RoomType 映射为对应的 Floor TileType</summary>
        private TileType RoomTypeToFloorTile(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Start => TileType.FloorStartTile,
                RoomType.Normal => TileType.FloorNormalTile,
                RoomType.Goal => TileType.FloorGoalTile,
                RoomType.Elite => TileType.FloorEliteTile,
                RoomType.Treasure => TileType.FloorTreasureTile,
                RoomType.Boss => TileType.FloorBossTile,
                RoomType.Shop => TileType.FloorShopTile,
                RoomType.Event => TileType.FloorEventTile,
                RoomType.Rest => TileType.FloorEventTile,
                _ => TileType.FloorNormalTile,
            };
        }

        /// <summary>根据实际瓦片位置计算包围盒</summary>
        private RectInt CalculateActualBounds(HashSet<Vector2Int> tiles)
        {
            if (tiles == null || tiles.Count == 0)
                return new RectInt(0, 0, 0, 0);

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var pos in tiles)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }

            // 加1因为max是最右/上边界，需要包含它
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>向指定 Tilemap 填充一组格子</summary>
        private void FillTilemap(Tilemap tilemap, TileBase tile, HashSet<Vector2Int> tiles)
        {
            if (tilemap == null || tile == null)
                return;

            foreach (var pos in tiles)
            {
                tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
            }
        }

        /// <summary>打印房间类型统计</summary>
        private void LogRoomTypeSummary(DungeonGraph graph)
        {
            var counts = new Dictionary<RoomType, int>();

            foreach (var room in graph.allRooms)
            {
                if (!counts.ContainsKey(room.roomType))
                    counts[room.roomType] = 0;
                counts[room.roomType]++;
            }

            var lines = new List<string>();
            foreach (var kvp in counts)
            {
                lines.Add($"  {kvp.Key}: {kvp.Value}");
            }

            Log($"房间类型分布：\n{string.Join("\n", lines)}");
        }

        // ==================== 日志工具 ====================

        private void Log(string msg)
        {
            Debug.Log($"[DungeonBuilder] {msg}");
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[DungeonBuilder] {msg}");
        }
    }
}
