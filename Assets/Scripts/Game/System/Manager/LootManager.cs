using System.Collections.Generic;
using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework;
using Game.Event;

/// <summary>
/// 掉落物管理器：处理敌人死亡时的物品掉落逻辑
/// </summary>
public class LootManager : SingletonMono<LootManager>
{
    // 玩家引用（用于武器拾取）
    private Player _player;

    // 追踪所有活跃掉落物（用于切换层时清理）
    private readonly HashSet<LootItem> _activeLootItems = new();

    protected override void Init()
    {
        // 懒初始化
    }

    private void Start()
    {
        // 获取玩家引用
        _player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();

        // 订阅敌人死亡事件
        EventCenter.Instance.Subscribe<EnemyKilledParams>(GameEventID.Combat_EnemyKilled, OnEnemyKilled);
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
        var lootTable = GameDataManager.Instance.GetLootTable(enemy.EnemyId);
        if (lootTable == null)
        {
            Log($"无掉落表配置: EnemyId={enemy.EnemyId}");
            return;
        }

        // 遍历掉落条目
        foreach (var entry in lootTable.Entries)
        {
            if (entry == null || entry.lootItemPrefab == null) continue;

            // 计算散落位置
            Vector2 spawnPos = deathPosition + Random.insideUnitCircle * lootTable.DropSpreadRadius;

            // 生成掉落物（内部处理概率抽取和数量计算）
            var lootItem = ItemFactory.Instance.CreateLootItem(entry, spawnPos);
            if (lootItem != null)
            {
                _activeLootItems.Add(lootItem);
                Log($"掉落物品: {lootItem.ItemDef?.ItemName ?? "Unknown"} x{lootItem.Quantity} at {spawnPos}");
            }
        }
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
    /// 判断装备栏是否已满，未满则装备，已满则放入背包
    /// </summary>
    public void CreateWeaponForPlayer(GunConfig config)
    {
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
        if (_player == null)
        {
            LogError("武器创建失败: player 为 null");
            return;
        }

        WeaponFactory.Instance.Create(config, _player.WeaponPivot, (weapon) =>
        {
            if (weapon == null)
            {
                LogError($"[LootManager] 武器创建失败: {config?.gunName ?? "null"}");
                return;
            }

            // 获取当前已装备武器列表和最大数量
            var equippedWeaponIds = SessionManager.Instance.GetEquippedWeaponIds();
            var roleData = GameDataManager.Instance?.GetRoleStaticData(SessionManager.Instance.CurrentSession?.selectedRoleId ?? 0);
            int maxSlots = roleData?.maxWeaponSlots ?? 2;

            if (equippedWeaponIds.Count < maxSlots)
            {
                // 装备栏未满，装备武器
                _player.weaponHandler.AddWeapon(weapon);
                equippedWeaponIds.Add(config.Id);
                SessionManager.Instance.SetEquippedWeaponIds(equippedWeaponIds);
                Log($"武器装备到玩家: {config.gunName} (已装备 {equippedWeaponIds.Count}/{maxSlots})");
            }
            else
            {
                // 装备栏已满，放入背包
                var inventoryWeaponIds = SessionManager.Instance.GetInventoryWeaponIds();
                inventoryWeaponIds.Add(config.Id);
                SessionManager.Instance.SetInventoryWeaponIds(inventoryWeaponIds);
                WeaponFactory.Instance.Release(weapon);
                Log($"武器放入背包: {config.gunName} (背包 {inventoryWeaponIds.Count} 把)");
            }
        });
    }

    // 日志
    private void Log(string msg) => Debug.Log($"[LootManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[LootManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[LootManager] {msg}");
}
