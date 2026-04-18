using System.Collections.Generic;
using Game.Event;
using LcIcemFramework.Core;
using ProcGen.Config;
using ProcGen.Core;
using ProcGen.Seed;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 房间状态
/// </summary>
public enum RoomState
{
    Unvisited,      // 未访问
    InProgress,     // 进行中
    Cleared         // 已清理
}

/// <summary>
/// 房间波次状态
/// </summary>
public class RoomBehaviourState
{
    public List<RoomBehaviourEntry> entries;  // 行为条目列表
    public int pendingCount;                 // 待完成的行为数
}

/// <summary>
/// 房间行为管理器
/// <para>负责检测玩家进入房间、调用房间行为、判断房间完成</para>
/// </summary>
public class RoomController
{
    private DungeonTileData _tileData;
    private RoomBehaviourTableConfig _behaviourTable;
    private GameRandom _rng;
    private int _currentRoomId = -1;
    private readonly Dictionary<int, RoomState> _roomStates = new();
    private readonly Dictionary<int, RoomBehaviourState> _roomBehaviourStates = new();

    // 门控制
    private Tilemap _wallTilemap;
    private TileInfo_SO _tileInfo;

    public RoomController()
    {
        EventCenter.Instance.Subscribe<EnemyKilledParams>(GameEventID.Combat_EnemyKilled, OnEnemyKilled);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize(DungeonTileData tileData, RoomBehaviourTableConfig behaviourTable, GameRandom rng,
        Tilemap wallTilemap, TileInfo_SO tileInfo)
    {
        _tileData = tileData;
        _behaviourTable = behaviourTable;
        _rng = rng;
        _currentRoomId = -1;
        _roomStates.Clear();
        _roomBehaviourStates.Clear();
        _wallTilemap = wallTilemap;
        _tileInfo = tileInfo;
    }

    /// <summary>
    /// 玩家创建/激活后立即检测当前房间并发布事件（用于首帧刷新UI）
    /// </summary>
    public void DetectCurrentRoom(Vector2 playerPos)
    {
        PublishLocationEvent(playerPos);
    }

    /// <summary>
    /// 强制刷新当前位置的房间信息（用于UI初始化时读取当前房间类型）
    /// </summary>
    public void RefreshCurrentRoom(Vector2 playerPos)
    {
        PublishLocationEvent(playerPos);
    }

    private void PublishLocationEvent(Vector2 playerPos)
    {
        if (_tileData == null)
            return;

        int roomId = _tileData.GetRoomIdAt(Vector2Int.FloorToInt(playerPos));

        if (roomId >= 0)
        {
            Room room = _tileData.GetRoom(roomId);
            if (room != null)
            {
                EventCenter.Instance.Publish(GameEventID.OnRoomEnter, new RoomEnterParams
                {
                    roomId = room.id,
                    roomType = room.roomType
                });
            }
        }
        else
        {
            int corridorId = _tileData.GetCorridorIdAt(Vector2Int.FloorToInt(playerPos));
            EventCenter.Instance.Publish(GameEventID.OnCorridorEnter, new CorridorEnterParams
            {
                corridorId = corridorId
            });
        }
    }

    /// <summary>
    /// 每帧检测玩家位置
    /// </summary>
    public void CheckAndSpawnInRoom(Vector2 playerPos)
    {
        if (_tileData == null)
            return;

        int newRoomId = _tileData.GetRoomIdAt(Vector2Int.FloorToInt(playerPos));

        Debug.Log($"[RoomCtrl] CheckAndSpawnInRoom: playerPos={playerPos}, newRoomId={newRoomId}, _currentRoomId={_currentRoomId}");

        if (newRoomId == _currentRoomId)
            return;

        _currentRoomId = newRoomId;

        if (newRoomId < 0)
        {
            // 玩家进入走廊，发布走廊事件
            int corridorId = _tileData.GetCorridorIdAt(Vector2Int.FloorToInt(playerPos));
            EventCenter.Instance.Publish(GameEventID.OnCorridorEnter, new CorridorEnterParams
            {
                corridorId = corridorId
            });
            return;
        }

        // 每次进入房间都发布事件（供UI刷新）
        Room room = _tileData.GetRoom(newRoomId);
        if (room != null)
        {
            EventCenter.Instance.Publish(GameEventID.OnRoomEnter, new RoomEnterParams
            {
                roomId = room.id,
                roomType = room.roomType
            });
        }

        // 仅未访问的房间需要执行行为逻辑
        if (!IsRoomUnvisited(newRoomId))
        {
            Debug.Log($"[RoomCtrl] Room {newRoomId} already visited, skipping");
            return;
        }

        if (room == null)
            return;

        Debug.Log($"[RoomCtrl] Visiting new room {newRoomId}");
        VisitRoom(room, playerPos);
    }

    private bool IsRoomUnvisited(int roomId)
    {
        return !_roomStates.TryGetValue(roomId, out var state) || state == RoomState.Unvisited;
    }

    private void VisitRoom(Room room, Vector2 playerPos)
    {
        Debug.Log($"[RoomCtrl] VisitRoom: roomId={room.id}, type={room.roomType}");

        switch (room.roomType)
        {
            case RoomType.Start:
            case RoomType.Goal:
                Debug.Log($"[RoomCtrl] Start/Goal room - marking cleared");
                MarkRoomCleared(room.id);
                break;

            case RoomType.Normal:
            case RoomType.Elite:
            case RoomType.Boss:
                Debug.Log($"[RoomCtrl] Combat room - closing doors");
                _roomStates[room.id] = RoomState.InProgress;
                CloseDoors(room.id);  // 关门
                ExecuteRoomBehaviours(room, playerPos);
                break;

            default:
                Debug.Log($"[RoomCtrl] Unknown room type - marking cleared");
                MarkRoomCleared(room.id);
                break;
        }
    }

    private void ExecuteRoomBehaviours(Room room, Vector2 playerPos)
    {
        if (_behaviourTable == null)
        {
            MarkRoomCleared(room.id);
            return;
        }

        var entries = _behaviourTable.GetEntriesByRoomType(room.roomType);
        if (entries == null || entries.Count == 0)
        {
            MarkRoomCleared(room.id);
            return;
        }

        // 记录行为状态
        var behaviourState = new RoomBehaviourState
        {
            entries = entries,
            pendingCount = entries.Count
        };
        _roomBehaviourStates[room.id] = behaviourState;

        // 执行每个行为
        foreach (var entry in entries)
        {
            entry.OnWaveComplete = () => OnBehaviourComplete(room.id);
            entry.Execute(room, _tileData, _rng, playerPos);
        }
    }

    private void OnBehaviourComplete(int roomId)
    {
        if (!_roomBehaviourStates.TryGetValue(roomId, out var behaviourState))
            return;

        behaviourState.pendingCount--;

        if (behaviourState.pendingCount <= 0)
        {
            // 所有行为完成
            CheckRoomClear(roomId);
        }
    }

    private void OnEnemyKilled(EnemyKilledParams param)
    {
        if (param.enemy == null)
            return;

        int roomId = param.enemy.RoomId;

        // 通知 sequential 模式的行为
        if (_roomBehaviourStates.TryGetValue(roomId, out var behaviourState))
        {
            foreach (var entry in behaviourState.entries)
            {
                if (entry is EnemyBehaviourEntry enemyEntry)
                {
                    enemyEntry.NotifyEnemyKilled(roomId);
                }
            }
        }

        CheckRoomClear(roomId);
    }

    private void CheckRoomClear(int roomId)
    {
        if (!_roomStates.TryGetValue(roomId, out var state) || state != RoomState.InProgress)
            return;

        // 检查是否还有待完成的行为
        if (_roomBehaviourStates.TryGetValue(roomId, out var behaviourState))
        {
            if (behaviourState.pendingCount > 0)
                return;

            // 检查所有行为是否完成
            foreach (var entry in behaviourState.entries)
            {
                if (!entry.IsComplete())
                    return;
            }
        }

        // 房间清理完成
        MarkRoomCleared(roomId);
    }

    private void MarkRoomCleared(int roomId)
    {
        _roomStates[roomId] = RoomState.Cleared;
        _roomBehaviourStates.Remove(roomId);
        OpenDoors(roomId);  // 开门
        EventCenter.Instance.Publish(GameEventID.OnRoomCleared, roomId);
    }

    /// <summary>
    /// 关闭指定房间的门（在 wallTilemap 上覆盖墙壁瓦片）
    /// </summary>
    private void CloseDoors(int roomId)
    {
        Debug.Log($"[RoomCtrl] CloseDoors: roomId={roomId}, wallTilemap={_wallTilemap != null}, tileInfo={_tileInfo != null}");

        if (_wallTilemap == null || _tileInfo == null)
        {
            Debug.LogWarning($"[RoomCtrl] CloseDoors failed: wallTilemap={_wallTilemap != null}, tileInfo={_tileInfo != null}");
            return;
        }

        var closedDoorTile = _tileInfo.GetTile(TileType.ClosedDoorTile);
        Debug.Log($"[RoomCtrl] ClosedDoorTile={closedDoorTile?.name ?? "null"}");
        if (closedDoorTile == null)
            return;

        if (!_tileData.TryGetRoomDoorTiles(roomId, out var doorTiles))
        {
            Debug.LogWarning($"[RoomCtrl] TryGetRoomDoorTiles failed for roomId={roomId}");
            return;
        }

        Debug.Log($"[RoomCtrl] Setting {doorTiles.Count} door tiles for roomId={roomId}");
        foreach (var pos in doorTiles)
        {
            _wallTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), closedDoorTile);
        }
    }

    /// <summary>
    /// 打开指定房间的门（擦除 wallTilemap 上的覆盖瓦片，露出下层 doorTilemap 的门）
    /// </summary>
    private void OpenDoors(int roomId)
    {
        if (_wallTilemap == null)
            return;

        if (!_tileData.TryGetRoomDoorTiles(roomId, out var doorTiles))
            return;

        foreach (var pos in doorTiles)
        {
            _wallTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
        }
    }

    /// <summary>
    /// 获取房间状态
    /// </summary>
    public RoomState GetRoomState(int roomId)
    {
        return _roomStates.TryGetValue(roomId, out var state) ? state : RoomState.Unvisited;
    }

    /// <summary>
    /// 获取所有房间状态（用于存档）
    /// </summary>
    public Dictionary<int, RoomState> GetAllRoomStates()
    {
        return new Dictionary<int, RoomState>(_roomStates);
    }

    /// <summary>
    /// 批量恢复房间状态（用于读档）
    /// </summary>
    public void RestoreRoomStates(Dictionary<int, RoomState> states)
    {
        if (states == null)
            return;

        foreach (var kvp in states)
        {
            _roomStates[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// 获取玩家当前所在房间ID
    /// </summary>
    public int GetCurrentRoomId() => _currentRoomId;

    /// <summary>
    /// 设置玩家当前所在房间ID（用于从检查点恢复）
    /// </summary>
    public void SetCurrentRoomId(int roomId)
    {
        _currentRoomId = roomId;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        EventCenter.Instance.Unsubscribe<EnemyKilledParams>(GameEventID.Combat_EnemyKilled, OnEnemyKilled);
    }
}
