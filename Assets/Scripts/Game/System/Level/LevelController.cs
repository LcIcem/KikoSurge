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

    private PlayerHandler _playerHandler;

    private DungeonBuilder _builder;
    private GameRandom _rng;
    private int _currentLayerIndex;
    private long _sessionSeed;

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

        _playerHandler = new PlayerHandler();
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

        // 获取起始位置并创建玩家
        Vector3 startWorldPos = GetStartRoomWorldPos();
        _playerHandler.CreatePlayer(startWorldPos);

        EventCenter.Instance.Publish(GameEventID.OnLayerEnter, _currentLayerIndex);
    }

    /// <summary>
    /// 进入下一层（R 键触发）
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

        // 构建新地牢
        _builder.Build(GetCurrentLayerModel(), _rng);

        // 获取新地牢的起始位置并激活玩家
        Vector3 startWorldPos = GetStartRoomWorldPos();
        _playerHandler.ReactivatePlayer(startWorldPos);

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
    /// 派生层种子
    /// </summary>
    private long DeriveLayerSeed(long sessionSeed, int layerIndex)
    {
        return sessionSeed + layerIndex * 100;
    }
}
