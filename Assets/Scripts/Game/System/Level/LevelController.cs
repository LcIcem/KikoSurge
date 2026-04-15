using System;
using System.Collections.Generic;
using Game.Event;
using LcIcemFramework.Core;
using ProcGen.Config;
using ProcGen.Core;
using ProcGen.Runtime;
using ProcGen.Seed;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Tilemap 引用")]
    [SerializeField] private Tilemap _floorTilemap;

    [Header("房间行为配置")]
    [SerializeField] private RoomBehaviourTable_SO _roomBehaviourTable;

    [Header("终点检查点预设体")]
    [SerializeField] private GameObject _checkpointPrefab;

    private RoomController _roomController;
    private PlayerHandler _playerHandler;

    private DungeonBuilder _builder;
    private GameRandom _rng;
    private int _currentLayerIndex;
    private long _sessionSeed;
    private GameObject _currentCheckpoint;  // 当前检查点

    public DungeonGraph CurrentGraph => _builder?.GetGraph();
    public bool IsBuildCompleted => _builder?.IsBuildCompleted ?? false;
    public int CurrentLayerIndex => _currentLayerIndex;
    public int MaxLayerCount => _dungeonModels?.Count ?? 0;
    public bool IsLastLayer => _currentLayerIndex >= MaxLayerCount - 1;

    private void Awake()
    {
        _builder = GetComponent<DungeonBuilder>();
        if (_builder == null)
            _builder = gameObject.AddComponent<DungeonBuilder>();

        _roomController = new RoomController();
        _playerHandler = new PlayerHandler();

        EventCenter.Instance.Subscribe(GameEventID.OnRequestRoomRefresh, OnRequestRoomRefresh);
    }

    private void OnRequestRoomRefresh()
    {
        if (_playerHandler != null && _playerHandler.PlayerInstance != null)
        {
            _roomController.RefreshCurrentRoom(_playerHandler.PlayerInstance.transform.position);
        }
    }

    private void Update()
    {
        if (_roomController != null && _playerHandler.PlayerInstance != null)
        {
            _roomController.CheckAndSpawnInRoom(_playerHandler.PlayerInstance.transform.position);
        }

        // 检查 E 键按下（检查点交互）
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteractCheckpoint();
        }
    }

    /// <summary>
    /// 尝试与检查点交互
    /// </summary>
    private void TryInteractCheckpoint()
    {
        if (_currentCheckpoint == null)
            return;

        Checkpoint checkpoint = _currentCheckpoint.GetComponent<Checkpoint>();
        if (checkpoint == null || !checkpoint.CanInteract)
            return;

        checkpoint.Interact();

        if (IsLastLayer)
        {
            Debug.Log("[LevelController] 最后一层，关卡完成");
            EventCenter.Instance.Publish(GameEventID.OnLayerComplete, _currentLayerIndex);
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
        _sessionSeed = sessionSeed;
        _currentLayerIndex = -1;
    }

    /// <summary>
    /// 进入关卡第一层（首次创建玩家）
    /// </summary>
    public void EnterFirstLayer()
    {
        _currentLayerIndex = 0;
        _rng = new GameRandom(DeriveLayerSeed(_sessionSeed, _currentLayerIndex));

        _builder.Build(GetCurrentLayerModel(), _rng);

        // 初始化 RoomController
        _roomController.Initialize(_builder.GetTileData(), _roomBehaviourTable, _rng);

        // 获取起始位置并创建玩家
        Vector3 startWorldPos = GetStartRoomWorldPos();
        _playerHandler.CreatePlayer(startWorldPos);

        // 立即检测当前房间并发布事件（刷新UI）
        _roomController.DetectCurrentRoom(startWorldPos);

        // 生成终点检查点
        SpawnCheckpoint();

        EventCenter.Instance.Publish(GameEventID.OnLayerEnter, _currentLayerIndex);
    }

    /// <summary>
    /// 进入下一层
    /// </summary>
    public void EnterNextLayer()
    {
        // 检查是否已达到最大层数
        if (_currentLayerIndex >= MaxLayerCount - 1)
        {
            Debug.Log("[LevelController] 已达最大层数，关卡完成");
            EventCenter.Instance.Publish(GameEventID.OnLayerComplete, _currentLayerIndex);
            return;
        }

        _currentLayerIndex++;

        // 派生新层种子
        _rng = new GameRandom(DeriveLayerSeed(_sessionSeed, _currentLayerIndex));

        // 失活玩家
        _playerHandler.DeactivatePlayer();

        // 清理上一层的敌人和掉落物
        EnemyFactory.Instance.ReleaseAll();
        LootManager.Instance.ClearAll();

        // 构建新地牢
        _builder.Build(GetCurrentLayerModel(), _rng);

        // 重新初始化 RoomController
        _roomController.Initialize(_builder.GetTileData(), _roomBehaviourTable, _rng);

        // 获取新地牢的起始位置并激活玩家
        Vector3 startWorldPos = GetStartRoomWorldPos();
        _playerHandler.ReactivatePlayer(startWorldPos);

        // 立即检测当前房间并发布事件（刷新UI）
        _roomController.DetectCurrentRoom(startWorldPos);

        // 生成终点检查点
        SpawnCheckpoint();

        EventCenter.Instance.Publish(GameEventID.OnLayerEnter, _currentLayerIndex);
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
        if (graph == null || _floorTilemap == null)
        {
            Debug.LogWarning("[LevelController] Graph or Tilemap is null");
            return Vector3.zero;
        }

        Room startRoom = graph.GetRoom(graph.startRoomId);
        Vector2Int center = startRoom.Center;
        return _floorTilemap.CellToWorld(new Vector3Int(center.x, center.y, 0));
    }

    /// <summary>
    /// 获取终点房间的世界坐标
    /// </summary>
    private Vector3 GetGoalRoomWorldPos()
    {
        var graph = CurrentGraph;
        if (graph == null || _floorTilemap == null)
        {
            Debug.LogWarning("[LevelController] Graph or Tilemap is null");
            return Vector3.zero;
        }

        Room goalRoom = graph.GetRoom(graph.goalRoomId);
        Vector2Int center = goalRoom.Center;
        return _floorTilemap.CellToWorld(new Vector3Int(center.x, center.y, 0));
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
    }

    /// <summary>
    /// 派生层种子
    /// </summary>
    private long DeriveLayerSeed(long sessionSeed, int layerIndex)
    {
        return sessionSeed + layerIndex * 100;
    }

    private void OnDestroy()
    {
        _roomController?.Dispose();
    }
}
