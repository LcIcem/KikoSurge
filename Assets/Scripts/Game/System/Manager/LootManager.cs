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
        // 懒初始化
    }

    private void Start()
    {
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
    /// 处理掉落逻辑（分组掉落）
    /// </summary>
    private void ProcessLootDrop(EnemyBase enemy, Vector2 deathPosition)
    {
        if (enemy == null) return;

        // 直接从 EnemyConfig 获取掉落表
        var lootTable = enemy.EnemyConfig?.lootTable;
        if (lootTable == null)
        {
            LogWarning($"无掉落表配置: EnemyId={enemy.EnemyId}, EnemyConfig={enemy.EnemyConfig}");
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

            // 独立判定每个条目的概率，记录命中的条目
            var candidates = new List<LootTableConfig.LootEntry>();
            foreach (var entry in group.entries)
            {
                if (entry == null || entry.itemConfig == null) continue;

                // 独立判定每个条目的概率
                if (Random.value <= entry.dropChance)
                {
                    candidates.Add(entry);
                }
            }

            // 限制最大掉落数量
            int pickCount = Mathf.Min(candidates.Count, group.maxPick);
            Log($"分组 '{group.groupName}' 判定掉落 {pickCount}/{group.maxPick} 个（命中 {candidates.Count} 个）");

            if (pickCount == 0) continue;

            // 打乱候选列表并取前 maxPick 个
            candidates = candidates.OrderBy(_ => Random.value).ToList();

            for (int i = 0; i < pickCount; i++)
            {
                var entry = candidates[i];

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

    // 日志
    private void Log(string msg) => Debug.Log($"[LootManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[LootManager] {msg}");
}
