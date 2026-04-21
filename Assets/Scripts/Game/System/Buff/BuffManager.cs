using System.Collections.Generic;
using UnityEngine;
using LcIcemFramework;
using LcIcemFramework.Core;
using Game.Event;

/// <summary>
/// Buff管理器（单例）
/// <para>管理所有激活的限时Buff，提供查询接口，驱动DOT Tick更新</para>
/// </summary>
public class BuffManager : Singleton<BuffManager>
{
        // Buff字典：buffId -> ActiveBuff
        private Dictionary<int, ActiveBuff> _activeBuffs = new Dictionary<int, ActiveBuff>();

        // 类型索引：BuffType -> List<buffId>（优化查询）
        private Dictionary<BuffType, List<int>> _buffTypeIndex = new Dictionary<BuffType, List<int>>();

        // 敌人索引：targetId -> EnemyBase（用于快速访问）
        private Dictionary<string, EnemyBase> _enemyIndex = new Dictionary<string, EnemyBase>();

        // DOT伤害回调：targetId -> 伤害值列表
        private Dictionary<string, List<float>> _pendingDotDamage = new Dictionary<string, List<float>>();

        protected override void Init()
        {
            // 注册到TimerManager的Update循环
            TimerManager.Instance.AddBuffTickListener(TickBuffs);
            Log("Initialized and registered to TimerManager");
        }

        #region Public API

        /// <summary>
        /// 添加Buff
        /// </summary>
        /// <param name="type">Buff类型</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="value">Buff数值</param>
        /// <param name="sourceId">来源ID（itemId/weaponId）</param>
        /// <param name="tickInterval">Tick间隔（秒），0表示非DOT</param>
        /// <param name="maxStacks">最大叠加层数</param>
        /// <param name="targetId">目标ID（敌人用GameObject InstanceID）</param>
        /// <returns>添加的Buff的buffId</returns>
        public int AddBuff(BuffType type, float duration, float value,
            string sourceId, float tickInterval = 0f, int maxStacks = 1, string targetId = null)
        {
            // 检查是否可刷新（同名Buff存在）
            int existingId = FindBuffByTypeAndSource(type, sourceId);
            if (existingId >= 0)
            {
                var existing = _activeBuffs[existingId];
                if (existing.stackCount < existing.maxStacks)
                {
                    existing.Refresh();
                    existing.value = value;
                    Log($"Refreshed buff {type} (stack {existing.stackCount}/{existing.maxStacks})");
                    NotifyBuffChanged();
                    return existingId;
                }
                else if (existing.maxStacks > 1)
                {
                    existing.Refresh();
                    NotifyBuffChanged();
                    return existingId;
                }
            }

            // 创建新Buff
            var buff = ActiveBuff.Create(type, duration, value, sourceId, tickInterval, maxStacks, targetId);
            _activeBuffs[buff.buffId] = buff;

            // 更新类型索引
            if (!_buffTypeIndex.ContainsKey(type))
                _buffTypeIndex[type] = new List<int>();
            _buffTypeIndex[type].Add(buff.buffId);

            Log($"Added buff {type} (id={buff.buffId}, duration={duration}, value={value}, source={sourceId})");
            NotifyBuffChanged();
            return buff.buffId;
        }

        /// <summary>
        /// 移除Buff
        /// </summary>
        public void RemoveBuff(int buffId)
        {
            if (!_activeBuffs.TryGetValue(buffId, out var buff))
                return;

            // 从类型索引移除
            if (_buffTypeIndex.TryGetValue(buff.type, out var list))
            {
                list.Remove(buffId);
            }

            // 如果是敌人buff，移除其效果
            if (buff.targetId != null && _enemyIndex.TryGetValue(buff.targetId, out var enemy))
            {
                OnBuffExpiredForEnemy(buff, enemy);
            }

            _activeBuffs.Remove(buffId);
            Log($"Removed buff {buff.type} (id={buffId})");
            NotifyBuffChanged();
        }

        /// <summary>
        /// 注册敌人（EnemyBase.OnEnable时调用）
        /// </summary>
        public void RegisterEnemy(string targetId, EnemyBase enemy)
        {
            if (!_enemyIndex.ContainsKey(targetId))
            {
                _enemyIndex[targetId] = enemy;
                Log($"Registered enemy: {targetId}");
            }
        }

