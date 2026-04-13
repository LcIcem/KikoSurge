using System.Collections.Generic;
using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using UnityEngine;

/// <summary>
/// 敌人工厂：创建敌人实例 + 对象池管理。
/// </summary>
public class EnemyFactory : SingletonMono<EnemyFactory>
{

    // 对象池
    private readonly Dictionary<EnemyType, Queue<EnemyBase>> _pools = new();
    private readonly Dictionary<EnemyType, EnemyBase> _prefabs = new();

    protected override void Init()
    {
        // 懒初始化，当前不需要额外逻辑
    }

    /// 注册敌人 Prefab
    public void Register(EnemyType type, EnemyBase prefab)
    {
        _prefabs[type] = prefab;
        if (!_pools.ContainsKey(type))
            _pools[type] = new Queue<EnemyBase>();
    }

    /// 创建敌人（从对象池）
    public EnemyBase Create(EnemySpawnParams p)
    {
        EnemyBase enemy = ManagerHub.Pool.Get<EnemyBase>(p.PrefabName, p.Position, Quaternion.identity);

        EventCenter.Instance.Publish(EventID.Combat_EnemySpawned,
            new EnemySpawnedParams { enemy = enemy, type = p.Type });

        return enemy;
    }

    /// 回收敌人（回对象池）
    public void Release(EnemyBase enemy)
    {
        if (enemy == null) return;

        ManagerHub.Pool.Release(enemy.gameObject);
    }
}