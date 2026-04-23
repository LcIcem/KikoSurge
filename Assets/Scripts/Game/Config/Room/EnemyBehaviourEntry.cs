using System;
using System.Collections.Generic;
using System.Linq;
using LcIcemFramework;
using ProcGen.Core;
using ProcGen.Seed;
using UnityEngine;
using Game.Event;
using LcIcemFramework.Core;

/// <summary>
/// 敌人生成选项（单种敌人配置）
/// </summary>
[Serializable]
public class EnemyChoice
{
    public EnemyConfig enemyConfig;
    public int weight = 1;
}

/// <summary>
/// Boss出生位置模式（相对于房间）
/// </summary>
public enum BossSpawnPosition
{
    RoomCenter,    // 房间中心
    TopLeft,       // 左上角
    TopRight,      // 右上角
    BottomLeft,    // 左下角
    BottomRight,   // 右下角
    NearestEdge,   // 距玩家最近的边缘
    FarthestEdge,  // 距玩家最远的边缘
    Random         // 随机位置
}

/// <summary>
/// 敌人生成行为条目
/// </summary>
[Serializable]
public class EnemyBehaviourEntry : RoomBehaviourEntry
{
    [Header("敌人选项列表（按权重随机）")]
    public List<EnemyChoice> enemyChoices = new();

    [Header("生成数量（每波次）")]
    public int minCount = 1;
    public int maxCount = 3;

    [Header("生成位置（Normal/Elite房间）")]
    public int minSpawnDist = 3;  // 距离玩家最小生成距离（格）
    public int minEdgeDist = 1;   // 距离房间边缘最小距离（格），防止贴墙生成

    [Header("生成位置（Boss房间）")]
    public BossSpawnPosition bossSpawnPosition = BossSpawnPosition.RoomCenter;

    // 内部状态
    private int _currentWave;
    private int _totalWaves;
    private Room _room;
    private GameRandom _rng;
    private Vector2 _playerPos;
    private List<Vector2Int> _validTiles;
    private HashSet<Vector2Int> _floorTiles;

    // Sequential 模式用：追踪当前波次的存活敌人数
    private int _enemiesAliveInCurrentWave;
    // 异步模式用：追踪所有波次存活敌人数
    private int _totalEnemiesAlive;

    public override void Execute(Room room, DungeonTileData tileData, GameRandom rng, Vector2 playerPos)
    {
        _room = room;
        _rng = rng;
        _playerPos = playerPos;
        _currentWave = 0;
        _totalWaves = rng.Range(minWaves, maxWaves + 1);
        _totalEnemiesAlive = 0;
        _enemiesAliveInCurrentWave = 0;

        // 从 tileData 获取 floor tiles
        tileData.TryGetRoomFloorTiles(room.id, out _floorTiles);

        // 预计算有效生成位置
        CalculateValidTiles();

        // 发布行为开始
        EventCenter.Instance.Publish(GameEventID.OnBehaviourStart, (RoomBehaviourEntry)this);

        // 执行第一波
        SpawnCurrentWave();
    }