        /// <summary>
        /// 注销敌人（EnemyBase.OnDisable时调用）
        /// </summary>
        public void UnregisterEnemy(string targetId)
        {
            if (_enemyIndex.ContainsKey(targetId))
            {
                _enemyIndex.Remove(targetId);
                Log($"Unregistered enemy: {targetId}");
            }
        }

        /// <summary>
        /// 获取玩家当前防御力加成（来自Shield buff）
        /// </summary>
        public float GetPlayerShieldBonus()
        {
            float bonus = 0f;
            if (_buffTypeIndex.TryGetValue(BuffType.Shield, out var list))
            {
                foreach (var id in list)
                {
                    if (_activeBuffs.TryGetValue(id, out var buff) && !buff.IsExpired)
                    {
                        bonus += buff.value * buff.stackCount;
                    }
                }
            }
            return bonus;
        }

        /// <summary>
        /// 获取玩家当前速度倍率（来自SpeedBoost buff）
        /// <para>返回值如1.5f表示150%速度</para>
        /// </summary>
        public float GetPlayerSpeedMultiplier()
        {
            float multiplier = 1f;
            if (_buffTypeIndex.TryGetValue(BuffType.SpeedBoost, out var list))
            {
                foreach (var id in list)
                {
                    if (_activeBuffs.TryGetValue(id, out var buff) && !buff.IsExpired)
                    {
                        // value=1.5表示+50%速度
                        multiplier += (buff.value - 1f) * buff.stackCount;
                    }
                }
            }
            return Mathf.Max(0.1f, multiplier);
        }

        /// <summary>
        /// 获取当前所有有效Buff（用于快照保存）
        /// </summary>
        public List<ActiveBuff> GetAllActiveBuffs()
        {
            var result = new List<ActiveBuff>();
            foreach (var buff in _activeBuffs.Values)
            {
                if (!buff.IsExpired)
                    result.Add(buff);
            }
            return result;
        }

        /// <summary>
        /// 从快照恢复Buff
        /// </summary>
        public void RestoreFromSnapshot(List<ActiveBuff> savedBuffs)
        {
            ClearAllBuffs();
            if (savedBuffs == null) return;

            foreach (var buff in savedBuffs)
            {
                // 重新生成ID（避免冲突）
                int newId = ActiveBuff.GenerateId();
                var restored = new ActiveBuff
                {
                    buffId = newId,
                    type = buff.type,
                    remainingTime = buff.remainingTime,
                    totalDuration = buff.totalDuration,
                    value = buff.value,
                    tickInterval = buff.tickInterval,
                    tickTimer = buff.tickTimer,
                    sourceId = buff.sourceId,
                    targetId = buff.targetId,
                    stackCount = buff.stackCount,
                    maxStacks = buff.maxStacks
                };

                _activeBuffs[newId] = restored;

                if (!_buffTypeIndex.ContainsKey(buff.type))
                    _buffTypeIndex[buff.type] = new List<int>();
                _buffTypeIndex[buff.type].Add(newId);
            }
            Log($"Restored {_activeBuffs.Count} buffs from snapshot");
        }

        /// <summary>
        /// 清除所有Buff（用于死亡/重置）
        /// </summary>
        public void ClearAllBuffs()
        {
            // 先重置所有敌人的buff效果
            foreach (var kvp in _enemyIndex)
            {
                kvp.Value?.ApplyFreezeMultiplier(1f);
            }

            _activeBuffs.Clear();
            _buffTypeIndex.Clear();
            _pendingDotDamage.Clear();
            Log("Cleared all buffs");
            NotifyBuffChanged();
        }

        #endregion

        /// <summary>
        /// 通知UI刷新（buff变化时调用）
        /// </summary>
        private void NotifyBuffChanged()
        {
            EventCenter.Instance.Publish(GameEventID.OnBuffChanged);
        }

        #region Private Methods

