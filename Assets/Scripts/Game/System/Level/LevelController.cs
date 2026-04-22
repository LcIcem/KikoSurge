using System;
using System.Collections.Generic;
using Game.Event;
using LcIcemFramework;
using LcIcemFramework.Core;
using ProcGen.Config;
using ProcGen.Core;
using ProcGen.Runtime;
using ProcGen.Seed;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 关卡控制器
/// <para>管理单个关卡内的多层地牢生成，负责地牢数据而非玩家创建</para>
/// </summary>
[RequireComponent(typeof(DungeonBuilder))]
public class LevelController : MonoBehaviour
{
    [Header("地牢配置（每层对应一个配置）")]
    [SerializeField] private List<DungeonModel_SO> _dungeonModels;

    [Header("房间行为配置")]
    [SerializeField] private RoomBehaviourTableConfig _roomBehaviourTable;

    [Header("终点检查点预设体")]
    [SerializeField] private GameObject _checkpointPrefab;

    [Header("休息点预设体（篝火）")]
    [SerializeField] private GameObject _restPointPrefab;

    [Header("宝箱预设体")]
    [SerializeField] private GameObject _chestPrefab;

    [Header("商人预设体")]
    [SerializeField] private GameObject _shopkeeperPrefab;

    [Header("瓦片配置")]
    [SerializeField] private TileInfo_SO _tileInfo;

    [Header("Tilemap 预设体（包含 Grid 及子 Tilemap）")]
    [SerializeField] private GameObject _tilemapPrefab;

    private RoomController _roomController;
    private PlayerHandler _playerHandler;

    private DungeonBuilder _builder;
    private GameRandom _rng;
    private int _currentLayerIndex;
    private long _sessionSeed;
    private GameObject _currentCheckpoint;
    private readonly List<GameObject> _restPoints = new();
    private readonly List<GameObject> _chests = new();
    private readonly List<GameObject> _shopkeepers = new();

    public DungeonGraph CurrentGraph => _builder?.GetGraph();
    public DungeonTileData GetTileData() => _builder?.GetTileData();
    public bool IsBuildCompleted => _builder?.IsBuildCompleted ?? false;
    public int CurrentLayerIndex => _currentLayerIndex;
    public int MaxLayerCount => _dungeonModels?.Count ?? 0;
    public GameObject PlayerInstance => _playerHandler?.PlayerInstance;
    public bool IsLastLayer => _currentLayerIndex >= MaxLayerCount - 1;

    private void Awake()
    {
        _builder = GetComponent<DungeonBuilder>();
        if (_builder == null)
            _builder = gameObject.AddComponent<DungeonBuilder>();

        // 在场景中直接查找 Tilemap 引用
        FindTilemapReferences();

        _roomController = new RoomController();
        _playerHandler = new PlayerHandler();

        EventCenter.Instance.Subscribe(GameEventID.OnRequestRoomRefresh, OnRequestRoomRefresh);
    }

    /// <summary>
    /// 实例化 Tilemap 预设体并提取子 Tilemap 引用
    /// </summary>
    private void FindTilemapReferences()
    {
        if (_tilemapPrefab == null)
        {
            Debug.LogError("[LevelController] _tilemapPrefab is not assigned! Please assign a Tilemap prefab in Inspector.");
            return;
        }

        // 实例化预设体（作为 LevelController 的子物体，销毁时一起清理）
        var instantiated = Instantiate(_tilemapPrefab, Vector3.zero, Quaternion.identity);
        instantiated.transform.SetParent(transform);
        instantiated.name = "TilemapGrid";

        // 从实例化后的子对象中找 Tilemap
        Tilemap floor = null, wall = null, door = null;

        foreach (var tm in instantiated.GetComponentsInChildren<Tilemap>())
        {
            string lowerName = tm.gameObject.name.ToLower();

            if (lowerName.Contains("floor") && floor == null)
                floor = tm;
            else if (lowerName.Contains("wall") && wall == null)
                wall = tm;
            else if (lowerName.Contains("door") && door == null)
                door = tm;
        }

        Debug.Log($"[LevelController] Tilemap instantiated, Floor:{floor?.gameObject.name ?? "null"}, Wall:{wall?.gameObject.name ?? "null"}, Door:{door?.gameObject.name ?? "null"}");

        if (floor == null || wall == null)
        {
            Debug.LogError("[LevelController] Could not find Floor/Wall tilemap in prefab. Make sure the prefab's child Tilemap GameObjects contain 'Floor'/'Wall'/'Door' in their names.");
            return;
        }

        _builder.SetTilemapReferences(floor, wall, door);
        Debug.Log($"[LevelController] TileInfo_SO: {_tileInfo?.name ?? "null"}");
        _builder.SetTileInfo(_tileInfo);
    }

