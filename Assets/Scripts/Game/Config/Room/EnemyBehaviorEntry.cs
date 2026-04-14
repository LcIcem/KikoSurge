using System;
using System.Collections.Generic;
using LcIcemFramework.Managers;
using ProcGen.Core;
using ProcGen.Seed;
using UnityEngine;

/// <summary>
/// 敌人生成选项（单种敌人配置）
/// </summary>
[Serializable]
public class EnemyChoice
{
    public EnemyDefBase enemyDef;
    public int weight = 1;
}

/// <summary>
/// 敌人生成行为条目
/// </summary>
[Serializable]
public class EnemyBehaviorEntry : RoomBehaviorEntry
{
    [Header("敌人选项列表（按权重随机）")]
    public List<EnemyChoice> enemyChoices = new();

    [Header("生成数量（每波次）")]
    public int minCount = 1;
    public int maxCount = 3;

    [Header("生成位置")]
    public int minSpawnDist = 3;  // 距离玩家最小生成距离（格）

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

    public override void Execute(Room room, DungeonTileData tileData, GameRandom rng, Vector2 playerPos)
    {
        _room = room;
        _rng = rng;
        _playerPos = playerPos;
        _currentWave = 0;
        _totalWaves = rng.Range(minWaves, maxWaves + 1);

        // 从 tileData 获取 floor tiles
        tileData.TryGetRoomFloorTiles(room.id, out _floorTiles);

        // 预计算有效生成位置
        CalculateValidTiles();

        // 执行第一波
        SpawnCurrentWave();
    }

    private void CalculateValidTiles()
    {
        _validTiles = new List<Vector2Int>();
        foreach (var tile in _floorTiles)
        {
            float dist = Vector2Int.Distance(tile, Vector2Int.FloorToInt(_playerPos));
            if (dist >= minSpawnDist)
                _validTiles.Add(tile);
        }

        if (_validTiles.Count == 0)
            _validTiles.AddRange(_floorTiles);
    }

    private EnemyDefBase SelectEnemyByWeight()
    {
        if (enemyChoices == null || enemyChoices.Count == 0)
            return null;

        // 计算总权重
        int totalWeight = 0;
        foreach (var choice in enemyChoices)
        {
            if (choice.enemyDef != null)
                totalWeight += choice.weight;
        }

        if (totalWeight <= 0)
            return null;

        // 随机选择
        int roll = _rng.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var choice in enemyChoices)
        {
            if (choice.enemyDef == null)
                continue;
            cumulative += choice.weight;
            if (roll < cumulative)
                return choice.enemyDef;
        }

        // 兜底返回最后一个
        return enemyChoices[enemyChoices.Count - 1].enemyDef;
    }

    private void SpawnCurrentWave()
    {
        if (_currentWave >= _totalWaves)
        {
            // 所有波次生成完毕
            OnWaveComplete?.Invoke();
            return;
        }

        // 生成当前波次敌人
        int count = _rng.Range(minCount, maxCount + 1);
        _enemiesAliveInCurrentWave = count;

        for (int i = 0; i < count; i++)
        {
            var tilePos = _validTiles[_rng.Range(0, _validTiles.Count)];
            Vector3 worldPos = new Vector3(tilePos.x, tilePos.y, 0);

            EnemyDefBase selectedEnemy = SelectEnemyByWeight();
            if (selectedEnemy == null)
                continue;

            EnemyFactory.Instance.Create(selectedEnemy, worldPos, enemy =>
            {
                enemy.RoomId = _room.id;
            });
        }

        _currentWave++;

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
            else
            {
                // 所有波次生成完毕
                OnWaveComplete?.Invoke();
            }
        }
    }

    /// <summary>
    /// 通知敌人死亡（由 RoomController 调用，仅 Sequential 模式使用）
    /// </summary>
    public void NotifyEnemyKilled()
    {
        if (spawnMode != WaveSpawnMode.Sequential)
            return;

        _enemiesAliveInCurrentWave--;

        if (_enemiesAliveInCurrentWave <= 0 && _currentWave < _totalWaves)
        {
            // 当前波次敌人全死，延迟后生成下一波
            ManagerHub.Timer.AddTimeOut(waveDelay, () => SpawnCurrentWave());
        }
        else if (_enemiesAliveInCurrentWave <= 0 && _currentWave >= _totalWaves)
        {
            // 所有波次完成
            OnWaveComplete?.Invoke();
        }
    }

    public override bool IsComplete()
    {
        // Sequential 模式：所有波次完成且当前波次敌人都死亡
        // Async 模式：所有波次生成完毕即可
        if (spawnMode == WaveSpawnMode.Sequential)
        {
            return _currentWave >= _totalWaves && _enemiesAliveInCurrentWave <= 0;
        }
        return _currentWave >= _totalWaves;
    }
}