        /// <summary>
        /// 每帧Tick更新（由TimerManager驱动）
        /// </summary>
        private void TickBuffs(float deltaTime)
        {
            // 克隆一份key列表避免修改字典异常
            var ids = new List<int>(_activeBuffs.Keys);

            // 收集DOT伤害
            _pendingDotDamage.Clear();

            foreach (var id in ids)
            {
                if (!_activeBuffs.TryGetValue(id, out var buff))
                    continue;

                // Tick推进
                bool triggered = buff.Tick(deltaTime);

                // DOT Buff触发
                if (triggered && buff.IsDot && buff.type == BuffType.Burn)
                {
                    // 收集DOT伤害，等待处理
                    if (!_pendingDotDamage.ContainsKey(buff.targetId))
                        _pendingDotDamage[buff.targetId] = new List<float>();
                    _pendingDotDamage[buff.targetId].Add(buff.value * buff.stackCount);
                }

                // 过期移除
                if (buff.IsExpired)
                {
                    RemoveBuff(id);
                }
            }

            // 处理DOT伤害（在所有buff tick完成后）
            ProcessDotDamage();

            // 更新敌人buff效果
            UpdateEnemyBuffs();
        }

        /// <summary>
        /// 处理DOT伤害
        /// </summary>
        private void ProcessDotDamage()
        {
            foreach (var kvp in _pendingDotDamage)
            {
                string targetId = kvp.Key;
                if (!_enemyIndex.TryGetValue(targetId, out var enemy))
                    continue;

                float totalDamage = 0f;
                foreach (var dmg in kvp.Value)
                {
                    totalDamage += dmg;
                }

                if (totalDamage > 0 && enemy != null)
                {
                    enemy.TakeDotDamage(totalDamage);
                }
            }
        }

        /// <summary>
        /// 更新所有敌人的buff效果
        /// </summary>
        private void UpdateEnemyBuffs()
        {
            foreach (var kvp in _enemyIndex)
            {
                string targetId = kvp.Key;
                EnemyBase enemy = kvp.Value;
                if (enemy == null) continue;

                float freezeMult = 1f;

                foreach (var buff in _activeBuffs.Values)
                {
                    if (buff.IsExpired || buff.targetId != targetId)
                        continue;

                    if (buff.type == BuffType.Freeze)
                    {
                        // 取最小减速（叠加时取最小值）
                        freezeMult = Mathf.Min(freezeMult, buff.value * buff.stackCount);
                    }
                }

                enemy.ApplyFreezeMultiplier(freezeMult);
            }
        }

        /// <summary>
        /// Buff过期时重置敌人效果
        /// </summary>
        private void OnBuffExpiredForEnemy(ActiveBuff buff, EnemyBase enemy)
        {
            if (buff.type == BuffType.Freeze && enemy != null)
            {
                // 需要重新计算其他freeze buff的效果
                float freezeMult = 1f;
                foreach (var other in _activeBuffs.Values)
                {
                    if (!other.IsExpired && other.targetId == buff.targetId && other.type == BuffType.Freeze)
                    {
                        freezeMult = Mathf.Min(freezeMult, other.value * other.stackCount);
                    }
                }
                enemy.ApplyFreezeMultiplier(freezeMult);
            }
        }

        /// <summary>
        /// 根据类型和来源查找Buff
        /// </summary>
        private int FindBuffByTypeAndSource(BuffType type, string sourceId)
        {
            if (!_buffTypeIndex.TryGetValue(type, out var list))
                return -1;

            foreach (var id in list)
            {
                if (_activeBuffs.TryGetValue(id, out var buff) &&
                    buff.sourceId == sourceId && !buff.IsExpired)
                {
                    return id;
                }
            }
            return -1;
        }

        #endregion

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void Log(string msg) => Debug.Log($"[BuffManager] {msg}");

        [System.Diagnostics.Conditional("DEBUG")]
        public void DebugPrintAllBuffs()
        {
            Log($"Active buffs: {_activeBuffs.Count}");
            foreach (var buff in _activeBuffs.Values)
            {
                Log($"  - {buff.type} (id={buff.buffId}, time={buff.remainingTime:F2}s, stack={buff.stackCount}/{buff.maxStacks}, target={buff.targetId})");
            }
        }

        #endregion
}
