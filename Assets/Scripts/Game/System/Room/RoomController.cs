using System.Collections.Generic;
using Game.Event;
using LcIcemFramework.Core;
using ProcGen.Core;
using ProcGen.Seed;
using UnityEngine;

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
public class RoomBehaviorState
{
    public List<RoomBehaviorEntry> entries;  // 行为条目列表
    public int pendingCount;                 // 待完成的行为数
}

/// <summary>
/// 房间行为管理器
/// <para>负责检测玩家进入房间、调用房间行为、判断房间完成</para>
/// </summary>
public class RoomController
{
    private DungeonTileData _tileData;
    private RoomBehaviorTable_SO _behaviorTable;
    private GameRandom _rng;
    private int _currentRoomId = -1;
    private readonly Dictionary<int, RoomState> _roomStates = new();
    private readonly Dictionary<int, RoomBehaviorState> _roomBehaviorStates = new();

    public RoomController()
    {
        EventCenter.Instance.Subscribe<EnemyKilledParams>(GameEventID.Combat_EnemyKilled, OnEnemyKilled);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize(DungeonTileData tileData, RoomBehaviorTable_SO behaviorTable, GameRandom rng)
    {
        _tileData = tileData;
        _behaviorTable = behaviorTable;
        _rng = rng;
        _currentRoomId = -1;
        _roomStates.Clear();
        _roomBehaviorStates.Clear();
    }

    /// <summary>
    /// 每帧检测玩家位置
    /// </summary>
    public void CheckAndSpawnInRoom(Vector2 playerPos)
    {
        if (_tileData == null)
            return;

        int newRoomId = _tileData.GetRoomIdAt(Vector2Int.FloorToInt(playerPos));

        if (newRoomId == _currentRoomId)
            return;

        _currentRoomId = newRoomId;

        if (newRoomId < 0)
            return;

        if (!IsRoomUnvisited(newRoomId))
            return;

        Room room = _tileData.GetRoom(newRoomId);
        if (room == null)
            return;

        VisitRoom(room, playerPos);
    }

    private bool IsRoomUnvisited(int roomId)
    {
        return !_roomStates.TryGetValue(roomId, out var state) || state == RoomState.Unvisited;
    }

    private void VisitRoom(Room room, Vector2 playerPos)
    {
        switch (room.roomType)
        {
            case RoomType.Start:
            case RoomType.Goal:
                MarkRoomCleared(room.id);
                break;

            case RoomType.Normal:
            case RoomType.Elite:
            case RoomType.Boss:
                _roomStates[room.id] = RoomState.InProgress;
                ExecuteRoomBehaviors(room, playerPos);
                break;

            default:
                MarkRoomCleared(room.id);
                break;
        }
    }

    private void ExecuteRoomBehaviors(Room room, Vector2 playerPos)
    {
        if (_behaviorTable == null)
        {
            MarkRoomCleared(room.id);
            return;
        }

        var entries = _behaviorTable.GetEntriesByRoomType(room.roomType);
        if (entries == null || entries.Count == 0)
        {
            MarkRoomCleared(room.id);
            return;
        }

        // 记录行为状态
        var behaviorState = new RoomBehaviorState
        {
            entries = entries,
            pendingCount = entries.Count
        };
        _roomBehaviorStates[room.id] = behaviorState;

        // 执行每个行为
        foreach (var entry in entries)
        {
            entry.OnWaveComplete = () => OnBehaviorComplete(room.id);
            entry.Execute(room, _tileData, _rng, playerPos);
        }
    }

    private void OnBehaviorComplete(int roomId)
    {
        if (!_roomBehaviorStates.TryGetValue(roomId, out var behaviorState))
            return;

        behaviorState.pendingCount--;

        if (behaviorState.pendingCount <= 0)
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
        if (_roomBehaviorStates.TryGetValue(roomId, out var behaviorState))
        {
            foreach (var entry in behaviorState.entries)
            {
                if (entry is EnemyBehaviorEntry enemyEntry)
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
        if (_roomBehaviorStates.TryGetValue(roomId, out var behaviorState))
        {
            if (behaviorState.pendingCount > 0)
                return;

            // 检查所有行为是否完成
            foreach (var entry in behaviorState.entries)
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
        _roomBehaviorStates.Remove(roomId);
        EventCenter.Instance.Publish(GameEventID.OnRoomCleared, roomId);
    }

    /// <summary>
    /// 获取房间状态
    /// </summary>
    public RoomState GetRoomState(int roomId)
    {
        return _roomStates.TryGetValue(roomId, out var state) ? state : RoomState.Unvisited;
    }

    /// <summary>
    /// 获取玩家当前所在房间ID
    /// </summary>
    public int GetCurrentRoomId() => _currentRoomId;

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        EventCenter.Instance.Unsubscribe<EnemyKilledParams>(GameEventID.Combat_EnemyKilled, OnEnemyKilled);
    }
}
