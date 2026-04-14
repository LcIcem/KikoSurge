using System.Collections.Generic;
using System.Linq;
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
    Unvisited,  // 未访问
    InProgress,  // 进行中（敌人存活）
    Cleared     // 已清理（敌人全死亡）
}

/// <summary>
/// 房间行为管理器
/// <para>负责检测玩家进入房间、触发房间行为（生成敌人/宝藏等）、判断房间完成</para>
/// </summary>
public class RoomController
{
    private DungeonTileData _tileData;
    private RoomBehaviorTable_SO _behaviorTable;
    private GameRandom _rng;
    private int _currentRoomId = -1;
    private readonly Dictionary<int, RoomState> _roomStates = new();
    private readonly Dictionary<int, List<EnemyBase>> _roomEnemies = new();

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
        _roomEnemies.Clear();

        EventCenter.Instance.Subscribe<EnemyKilledParams>(EventID.Combat_EnemyKilled, OnEnemyKilled);
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

        ProcGen.Core.Room room = _tileData.GetRoom(newRoomId);
        if (room == null)
            return;

        VisitRoom(room);
    }

    private bool IsRoomUnvisited(int roomId)
    {
        return !_roomStates.TryGetValue(roomId, out var state) || state == RoomState.Unvisited;
    }

    private void VisitRoom(ProcGen.Core.Room room)
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
                SpawnEnemiesForRoom(room);
                break;

            default:
                MarkRoomCleared(room.id);
                break;
        }
    }

    private void SpawnEnemiesForRoom(ProcGen.Core.Room room)
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

        if (!_tileData.TryGetRoomFloorTiles(room.id, out var floorTiles) || floorTiles.Count == 0)
            return;

        var enemiesInRoom = new List<EnemyBase>();

        // 计算总权重（归一化用）
        int totalWeight = 0;
        foreach (var entry in entries)
        {
            if (entry.enemyDef != null)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
        {
            MarkRoomCleared(room.id);
            return;
        }

        foreach (var entry in entries)
        {
            if (entry.enemyDef == null)
                continue;

            // 归一化权重选择
            if (_rng.Range(0, totalWeight) >= entry.weight)
            {
                totalWeight -= entry.weight;
                continue;
            }
            totalWeight -= entry.weight;

            int count = _rng.Range(entry.minCount, entry.maxCount + 1);

            for (int i = 0; i < count; i++)
            {
                Vector2Int tilePos = floorTiles.ElementAt(_rng.Range(0, floorTiles.Count));
                Vector3 worldPos = new Vector3(tilePos.x, tilePos.y, 0);

                EnemyFactory.Instance.Create(entry.enemyDef, worldPos, enemy =>
                {
                    enemy.RoomId = room.id;
                    enemiesInRoom.Add(enemy);
                });
            }
        }

        _roomEnemies[room.id] = enemiesInRoom;

        EventCenter.Instance.Publish(EventID.Combat_WaveStart,
            new WaveStartParams { waveNum = 1, totalEnemies = enemiesInRoom.Count });
    }

    private void OnEnemyKilled(EnemyKilledParams param)
    {
        if (param.enemy != null)
        {
            CheckRoomClear(param.enemy.RoomId);
        }
    }

    private void CheckRoomClear(int roomId)
    {
        if (!_roomStates.TryGetValue(roomId, out var state) || state != RoomState.InProgress)
            return;

        if (_roomEnemies.TryGetValue(roomId, out var enemies))
            enemies.RemoveAll(e => e == null);

        if (enemies == null || enemies.Count == 0)
            MarkRoomCleared(roomId);
    }

    private void MarkRoomCleared(int roomId)
    {
        _roomStates[roomId] = RoomState.Cleared;
        _roomEnemies.Remove(roomId);
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
        EventCenter.Instance.Unsubscribe<EnemyKilledParams>(EventID.Combat_EnemyKilled, OnEnemyKilled);
    }
}
