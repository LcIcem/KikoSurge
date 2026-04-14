using System;
using ProcGen.Core;
using ProcGen.Seed;
using UnityEngine;

/// <summary>
/// 波次生成模式
/// </summary>
public enum WaveSpawnMode
{
    Sequential,  // 队列式：必须等待上一波结束才生成下一波
    Async       // 异步式：每波独立生成，不依赖上一波
}

/// <summary>
/// 房间行为条目基类
/// <para>通过继承扩展不同类型的行为（敌人生成、宝藏、事件等）</para>
/// </summary>
[Serializable]
public abstract class RoomBehaviorEntry
{
    [Header("波次配置")]
    public int minWaves = 1;
    public int maxWaves = 1;
    public float waveDelay = 2f;  // 波次间隔（秒）

    [Header("生成模式")]
    public WaveSpawnMode spawnMode = WaveSpawnMode.Async;

    /// <summary>
    /// 执行行为（由 RoomController 调用）
    /// </summary>
    public abstract void Execute(Room room, DungeonTileData tileData, GameRandom rng, Vector2 playerPos);

    /// <summary>
    /// 是否完成所有波次
    /// </summary>
    public abstract bool IsComplete();

    /// <summary>
    /// 波次完成回调（由实现类在适当时候调用）
    /// </summary>
    public Action OnWaveComplete { get; set; }
}