    private void OnRequestRoomRefresh()
    {
        // 如果地牢未构建完成，忽略刷新请求
        if (_builder == null || !_builder.IsBuildCompleted)
        {
            return;
        }

        // 如果玩家已创建，使用玩家位置刷新
        if (_playerHandler != null && _playerHandler.PlayerInstance != null)
        {
            _roomController.RefreshCurrentRoom(_playerHandler.PlayerInstance.transform.position);
            return;
        }

        // 玩家未创建但地牢已构建（异步加载期间），使用起始房间位置刷新
        if (_currentLayerIndex >= 0)
        {
            Vector3 startPos = GetStartRoomWorldPos();
            _roomController.RefreshCurrentRoom(startPos);
        }
    }

    private void Update()
    {
        if (_roomController != null && _playerHandler.PlayerInstance != null)
        {
            _roomController.CheckAndSpawnInRoom(_playerHandler.PlayerInstance.transform.position);
        }
    }

    /// <summary>
    /// 终点检查点激活回调
    /// <para>保存checkpoint后进入下一层（或通关）</para>
    /// </summary>
    private void OnCheckpointActivated()
    {
        // 保存检查点快照到 SessionManager
        var snapshot = CreateCheckpointSnapshot();
        if (snapshot != null && SessionManager.Instance != null)
        {
            SessionManager.Instance.SaveCheckpoint(snapshot);
        }
        else
        {
            Debug.LogWarning($"[LevelController] Cannot save checkpoint: snapshot={(snapshot != null)}, SessionManager={(SessionManager.Instance != null)}");
        }

        if (IsLastLayer)
        {
            Debug.Log("[LevelController] 最后一层，关卡完成");
            EventCenter.Instance.Publish(GameEventID.OnLayerComplete, _currentLayerIndex);
            GameLifecycleManager.Instance.GameClear();
        }
        else
        {
            EnterNextLayer();
        }
    }

    /// <summary>
    /// 初始化关卡（由 GameLifecycleManager 调用）
    /// </summary>
    public void Initialize(long sessionSeed)
    {
        Debug.Log($"[LevelController] Initialize called with seed={sessionSeed}");
        _sessionSeed = sessionSeed;
        _currentLayerIndex = -1;
    }

