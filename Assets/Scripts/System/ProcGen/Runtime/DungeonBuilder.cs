using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using ProcGen.Config;
using ProcGen.Core;
using ProcGen.Generator;
using ProcGen.Seed;

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

        [Header("瓦片资源")]
        [SerializeField] private TileBase _floorNormalTile;       // 地面瓦片
        [SerializeField] private TileBase _floorStartTile;  // 开始地面瓦片
        [SerializeField] private TileBase _floorGoalTile;   // 终点地面瓦片
        [SerializeField] private TileBase _floorShopTile;   // 商店地面瓦片
        [SerializeField] private TileBase _floorTreasureTile;   // 宝藏地面瓦片
        [SerializeField] private TileBase _floorBossTile;   // Boss地面瓦片
        [SerializeField] private TileBase _floorEliteTile;   // 精英地面瓦片
        [SerializeField] private TileBase _floorCorridorTile;   // 走廊地面瓦片
        [SerializeField] private TileBase _wallTile;        // 墙壁瓦片

        [Header("生成配置")]
        [SerializeField] private DungeonModel_SO _dungeonModel; // Inspector上的地牢配置SO

        [Header("调试")]
        [SerializeField] private bool _showDebugLog = true;    // 是否打印调试日志

        // ==================== 私有字段 ====================
        private IDungeonGenerator _generator; // 生成器实例
        private DungeonGraph _currentGraph;   // 当前生成的地牢图（供外部访问）

        // ==================== Unity 生命周期 ====================

        private void Awake()
        {
            // 使用 RoomFirst 生成器来生成地图
            // 后续想要更改生成器 只需要重写IDungeonGenerator接口 然后赋值给_generator就好了
            // 如果想要 多个生成器 可供选择，也可以用IDungeonGenerator列表来存储
            _generator = new RoomFirstGenerator();
        }

        // ==================== 公共方法 ====================

        /// <summary>生成并构建地牢场景（使用随机种子，仅用于编辑器/调试）</summary>
        /// <param name="config">地牢配置（为空则使用 Inspector 引用）</param>
        public void Build(DungeonModel_SO config = null)
        {
            Build(config, new GameSeed(System.Environment.TickCount.ToString()));
        }

        /// <summary>生成并构建地牢场景（使用指定游戏种子）</summary>
        /// <param name="config">地牢配置（为空则使用 Inspector 引用）</param>
        /// <param name="seed">游戏种子（由外部 GameSeed 管理器创建）</param>
        public void Build(DungeonModel_SO config, GameSeed seed)
        {
            // 参数优先级：传入 > Inspector
            config ??= _dungeonModel;

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

            // Step 2: 生成地牢图数据
            Log($"开始生成地牢... (Seed: {seed.SeedString})");
            var graph = _generator.Generate(config, seed);

            if (graph.allRooms.Count == 0)
            {
                LogError("生成失败：未生成任何有效房间。");
                return;
            }

            // Step 3: 缓存 graph 供外部访问
            _currentGraph = graph;

            // Step 4: 填充地面 Tilemap
            FillTilemap(_floorTilemap, _floorNormalTile, graph.GetRoomFloorTilesByType(RoomType.Normal));
            FillTilemap(_floorTilemap, _floorStartTile, graph.GetRoomFloorTilesByType(RoomType.Start));
            FillTilemap(_floorTilemap, _floorGoalTile, graph.GetRoomFloorTilesByType(RoomType.Goal));
            FillTilemap(_floorTilemap, _floorShopTile, graph.GetRoomFloorTilesByType(RoomType.Shop));
            FillTilemap(_floorTilemap, _floorTreasureTile, graph.GetRoomFloorTilesByType(RoomType.Treasure));
            FillTilemap(_floorTilemap, _floorBossTile, graph.GetRoomFloorTilesByType(RoomType.Boss));
            FillTilemap(_floorTilemap, _floorEliteTile, graph.GetRoomFloorTilesByType(RoomType.Elite));
            FillTilemap(_floorTilemap, _floorCorridorTile, graph.GetCorridorFloorTiles());

            // Step 5: 填充墙壁 Tilemap（地面周围一圈）
            var wallTiles = graph.GetAllWallTiles();
            FillTilemap(_wallTilemap, _wallTile, wallTiles);

            Log($"地牢生成完成：{graph.allRooms.Count} 个房间，{graph.corridors.Count} 条走廊，" +
                $"地面 {graph.GetAllFloorTiles().Count} 格，墙壁 {wallTiles.Count} 格。");

            // Step 6: 打印房间类型分布
            LogRoomTypeSummary(graph);
        }

        /// <summary>清空所有 Tilemap</summary>
        public void Clear()
        {
            _floorTilemap?.ClearAllTiles();
            _wallTilemap?.ClearAllTiles();
            _currentGraph = null;
            Log("Tilemap 已清空。");
        }

        /// <summary>获取当前生成的地牢图（Build 之后有效）</summary>
        public DungeonGraph GetGraph() => _currentGraph;

        /// <summary>重新生成（使用随机种子重新生成，仅用于编辑器/调试）</summary>
        [ContextMenu("重新生成地牢")]
        public void Rebuild()
        {
            Build();
        }

        // ==================== 私有方法 ====================

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
            if (_showDebugLog)
                Debug.Log($"[DungeonBuilder] {msg}");
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[DungeonBuilder] {msg}");
        }
    }
}
