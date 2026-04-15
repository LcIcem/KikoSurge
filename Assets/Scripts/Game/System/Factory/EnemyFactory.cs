using System.Collections.Generic;
using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using UnityEngine;
using UnityEngine.Events;
using Game.Event;

/// <summary>
/// 敌人工厂：根据配置创建敌人实例，支持 Addressables 预设体加载。
/// </summary>
public class EnemyFactory : SingletonMono<EnemyFactory>
{
    // 对象池（通过 PoolManager 管理，这里只做统计）
    private readonly Dictionary<EnemyType, int> _spawnedCounts = new();

    // 追踪所有活跃敌人（用于切换层时清理）
    private readonly HashSet<EnemyBase> _activeEnemies = new();

    protected override void Init()
    {
        // 懒初始化
    }

    /// <summary>
    /// 根据配置创建敌人（从对象池获取）
    /// </summary>
    /// <param name="config">敌人配置 SO</param>
    /// <param name="position">生成位置</param>
    /// <param name="onCreated">创建完成回调</param>
    public void Create(EnemyDefBase config, Vector3 position, UnityAction<EnemyBase> onCreated)
    {
        if (config == null)
        {
            Debug.LogError("[EnemyFactory] 敌人配置为 null");
            onCreated?.Invoke(null);
            return;
        }

        // 从 PoolManager 获取实例（自动懒注册并从 Addressables 加载预设体）
        EnemyBase enemy = ManagerHub.Pool.Get<EnemyBase>(config.PrefabAddress, position, Quaternion.identity);

        if (enemy == null)
        {
            Debug.LogError($"[EnemyFactory] 从池获取敌人失败: {config.PrefabAddress}");
            onCreated?.Invoke(null);
            return;
        }

        // 使用配置初始化敌人
        enemy.Init(config);

        // 追踪活跃敌人
        _activeEnemies.Add(enemy);

        // 统计
        if (_spawnedCounts.ContainsKey(config.Type))
            _spawnedCounts[config.Type]++;
        else
            _spawnedCounts[config.Type] = 1;

        EventCenter.Instance.Publish(GameEventID.Combat_EnemySpawned,
            new EnemySpawnedParams { enemy = enemy, type = config.Type });

        onCreated?.Invoke(enemy);
    }

    /// <summary>
    /// 回收敌人（回对象池）
    /// </summary>
    public void Release(EnemyBase enemy)
    {
        if (enemy == null) return;

        // 移除追踪
        _activeEnemies.Remove(enemy);

        // 统计
        EnemyDefBase config = GetEnemyConfigByEnemy(enemy);
        if (config != null && _spawnedCounts.ContainsKey(config.Type))
        {
            _spawnedCounts[config.Type]--;
            if (_spawnedCounts[config.Type] <= 0)
                _spawnedCounts.Remove(config.Type);
        }

        ManagerHub.Pool.Release(enemy.gameObject);
    }

    /// <summary>
    /// 释放所有活跃敌人（切换层时调用）
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var enemy in _activeEnemies)
        {
            if (enemy != null)
                enemy.ReleaseImmediately();
        }
        _activeEnemies.Clear();
        _spawnedCounts.Clear();
    }

    /// <summary>
    /// 通过敌人实例反查其配置（临时方案，后续优化）
    /// </summary>
    private EnemyDefBase GetEnemyConfigByEnemy(EnemyBase enemy)
    {
        // 简单实现：根据 HP 等属性匹配（后续可优化为直接存储引用）
        return null;
    }

    /// <summary>
    /// 获取已生成敌人数量
    /// </summary>
    public int GetSpawnedCount(EnemyType type)
    {
        return _spawnedCounts.TryGetValue(type, out var count) ? count : 0;
    }
}
