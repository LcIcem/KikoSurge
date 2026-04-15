using System.Collections.Generic;
using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.Pool;
using Game.Util.Const;
using Game.Event;

/// <summary>
/// 掉落物管理器：处理敌人死亡时的物品掉落逻辑
/// </summary>
public class LootManager : SingletonMono<LootManager>
{
    // LootItem 预设体的 Addressables 地址（需要预先创建 LootItem 预设体并注册）
    private const string LOOT_ITEM_PREFAB_ADDRESS = GameConstants.LOOT_ITEM_PREFAB_ADDRESS;

    // 玩家引用（用于武器拾取）
    private Player _player;

    // 追踪所有活跃掉落物（用于切换层时清理）
    private readonly HashSet<LootItem> _activeLootItems = new();

    protected override void Init()
    {
        // 不在此处初始化，等待 Start() 确保所有 Manager 已完成 Awake
    }

    private void Start()
    {
        // 获取玩家引用
        _player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();

        // 订阅敌人死亡事件
        EventCenter.Instance.Subscribe<EnemyKilledParams>(GameEventID.Combat_EnemyKilled, OnEnemyKilled);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 退订事件
        EventCenter.Instance.Unsubscribe<EnemyKilledParams>(GameEventID.Combat_EnemyKilled, OnEnemyKilled);
    }

    /// <summary>
    /// 敌人死亡事件处理
    /// </summary>
    private void OnEnemyKilled(EnemyKilledParams p)
    {
        ProcessLootDrop(p.enemy, p.position);
    }

    /// <summary>
    /// 处理掉落逻辑
    /// </summary>
    private void ProcessLootDrop(EnemyBase enemy, Vector2 deathPosition)
    {
        if (enemy == null) return;

        // 从 GameDataManager 获取掉落表
        var lootTable = GameDataManager.Instance.GetLootTable(enemy.Type);
        if (lootTable == null)
        {
            Log($"无掉落表配置: EnemyType={enemy.Type}");
            return;
        }

        // 遍历掉落条目
        foreach (var entry in lootTable.Entries)
        {
            if (entry.ItemDef == null) continue;

            // 概率抽取
            if (Random.value <= entry.DropChance)
            {
                // 计算掉落数量
                int quantity = Random.Range(entry.MinQuantity, entry.MaxQuantity + 1);

                // 散落位置
                Vector2 spawnPos = deathPosition + Random.insideUnitCircle * lootTable.DropSpreadRadius;

                // 生成掉落物
                SpawnLootItem(entry.ItemDef, quantity, spawnPos);
            }
        }
    }

    /// <summary>
    /// 生成掉落物
    /// </summary>
    private void SpawnLootItem(LootItemDefBase itemDef, int quantity, Vector2 position)
    {
        // 从对象池获取 LootItem
        var lootItem = ManagerHub.Pool.Get<LootItem>(LOOT_ITEM_PREFAB_ADDRESS, position, Quaternion.identity);

        if (lootItem == null)
        {
            LogError($"LootItem 获取失败，请确认预设体已注册: {LOOT_ITEM_PREFAB_ADDRESS}");
            return;
        }

        lootItem.Initialize(itemDef, quantity);
        _activeLootItems.Add(lootItem);
        Log($"掉落物品: {itemDef.ItemName} x{quantity} at {position}");
    }

    /// <summary>
    /// 清理所有掉落物（切换层时调用）
    /// </summary>
    public void ClearAll()
    {
        foreach (var lootItem in _activeLootItems)
        {
            if (lootItem != null)
                ManagerHub.Pool.Release(lootItem.gameObject);
        }
        _activeLootItems.Clear();
    }

    /// <summary>
    /// 为玩家创建武器（从掉落物拾取时调用）
    /// </summary>
    public void CreateWeaponForPlayer(GunConfig config)
    {
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
        if (config == null || _player == null)
        {
            LogError("武器创建失败: config 或 player 为 null");
            return;
        }

        if (config.gunPrefab == null)
        {
            LogError($"[LootManager] 武器配置 {config.gunName} 没有指定预设体");
            return;
        }

        // 实例化武器预设体
        var weaponObj = Object.Instantiate(config.gunPrefab, _player.transform);
        weaponObj.SetActive(false);

        var weapon = weaponObj.GetComponent<WeaponBase>();
        if (weapon == null)
        {
            LogError($"[LootManager] 武器预设体 {config.gunName} 上没有 WeaponBase 组件");
            return;
        }

        weapon.Init(config);
        _player.weaponHandler.AddWeapon(weapon);
        Log($"武器添加到玩家: {config.gunName}");
    }

    // 日志
    private void Log(string msg) => Debug.Log($"[LootManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[LootManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[LootManager] {msg}");
}
