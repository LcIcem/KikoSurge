using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework;
using Game.Event;

/// <summary>
/// 掉落物管理器：处理敌人死亡时的物品掉落逻辑
/// </summary>
public class LootManager : SingletonMono<LootManager>
{
    // 追踪所有活跃掉落物（用于切换层时清理）
    private readonly HashSet<LootItem> _activeLootItems = new();

    protected override void Init()
    {
        Debug.Log("[LootManager] Init() called - subscribing to events");
        // 订阅敌人死亡事件（放在 Init 中确保在所有 Start 之前完成订阅）
        EventCenter.Instance.Subscribe<EnemyKilledParams>(GameEventID.Combat_EnemyKilled, OnEnemyKilled);
    }

    /// <summary>
    /// 敌人死亡事件处理
    /// </summary>
    private void OnEnemyKilled(EnemyKilledParams p)
    {
        Log($"[OnEnemyKilled] 收到敌人死亡事件: enemy={p.enemy?.name ?? "null"}, position={p.position}");

        // 防御性检查
        if (p.enemy == null)
        {
            LogWarning("EnemyKilledParams.enemy is null");
            return;
        }
        if (p.enemy.EnemyConfig == null)
        {
            LogWarning($"EnemyConfig is null for enemy: {p.enemy.EnemyId}");
            return;
        }

        ProcessLootDrop(p.enemy, p.position);
    }

    /// <summary>
    /// 处理掉落逻辑（分组掉落）
    /// </summary>
    private void ProcessLootDrop(EnemyBase enemy, Vector2 deathPosition)
    {
        if (enemy == null) return;

        // 直接从 EnemyConfig 获取掉落表
        var config = enemy.EnemyConfig;
        Log($"[DEBUG] EnemyId={enemy.EnemyId}, EnemyConfig={config}, lootTable={(config != null ? config.lootTable : "N/A")}");

        var lootTable = config?.lootTable;
        if (lootTable == null)
        {
            LogWarning($"无掉落表配置: EnemyId={enemy.EnemyId}, EnemyConfig={config}");
            return;
        }

        if (lootTable.groups == null || lootTable.groups.Count == 0)
        {
            LogWarning($"掉落表 groups 为空: EnemyId={enemy.EnemyId}");
            return;
        }

        // 遍历每个分组
        foreach (var group in lootTable.groups)
        {
            if (group == null || group.entries == null || group.entries.Count == 0)
            {
                LogWarning($"掉落组为空或无效: groupName={group?.groupName}");
                continue;
            }

            // 过滤出有效条目
            var validEntries = group.entries.Where(e => e != null && e.itemConfig != null).ToList();
            if (validEntries.Count == 0) continue;

            // 有放回抽取：从有效条目中随机选，每次都独立判定概率
            for (int i = 0; i < group.maxPick; i++)
            {
                // 随机选一个条目
                var entry = validEntries[Random.Range(0, validEntries.Count)];

                // 独立判定该次抽取的命中概率
                if (Random.value > entry.dropChance) continue;

                // 计算数量
                int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);

                // 计算散落位置
                Vector2 spawnPos = deathPosition + Random.insideUnitCircle * lootTable.DropSpreadRadius;

                // 生成掉落物
                Log($"生成掉落物: {entry.itemConfig.Name} x{quantity} at {spawnPos}");
                var lootItem = ItemFactory.Instance.CreateLootItem(entry.itemConfig, quantity, spawnPos);
                if (lootItem != null)
                {
                    _activeLootItems.Add(lootItem);
                    Log($"掉落成功: {lootItem.ItemDef?.Name ?? "Unknown"} x{lootItem.Quantity}");
                }
                else
                {
                    LogWarning($"掉落失败: ItemFactory.CreateLootItem 返回 null");
                }
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
    /// 处理宝箱掉落逻辑
    /// </summary>
    public void ProcessChestLoot(ChestConfig chestConfig, Vector2 position)
    {
        if (chestConfig == null)
        {
            LogWarning("宝箱配置为空");
            return;
        }

        var lootTable = chestConfig.LootTable;
        if (lootTable == null)
        {
            LogWarning($"宝箱 '{chestConfig.ChestName}' 无掉落表配置");
            return;
        }

        ProcessLootTable(lootTable, position);
    }

    /// <summary>
    /// 处理掉落表（分组掉落）
    /// </summary>
    private void ProcessLootTable(LootTableConfig lootTable, Vector2 position)
    {
        if (lootTable.groups == null || lootTable.groups.Count == 0)
        {
            LogWarning("掉落表 groups 为空");
            return;
        }

        // 遍历每个分组
        foreach (var group in lootTable.groups)
        {
            if (group == null || group.entries == null || group.entries.Count == 0)
            {
                LogWarning($"掉落组为空或无效: groupName={group?.groupName}");
                continue;
            }

            // 过滤出有效条目
            var validEntries = group.entries.Where(e => e != null && e.itemConfig != null).ToList();
            if (validEntries.Count == 0) continue;

            // 有放回抽取：从有效条目中随机选，每次都独立判定概率
            for (int i = 0; i < group.maxPick; i++)
            {
                // 随机选一个条目
                var entry = validEntries[UnityEngine.Random.Range(0, validEntries.Count)];

                // 独立判定该次抽取的命中概率
                if (UnityEngine.Random.value > entry.dropChance) continue;

                // 计算数量
                int quantity = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);

                // 计算散落位置
                Vector2 spawnPos = position + UnityEngine.Random.insideUnitCircle * lootTable.DropSpreadRadius;

                // 生成掉落物
                Log($"生成掉落物: {entry.itemConfig.Name} x{quantity} at {spawnPos}");
                var lootItem = ItemFactory.Instance.CreateLootItem(entry.itemConfig, quantity, spawnPos);
                if (lootItem != null)
                {
                    _activeLootItems.Add(lootItem);
                    Log($"掉落成功: {lootItem.ItemDef?.Name ?? "Unknown"} x{lootItem.Quantity}");
                }
                else
                {
                    LogWarning($"掉落失败: ItemFactory.CreateLootItem 返回 null");
                }
            }
        }
    }

    // 日志
    private void Log(string msg) => Debug.Log($"[LootManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[LootManager] {msg}");
}