    /// <summary>
    /// 进入指定层（用于继续游戏时恢复到checkpoint所在层）
    /// </summary>
    public void EnterLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= MaxLayerCount)
        {
            Debug.LogError($"[LevelController] Invalid layer index: {layerIndex}, max is {MaxLayerCount - 1}");
            return;
        }

        _currentLayerIndex = layerIndex;
        _rng = new GameRandom(DeriveLayerSeed(_sessionSeed, _currentLayerIndex));

        // 构建地牢
        Debug.Log($"[LevelController] Building dungeon for layer {layerIndex}...");
        _builder.Build(GetCurrentLayerModel(), _rng);
        Debug.Log($"[LevelController] Dungeon built. IsBuildCompleted={_builder.IsBuildCompleted}, Graph={CurrentGraph != null}, FloorTilemap={_builder?.FloorTilemap != null}");

        // 初始化 RoomController
        _roomController.Initialize(_builder.GetTileData(), _roomBehaviourTable, _rng,
            _builder.WallTilemap, _builder.TileInfo);

        // 检查是否有检查点需要恢复
        Vector3 startWorldPos = GetStartRoomWorldPos();
        var checkpoint = SessionManager.Instance.GetCurrentCheckpoint();
        float checkpointHealth = 0f;
        if (checkpoint != null && checkpoint.floorIndex == _currentLayerIndex)
        {
            RestoreFromCheckpoint(checkpoint);
            startWorldPos = checkpoint.GetPlayerWorldPos();
            checkpointHealth = checkpoint.currentHealth;
            Debug.Log($"[LevelController] Restoring from checkpoint at {startWorldPos}, health={checkpointHealth}");
        }

        Debug.Log($"[LevelController] Creating/reacting player at {startWorldPos}...");

        // 如果是第0层（首次创建玩家），创建玩家；否则激活玩家
        if (layerIndex == 0 && !IsPlayerCreated())
        {
            // 从检查点恢复生命值到 SessionManager（必须在 GetPlayerData 之前调用）
            if (checkpointHealth > 0)
            {
                SessionManager.Instance.SetPlayerHealth(checkpointHealth);
                Debug.Log($"[LevelController] Restored health to SessionManager: {checkpointHealth}");
            }

            var playerData = SessionManager.Instance.GetPlayerData();
            _playerHandler.CreatePlayer(startWorldPos, playerData);
        }
        else
        {
            _playerHandler.ReactivatePlayer(startWorldPos);
        }

        // 立即检测当前房间并发布事件（刷新UI）
        _roomController.DetectCurrentRoom(startWorldPos);

        // 生成终点检查点
        SpawnCheckpoint();

        // 生成休息点（篝火）
        SpawnRestPoint();

        // 生成宝箱
        SpawnChest();

        // 生成商人
        SpawnShopkeeper();

        EventCenter.Instance.Publish(GameEventID.OnLayerEnter, _currentLayerIndex);
    }

    /// <summary>
    /// 检查玩家是否已创建
    /// </summary>
    private bool IsPlayerCreated()
    {
        return _playerHandler?.PlayerInstance != null;
    }

    /// <summary>
    /// 进入关卡第一层（首次创建玩家）
    /// </summary>
    public void EnterFirstLayer()
    {
        EnterLayer(0);
    }

    /// <summary>
    /// 进入下一层
    /// </summary>
    public void EnterNextLayer()
    {
        Debug.Log($"[LevelController] EnterNextLayer called. CurrentLayer={_currentLayerIndex}, MaxLayer={MaxLayerCount}");

        // 检查是否已达到最大层数
        if (_currentLayerIndex >= MaxLayerCount - 1)
        {
            Debug.Log("[LevelController] 已达最大层数，关卡完成");
            EventCenter.Instance.Publish(GameEventID.OnLayerComplete, _currentLayerIndex);
            return;
        }

        int nextLayerIndex = _currentLayerIndex + 1;
        Debug.Log($"[LevelController] Moving to layer {nextLayerIndex}");

        // 失活玩家
        if (_playerHandler != null)
        {
            _playerHandler.DeactivatePlayer();
        }
        else
        {
            Debug.LogWarning("[LevelController] _playerHandler is null!");
        }

        // 清理上一层的敌人和掉落物
        EnemyFactory.Instance?.ReleaseAll();
        LootManager.Instance?.ClearAll();

        // 直接进入下一层（不保存checkpoint，因为checkpoint只在篝火点保存）
        EnterLayer(nextLayerIndex);
    }

    /// <summary>
    /// 获取当前层对应的地牢配置
    /// </summary>
    private DungeonModel_SO GetCurrentLayerModel()
    {
        if (_dungeonModels == null || _dungeonModels.Count == 0)
        {
            Debug.LogWarning("[LevelController] DungeonModels 未配置，使用默认配置");
            return null;
        }

        if (_currentLayerIndex >= _dungeonModels.Count)
        {
            Debug.LogWarning($"[LevelController] 层索引 {_currentLayerIndex} 超出配置数量，返回最后一个配置");
            return _dungeonModels[_dungeonModels.Count - 1];
        }

        return _dungeonModels[_currentLayerIndex];
    }

    /// <summary>
    /// 获取起始房间的世界坐标
    /// </summary>
    private Vector3 GetStartRoomWorldPos()
    {
        var graph = CurrentGraph;
        var floorTilemap = _builder?.FloorTilemap;
        if (graph == null || floorTilemap == null)
        {
            Debug.LogWarning("[LevelController] Graph or FloorTilemap is null");
            return Vector3.zero;
        }

        Room startRoom = graph.GetRoom(graph.startRoomId);
        Vector2Int center = startRoom.Center;
        return floorTilemap.CellToWorld(new Vector3Int(center.x, center.y, 0));
    }

    /// <summary>
    /// 获取终点房间的世界坐标
    /// </summary>
    private Vector3 GetGoalRoomWorldPos()
    {
        var graph = CurrentGraph;
        var floorTilemap = _builder?.FloorTilemap;
        if (graph == null || floorTilemap == null)
        {
            Debug.LogWarning("[LevelController] Graph or FloorTilemap is null");
            return Vector3.zero;
        }

        Room goalRoom = graph.GetRoom(graph.goalRoomId);
        Vector2Int center = goalRoom.Center;
        return floorTilemap.CellToWorld(new Vector3Int(center.x, center.y, 0));
    }

    /// <summary>
    /// 生成终点检查点
    /// </summary>
    private void SpawnCheckpoint()
    {
        // 销毁旧的检查点
        if (_currentCheckpoint != null)
        {
            Destroy(_currentCheckpoint);
            _currentCheckpoint = null;
        }

        if (_checkpointPrefab == null)
            return;

        Vector3 goalPos = GetGoalRoomWorldPos();
        _currentCheckpoint = Instantiate(_checkpointPrefab, goalPos, Quaternion.identity);

        // 订阅检查点激活事件
        if (_currentCheckpoint != null)
        {
            var checkpoint = _currentCheckpoint.GetComponent<Checkpoint>();
            if (checkpoint != null)
            {
                checkpoint.OnCheckpointActivated += OnCheckpointActivated;
            }
        }
    }

    /// <summary>
    /// 获取所有休息房间的世界坐标列表
    /// </summary>
    private List<Vector3> GetAllRestRoomWorldPos()
    {
        var positions = new List<Vector3>();
        var graph = CurrentGraph;
        var floorTilemap = _builder?.FloorTilemap;
        if (graph == null || floorTilemap == null)
        {
            Debug.LogWarning("[LevelController] Graph or FloorTilemap is null");
            return positions;
        }

        // 遍历所有房间，找到所有休息室类型
        foreach (var room in graph.allRooms)
        {
            if (room.roomType == RoomType.Rest)
            {
                // 使用房间实际地面瓦片的中心作为世界坐标
                if (_builder.GetTileData().TryGetRoomFloorTiles(room.id, out var floorTiles) && floorTiles.Count > 0)
                {
                    // 计算所有地面瓦片的中心点（网格坐标）
                    int sumX = 0, sumY = 0;
                    foreach (var tile in floorTiles)
                    {
                        sumX += tile.x;
                        sumY += tile.y;
                    }
                    Vector2Int centerGrid = new Vector2Int(sumX / floorTiles.Count, sumY / floorTiles.Count);
                    Vector3 worldPos = floorTilemap.CellToWorld(new Vector3Int(centerGrid.x, centerGrid.y, 0));
                    // CellToWorld 返回瓦片中心，加上 0.5f 偏移到世界坐标中心
                    worldPos += new Vector3(0.5f, 0.5f, 0);
                    positions.Add(worldPos);
                }
            }
        }

        return positions;
    }

    /// <summary>
    /// 生成休息点（篝火）
    /// </summary>
    private void SpawnRestPoint()
    {
        // 销毁旧的休息点
        foreach (var restPoint in _restPoints)
        {
            if (restPoint != null)
                Destroy(restPoint);
        }
        _restPoints.Clear();

        if (_restPointPrefab == null)
            return;

        var restPositions = GetAllRestRoomWorldPos();
        if (restPositions.Count == 0)
            return;

        foreach (var restPos in restPositions)
        {
            var restPoint = Instantiate(_restPointPrefab, restPos, Quaternion.identity);
            _restPoints.Add(restPoint);
        }

        Debug.Log($"[LevelController] Spawned {_restPoints.Count} rest points (campfires)");
    }

    /// <summary>
    /// 获取所有宝藏房间的世界坐标列表
    /// </summary>
    private List<Vector3> GetAllTreasureRoomWorldPos()
    {
        var positions = new List<Vector3>();
        var graph = CurrentGraph;
        var floorTilemap = _builder?.FloorTilemap;
        if (graph == null || floorTilemap == null)
        {
            Debug.LogWarning("[LevelController] Graph or FloorTilemap is null");
            return positions;
        }

        // 遍历所有房间，找到所有宝藏间类型
        foreach (var room in graph.allRooms)
        {
            if (room.roomType == RoomType.Treasure)
            {
                // 使用房间实际地面瓦片的中心作为世界坐标
                if (_builder.GetTileData().TryGetRoomFloorTiles(room.id, out var floorTiles) && floorTiles.Count > 0)
                {
                    // 计算所有地面瓦片的中心点（网格坐标）
                    int sumX = 0, sumY = 0;
                    foreach (var tile in floorTiles)
                    {
                        sumX += tile.x;
                        sumY += tile.y;
                    }
                    Vector2Int centerGrid = new Vector2Int(sumX / floorTiles.Count, sumY / floorTiles.Count);
                    Vector3 worldPos = floorTilemap.CellToWorld(new Vector3Int(centerGrid.x, centerGrid.y, 0));
                    // CellToWorld 返回瓦片中心，加上 0.5f 偏移到世界坐标中心
                    worldPos += new Vector3(0.5f, 0.5f, 0);
                    positions.Add(worldPos);
                }
            }
        }

        return positions;
    }

    /// <summary>
    /// 获取所有商店房间的世界坐标列表
    /// </summary>
    private List<Vector3> GetAllShopRoomWorldPos()
    {
        var positions = new List<Vector3>();
        var graph = CurrentGraph;
        var floorTilemap = _builder?.FloorTilemap;
        if (graph == null || floorTilemap == null)
        {
            Debug.LogWarning("[LevelController] Graph or FloorTilemap is null");
            return positions;
        }

        // 遍历所有房间，找到所有商店房间类型
        foreach (var room in graph.allRooms)
        {
            if (room.roomType == RoomType.Shop)
            {
                // 使用房间实际地面瓦片的中心作为世界坐标
                if (_builder.GetTileData().TryGetRoomFloorTiles(room.id, out var floorTiles) && floorTiles.Count > 0)
                {
                    // 计算所有地面瓦片的中心点（网格坐标）
                    int sumX = 0, sumY = 0;
                    foreach (var tile in floorTiles)
                    {
                        sumX += tile.x;
                        sumY += tile.y;
                    }
                    Vector2Int centerGrid = new Vector2Int(sumX / floorTiles.Count, sumY / floorTiles.Count);
                    Vector3 worldPos = floorTilemap.CellToWorld(new Vector3Int(centerGrid.x, centerGrid.y, 0));
                    // CellToWorld 返回瓦片中心，加上 0.5f 偏移到世界坐标中心
                    worldPos += new Vector3(0.5f, 0.5f, 0);
                    positions.Add(worldPos);
                }
            }
        }

        return positions;
    }

    /// <summary>
    /// 生成宝箱
    /// </summary>
    private void SpawnChest()
    {
        // 销毁旧的宝箱
        foreach (var chest in _chests)
        {
            if (chest != null)
                Destroy(chest);
        }
        _chests.Clear();

        if (_chestPrefab == null)
            return;

        var treasurePositions = GetAllTreasureRoomWorldPos();
        if (treasurePositions.Count == 0)
            return;

        foreach (var chestPos in treasurePositions)
        {
            var chest = Instantiate(_chestPrefab, chestPos, Quaternion.identity);
            _chests.Add(chest);
        }

        Debug.Log($"[LevelController] Spawned {_chests.Count} chests");
    }

    /// <summary>
    /// 生成商人
    /// </summary>
    private void SpawnShopkeeper()
    {
        // 销毁旧的商人
        foreach (var shopkeeper in _shopkeepers)
        {
            if (shopkeeper != null)
                Destroy(shopkeeper);
        }
        _shopkeepers.Clear();

        if (_shopkeeperPrefab == null)
            return;

        var shopPositions = GetAllShopRoomWorldPos();
        if (shopPositions.Count == 0)
            return;

        foreach (var shopPos in shopPositions)
        {
            var shopkeeper = Instantiate(_shopkeeperPrefab, shopPos, Quaternion.identity);
            _shopkeepers.Add(shopkeeper);

            // 设置种子并生成商品
            var interactable = shopkeeper.GetComponent<ShopkeeperInteractable>();
            if (interactable != null)
            {
                interactable.SetRng(_rng);
                interactable.OnRoomFirstActivated();
            }
        }

        Debug.Log($"[LevelController] Spawned {_shopkeepers.Count} shopkeepers");
    }

    /// <summary>
    /// 派生层种子
    /// </summary>
    private long DeriveLayerSeed(long sessionSeed, int layerIndex)
    {
        return sessionSeed + layerIndex * 100;
    }

    /// <summary>
    /// 销毁玩家（由外部如 RestartGame 调用）
    /// </summary>
    public void DestroyPlayer()
    {
        Debug.Log($"[LevelController] DestroyPlayer called. _playerHandler = {_playerHandler?.GetType().Name ?? "null"}");
        _playerHandler?.DestroyPlayer();
    }

    /// <summary>
    /// 创建当前层的检查点快照（用于中途退出后继续游玩）
    /// <para>注意：死亡后session结束，checkpoint不会被使用</para>
    /// </summary>
    public LayerSnapshot CreateCheckpointSnapshot()
    {
        if (_builder == null || _builder.GetGraph() == null)
        {
            Debug.LogWarning("[LevelController] Cannot create checkpoint: builder or graph is null");
            return null;
        }

        var graph = _builder.GetGraph();
        var snapshot = new LayerSnapshot(_currentLayerIndex, DeriveLayerSeed(_sessionSeed, _currentLayerIndex));

        // 保存房间状态
        var roomStates = _roomController.GetAllRoomStates();
        foreach (var room in graph.allRooms)
        {
            bool isCleared = roomStates.TryGetValue(room.id, out var state) && state == RoomState.Cleared;
            snapshot.roomStates.Add(new RoomSaveState(room.id, room.roomType.ToString(), isCleared));
        }

        // 保存当前房间ID
        snapshot.currentRoomId = _roomController.GetCurrentRoomId();

        // 保存起点/终点房间ID
        snapshot.startRoomId = graph.startRoomId;
        snapshot.bossRoomId = graph.goalRoomId;

        // 保存玩家位置
        if (_playerHandler?.PlayerInstance != null)
        {
            snapshot.SetPlayerWorldPos(_playerHandler.PlayerInstance.transform.position);
        }

        // 保存玩家当前生命值
        snapshot.currentHealth = SessionManager.Instance.GetPlayerHealth();

        // 保存当前激活的buff
        snapshot.activeBuffs = BuffManager.Instance.GetAllActiveBuffs();

        Debug.Log($"[LevelController] Checkpoint snapshot created: floor={_currentLayerIndex}, currentRoom={snapshot.currentRoomId}, health={snapshot.currentHealth}, buffs={snapshot.activeBuffs.Count}");
        return snapshot;
    }

    /// <summary>
    /// 从检查点快照恢复房间状态
    /// </summary>
    public void RestoreFromCheckpoint(LayerSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("[LevelController] Cannot restore from null checkpoint");
            return;
        }

        if (_builder == null || _builder.GetGraph() == null)
        {
            Debug.LogWarning("[LevelController] Cannot restore: builder or graph is null");
            return;
        }

        var graph = _builder.GetGraph();

        // 恢复房间状态
        var roomStates = new Dictionary<int, RoomState>();
        foreach (var roomState in snapshot.roomStates)
        {
            var state = roomState.isCleared ? RoomState.Cleared : RoomState.Unvisited;
            roomStates[roomState.roomId] = state;
        }
        _roomController.RestoreRoomStates(roomStates);

        // 恢复当前房间ID（带验证，防止存档损坏导致房间ID越界）
        int savedRoomId = snapshot.currentRoomId;
        int maxRoomId = graph.allRooms != null ? graph.allRooms.Count - 1 : 0;
        if (savedRoomId >= 0 && savedRoomId <= maxRoomId)
        {
            _roomController.SetCurrentRoomId(savedRoomId);
            Debug.Log($"[LevelController] Restored currentRoomId={savedRoomId}");
        }
        else
        {
            // 降级处理：使用起始房间
            Debug.LogWarning($"[LevelController] Invalid currentRoomId={savedRoomId} (max={maxRoomId}), falling back to startRoomId={snapshot.startRoomId}");
            if (snapshot.startRoomId >= 0 && snapshot.startRoomId <= maxRoomId)
            {
                _roomController.SetCurrentRoomId(snapshot.startRoomId);
            }
        }

        // 恢复玩家位置
        if (_playerHandler?.PlayerInstance != null)
        {
            var worldPos = snapshot.GetPlayerWorldPos();
            _playerHandler.PlayerInstance.transform.position = worldPos;
            Debug.Log($"[LevelController] Restored player position to {worldPos}");
        }

        // 恢复激活的buff
        BuffManager.Instance.RestoreFromSnapshot(snapshot.activeBuffs);

        Debug.Log($"[LevelController] Restored from checkpoint: floor={snapshot.floorIndex}, currentRoom={snapshot.currentRoomId}, buffs={snapshot.activeBuffs?.Count ?? 0}");
    }

    private void OnDestroy()
    {
        try { EnemyFactory.Instance?.ReleaseAll(); } catch (System.Exception e) { Debug.LogException(e); }
        try { LootManager.Instance?.ClearAll(); } catch (System.Exception e) { Debug.LogException(e); }
        try { ManagerHub.Pool?.ClearAll(); } catch (System.Exception e) { Debug.LogException(e); }

        // 取消订阅检查点事件
        if (_currentCheckpoint != null)
        {
            var checkpoint = _currentCheckpoint.GetComponent<Checkpoint>();
            if (checkpoint != null)
            {
                checkpoint.OnCheckpointActivated -= OnCheckpointActivated;
            }
        }

        _roomController?.Dispose();
    }
}