using LcIcemFramework.Core;

namespace Game.Event
{
    /// <summary>
    /// 游戏事件 ID 扩展
    /// 所有游戏业务事件统一在此定义
    /// </summary>
    public static class GameEventID
    {
        // 1000-1999: 会话生命周期
        public static readonly EventID OnSessionStart = (EventID)1000;        // 新场次开始
        public static readonly EventID OnSessionContinue = (EventID)1001;     // 继续已有场次（断点续玩）
        public static readonly EventID OnSessionEnd = (EventID)1002;         // 场次结束
        public static readonly EventID OnSessionSave = (EventID)1003;        // 场次保存

        // 2000-2999: 章节/层
        public static readonly EventID OnChapterStart = (EventID)2000;      // 章节开始
        public static readonly EventID OnChapterEnd = (EventID)2001;         // 章节结束
        public static readonly EventID OnLayerGenerated = (EventID)2002;     // 层生成完成
        public static readonly EventID OnLayerEnter = (EventID)2003;        // 进入新层
        public static readonly EventID OnLayerComplete = (EventID)2004;      // 层完成

        // 3000-3999: 房间
        public static readonly EventID OnRoomEnter = (EventID)3000;         // 进入房间
        public static readonly EventID OnRoomCleared = (EventID)3001;      // 房间清理完成
        public static readonly EventID OnRoomExit = (EventID)3002;          // 离开房间
        public static readonly EventID OnCorridorEnter = (EventID)3003;     // 进入走廊
        public static readonly EventID OnRequestRoomRefresh = (EventID)3004; // 请求刷新当前房间UI

        // 4000-4999: 玩家状态
        public static readonly EventID OnPlayerDamaged = (EventID)4000;     // 玩家受伤
        public static readonly EventID OnPlayerHealed = (EventID)4001;       // 玩家治疗
        public static readonly EventID OnPlayerDeath = (EventID)4002;       // 玩家死亡
        public static readonly EventID OnGoldChanged = (EventID)4003;       // 金币变化
        public static readonly EventID OnCurrentWeaponChanged = (EventID)4004;    // 当前武器变化
        public static readonly EventID OnAmmoChanged = (EventID)4005;              // 弹药变化
        public static readonly EventID OnReloadProgress = (EventID)4006;           // 换弹进度
        public static readonly EventID OnRelicAcquired = (EventID)4005;     // 获得遗物
        public static readonly EventID OnCardAcquired = (EventID)4006;      // 获得卡牌

        // 5000-5999: 战利品与波次
        public static readonly EventID OnLootDropped = (EventID)5000;       // 战利品掉落
        public static readonly EventID OnLootPickedUp = (EventID)5001;     // 战利品拾取
        public static readonly EventID OnWaveStarted = (EventID)5002;       // 波次开始
        public static readonly EventID OnWaveCleared = (EventID)5003;       // 波次完成
        public static readonly EventID OnWaveUpdate = (EventID)5006;       // 波次更新（异步模式敌人死亡时更新UI）
        public static readonly EventID OnBehaviourStart = (EventID)5004;      // 行为开始
        public static readonly EventID OnBehaviourEnd = (EventID)5005;        // 行为结束

        // 6000-6999: Meta 进度
        public static readonly EventID OnMetaSave = (EventID)6000;          // Meta 保存
        public static readonly EventID OnMetaLoad = (EventID)6001;         // Meta 加载
        public static readonly EventID OnUnlockWeapon = (EventID)6002;      // 解锁武器
        public static readonly EventID OnUpgradeWeapon = (EventID)6003;     // 升级武器
        public static readonly EventID OnUpgradeCard = (EventID)6004;       // 升级卡牌

        // 7000-7999: Modifiers
        public static readonly EventID OnModifierApplied = (EventID)7000;   // 修改器应用
        public static readonly EventID OnModifierRemoved = (EventID)7001;   // 修改器移除
        public static readonly EventID OnModifierStackChanged = (EventID)7002; // 修改器层数变化

        // 8000-8999: 战斗系统
        public static readonly EventID Combat_PlayerDamaged = (EventID)8000;    // 玩家受伤
        public static readonly EventID Combat_BulletSpawned = (EventID)8001;    // 子弹生成
        public static readonly EventID Combat_BulletHit = (EventID)8002;         // 子弹命中
        public static readonly EventID Combat_EnemyDamaged = (EventID)8003;      // 敌人受伤
        public static readonly EventID Combat_EnemyKilled = (EventID)8004;       // 敌人死亡
        public static readonly EventID Combat_EnemyAttack = (EventID)8005;       // 敌人攻击
        public static readonly EventID Combat_EnemyHitPlayer = (EventID)8013;     // 敌人命中玩家
        public static readonly EventID Combat_EnemySpawned = (EventID)8006;      // 敌人生成
        public static readonly EventID Combat_WaveStart = (EventID)8007;         // 波次开始（战斗）
        public static readonly EventID Combat_WaveComplete = (EventID)8008;      // 波次完成（战斗）
        public static readonly EventID Combat_AllWavesComplete = (EventID)8009;  // 所有波次完成
        public static readonly EventID Combat_Reloading = (EventID)8010;         // 开始换弹
        public static readonly EventID Combat_Reloaded = (EventID)8011;          // 换弹完成
        public static readonly EventID Combat_CancelReload = (EventID)8012;      // 取消换弹
        public static readonly EventID Combat_CriticalHit = (EventID)8020;      // 暴击发生（UI显示暴击特效）
        public static readonly EventID Combat_ShowDamageNumber = (EventID)8021; // 伤害数字显示

        // 9000-9999: UI 框架扩展
        public static readonly EventID UpdateHeartDisplay = (EventID)9000; // 更新血量显示
        public static readonly EventID PlayerIsDead = (EventID)9001;       // 玩家死亡（UI响应）
        public static readonly EventID RestartGame = (EventID)9002;         // 重新开始游戏
        public static readonly EventID OnDeathAnimationEnd = (EventID)9003; // 死亡动画播放结束
        public static readonly EventID OnBuffChanged = (EventID)9004;      // Buff变化（用于刷新UI）

        // 100-199: 大厅系统
        public static readonly EventID OnDungeonEntryConfirmed = (EventID)1010; // 确认进入地牢
        public static readonly EventID OnDungeonPanelClosed = (EventID)1011;   // 地牢面板关闭

        // 1100-1199: 背包系统
        public static readonly EventID OnInventoryChanged = (EventID)1100;       // 背包内容变化
        public static readonly EventID OnInventoryItemAdded = (EventID)1101;     // 物品添加
        public static readonly EventID OnInventoryItemRemoved = (EventID)1102;   // 物品移除
    }
}