    private void CalculateValidTiles()
    {
        _validTiles = new List<Vector2Int>();
        RectInt bounds = _room.Bounds;
        Vector2Int playerTile = Vector2Int.FloorToInt(_playerPos);

        // Boss房间使用相对位置计算，不再基于玩家距离
        if (_room.roomType == RoomType.Boss)
        {
            CalculateBossSpawnTiles(bounds, playerTile);
            return;
        }

        // Normal/Elite房间：基于玩家距离的逻辑
        foreach (var tile in _floorTiles)
        {
            // 排除距离玩家太近的格子
            float dist = Vector2Int.Distance(tile, playerTile);
            if (dist < minSpawnDist)
                continue;

            // 排除距离房间边缘太近的格子
            if (tile.x - bounds.x < minEdgeDist ||
                tile.y - bounds.y < minEdgeDist ||
                bounds.x + bounds.width - tile.x - 1 < minEdgeDist ||
                bounds.y + bounds.height - tile.y - 1 < minEdgeDist)
                continue;

            _validTiles.Add(tile);
        }

        // 兜底：如果有效格子为空，不再退回全部 floor tiles
        // 而是放宽 minSpawnDist 条件重新计算
        if (_validTiles.Count == 0)
        {
            foreach (var tile in _floorTiles)
            {
                float dist = Vector2Int.Distance(tile, playerTile);
                // 只排除距离玩家太近的格子，保留边缘限制
                if (dist < 1)
                    continue;

                // 排除距离房间边缘太近的格子
                if (tile.x - bounds.x < minEdgeDist ||
                    tile.y - bounds.y < minEdgeDist ||
                    bounds.x + bounds.width - tile.x - 1 < minEdgeDist ||
                    bounds.y + bounds.height - tile.y - 1 < minEdgeDist)
                    continue;

                _validTiles.Add(tile);
            }
        }

        // 如果仍然为空（房间极小），取距离玩家最远的格子
        if (_validTiles.Count == 0)
        {
            float maxDist = 0f;
            Vector2Int bestTile = _floorTiles.Count > 0 ? _floorTiles.ElementAt(0) : playerTile;
            foreach (var tile in _floorTiles)
            {
                float dist = Vector2Int.Distance(tile, playerTile);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    bestTile = tile;
                }
            }
            _validTiles.Add(bestTile);
        }
    }

    /// <summary>
    /// 计算Boss房间的出生位置（基于房间相对位置）
    /// </summary>
    private void CalculateBossSpawnTiles(RectInt bounds, Vector2Int playerTile)
    {
        switch (bossSpawnPosition)
        {
            case BossSpawnPosition.RoomCenter:
                // 房间中心点
                Vector2Int center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
                FindNearestFloorTile(center);
                break;

            case BossSpawnPosition.TopLeft:
                FindNearestFloorTile(new Vector2Int(bounds.x, bounds.y + bounds.height - 1));
                break;

            case BossSpawnPosition.TopRight:
                FindNearestFloorTile(new Vector2Int(bounds.x + bounds.width - 1, bounds.y + bounds.height - 1));
                break;

            case BossSpawnPosition.BottomLeft:
                FindNearestFloorTile(new Vector2Int(bounds.x, bounds.y));
                break;

            case BossSpawnPosition.BottomRight:
                FindNearestFloorTile(new Vector2Int(bounds.x + bounds.width - 1, bounds.y));
                break;

            case BossSpawnPosition.NearestEdge:
                FindNearestEdgeTile(playerTile);
                break;

            case BossSpawnPosition.FarthestEdge:
                FindFarthestEdgeTile(playerTile);
                break;

            case BossSpawnPosition.Random:
                // 随机位置：从所有floor tiles中排除边缘后随机选
                foreach (var tile in _floorTiles)
                {
                    if (tile.x - bounds.x < minEdgeDist ||
                        tile.y - bounds.y < minEdgeDist ||
                        bounds.x + bounds.width - tile.x - 1 < minEdgeDist ||
                        bounds.y + bounds.height - tile.y - 1 < minEdgeDist)
                        continue;
                    _validTiles.Add(tile);
                }
                // 如果为空则取最远边缘
                if (_validTiles.Count == 0)
                    FindFarthestEdgeTile(playerTile);
                break;
        }
    }

    /// <summary>
    /// 找到距离目标点最近的floor tile
    /// </summary>
    private void FindNearestFloorTile(Vector2Int target)
    {
        float minDist = float.MaxValue;
        Vector2Int bestTile = _floorTiles.Count > 0 ? _floorTiles.ElementAt(0) : target;

        foreach (var tile in _floorTiles)
        {
            float dist = Vector2Int.Distance(tile, target);
            if (dist < minDist)
            {
                minDist = dist;
                bestTile = tile;
            }
        }
        _validTiles.Add(bestTile);
    }

    /// <summary>
    /// 找到距离玩家最近的边缘tile
    /// </summary>
    private void FindNearestEdgeTile(Vector2Int playerTile)
    {
        float minDist = float.MaxValue;
        Vector2Int bestTile = playerTile;

        foreach (var tile in _floorTiles)
        {
            // 检查是否在边缘
            if (tile.x - _room.Bounds.x < minEdgeDist ||
                tile.y - _room.Bounds.y < minEdgeDist ||
                _room.Bounds.x + _room.Bounds.width - tile.x - 1 < minEdgeDist ||
                _room.Bounds.y + _room.Bounds.height - tile.y - 1 < minEdgeDist)
            {
                float dist = Vector2Int.Distance(tile, playerTile);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestTile = tile;
                }
            }
        }

        // 如果没找到边缘tile，取最近的floor tile
        if (bestTile == playerTile)
            FindNearestFloorTile(playerTile);
        else
            _validTiles.Add(bestTile);
    }

    /// <summary>
    /// 找到距离玩家最远的边缘tile
    /// </summary>
    private void FindFarthestEdgeTile(Vector2Int playerTile)
    {
        float maxDist = 0f;
        Vector2Int bestTile = playerTile;

        foreach (var tile in _floorTiles)
        {
            // 检查是否在边缘
            if (tile.x - _room.Bounds.x < minEdgeDist ||
                tile.y - _room.Bounds.y < minEdgeDist ||
                _room.Bounds.x + _room.Bounds.width - tile.x - 1 < minEdgeDist ||
                _room.Bounds.y + _room.Bounds.height - tile.y - 1 < minEdgeDist)
            {
                float dist = Vector2Int.Distance(tile, playerTile);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    bestTile = tile;
                }
            }
        }

        // 如果没找到边缘tile，取最远的floor tile
        if (bestTile == playerTile)
        {
            foreach (var tile in _floorTiles)
            {
                float dist = Vector2Int.Distance(tile, playerTile);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    bestTile = tile;
                }
            }
        }
        _validTiles.Add(bestTile);
    }

    private EnemyConfig SelectEnemyByWeight()
    {
        if (enemyChoices == null || enemyChoices.Count == 0)
            return null;

        // 计算总权重
        int totalWeight = 0;
        foreach (var choice in enemyChoices)
        {
            if (choice.enemyConfig != null)
                totalWeight += choice.weight;
        }

        if (totalWeight <= 0)
            return null;

        // 随机选择
        int roll = _rng.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var choice in enemyChoices)
        {
            if (choice.enemyConfig == null)
                continue;
            cumulative += choice.weight;
            if (roll < cumulative)
                return choice.enemyConfig;
        }

        // 兜底返回最后一个
        return enemyChoices[enemyChoices.Count - 1].enemyConfig;
    }

    private void SpawnCurrentWave()
    {
        if (_currentWave >= _totalWaves)
        {
            // 所有波次生成完毕，发布行为结束
            EventCenter.Instance.Publish(GameEventID.OnBehaviourEnd, (RoomBehaviourEntry)this);
            OnWaveComplete?.Invoke();
            return;
        }

        // 每次生成前，用玩家当前位置重新计算有效格子
        CalculateValidTiles();

        // 生成当前波次敌人
        int count = _rng.Range(minCount, maxCount + 1);
        _enemiesAliveInCurrentWave = count;
        // 异步模式累加总数，顺序模式重置为当前波数量
        if (spawnMode == WaveSpawnMode.Async)
            _totalEnemiesAlive += count;
        else
            _totalEnemiesAlive = count;

        for (int i = 0; i < count; i++)
        {
            var tilePos = _validTiles[_rng.Range(0, _validTiles.Count)];
            Vector3 worldPos = new Vector3(tilePos.x, tilePos.y, 0);

            EnemyConfig selectedEnemy = SelectEnemyByWeight();
            if (selectedEnemy == null)
                continue;

            EnemyFactory.Instance.Create(selectedEnemy, worldPos, enemy =>
            {
                enemy.RoomId = _room.id;
            });
        }

        _currentWave++;

        // 发布波次开始
        EventCenter.Instance.Publish(GameEventID.OnWaveStarted,
            new WaveStartParams { behaviourName = behaviourName, currentWave = _currentWave, totalWaves = _totalWaves, enemiesInWave = _totalEnemiesAlive });

        if (spawnMode == WaveSpawnMode.Sequential)
        {
            // 队列式：等待敌人死亡后再生成下一波
            // 敌人死亡通过 NotifyEnemyKilled 通知
        }
        else
        {
            // 异步式：延迟后直接生成下一波
            if (_currentWave < _totalWaves)
            {
                ManagerHub.Timer.AddTimeOut(waveDelay, () => SpawnCurrentWave());
            }
            // 注意：异步模式下 OnBehaviourEnd 在 NotifyEnemyKilled 中发布（所有敌人都死亡时）
        }
    }

    /// <summary>
    /// 通知敌人死亡（由 RoomController 调用）
    /// </summary>
    public void NotifyEnemyKilled(int enemyRoomId)
    {
        // 验证敌人是否属于当前行为实例的房间
        if (enemyRoomId != _room.id)
            return;

        _enemiesAliveInCurrentWave--;
        _totalEnemiesAlive--;

        // 发布波次更新（实时更新剩余敌人数）
        // 顺序模式用当前波剩余数，异步模式用所有波剩余总数
        int remaining = spawnMode == WaveSpawnMode.Sequential ? _enemiesAliveInCurrentWave : _totalEnemiesAlive;
        EventCenter.Instance.Publish(GameEventID.OnWaveUpdate,
            new WaveUpdateParams { behaviourName = behaviourName, currentWave = _currentWave, totalWaves = _totalWaves, remainingEnemies = remaining });

        if (spawnMode == WaveSpawnMode.Sequential)
        {
            if (_enemiesAliveInCurrentWave <= 0)
            {
                // 发布波次清理完成
                EventCenter.Instance.Publish(GameEventID.OnWaveCleared,
                    new WaveClearedParams { waveNum = _currentWave });
            }

            if (_enemiesAliveInCurrentWave <= 0 && _currentWave < _totalWaves)
            {
                // 当前波次敌人全死，延迟后生成下一波
                ManagerHub.Timer.AddTimeOut(waveDelay, () => SpawnCurrentWave());
            }
            else if (_enemiesAliveInCurrentWave <= 0 && _currentWave >= _totalWaves)
            {
                // 所有波次完成
                EventCenter.Instance.Publish(GameEventID.OnBehaviourEnd, (RoomBehaviourEntry)this);
                OnWaveComplete?.Invoke();
            }
        }
        else
        {
            // 异步模式：检查是否所有敌人都死亡且所有波次已生成
            if (_totalEnemiesAlive <= 0 && _currentWave >= _totalWaves)
            {
                EventCenter.Instance.Publish(GameEventID.OnBehaviourEnd, (RoomBehaviourEntry)this);
                OnWaveComplete?.Invoke();
            }
        }
    }

    public override bool IsComplete()
    {
        // Sequential 模式：所有波次完成且当前波次敌人都死亡
        // Async 模式：所有波次生成完毕且所有敌人都死亡
        if (spawnMode == WaveSpawnMode.Sequential)
        {
            return _currentWave >= _totalWaves && _enemiesAliveInCurrentWave <= 0;
        }
        return _currentWave >= _totalWaves && _totalEnemiesAlive <= 0;
    }
}
