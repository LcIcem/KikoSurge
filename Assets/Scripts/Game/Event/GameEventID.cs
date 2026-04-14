using LcIcemFramework.Core;

namespace Game.Event
{
    /// <summary>
    /// 游戏事件 ID 扩展
    /// <para>通过 static readonly EventID + (EventID)1000+ 扩展框架已有枚举，不修改框架层文件</para>
    /// </summary>
    public static class GameEventID
    {
        // 1000-1999: 会话生命周期
        /// <summary>新场次开始</summary>
        public static readonly EventID OnSessionStart = (EventID)1000;
        /// <summary>继续已有场次（断点续玩）</summary>
        public static readonly EventID OnSessionContinue = (EventID)1001;
        /// <summary>场次结束</summary>
        public static readonly EventID OnSessionEnd = (EventID)1002;
        /// <summary>场次保存</summary>
        public static readonly EventID OnSessionSave = (EventID)1003;

        // 2000-2999: 章节/层
        /// <summary>章节开始</summary>
        public static readonly EventID OnChapterStart = (EventID)2000;
        /// <summary>章节结束</summary>
        public static readonly EventID OnChapterEnd = (EventID)2001;
        /// <summary>层生成完成</summary>
        public static readonly EventID OnLayerGenerated = (EventID)2002;
        /// <summary>进入新层</summary>
        public static readonly EventID OnLayerEnter = (EventID)2003;
        /// <summary>层完成</summary>
        public static readonly EventID OnLayerComplete = (EventID)2004;

        // 3000-3999: 房间
        /// <summary>进入房间</summary>
        public static readonly EventID OnRoomEnter = (EventID)3000;
        /// <summary>房间清理完成</summary>
        public static readonly EventID OnRoomCleared = (EventID)3001;
        /// <summary>离开房间</summary>
        public static readonly EventID OnRoomExit = (EventID)3002;

        // 4000-4999: 玩家状态
        /// <summary>玩家受伤</summary>
        public static readonly EventID OnPlayerDamaged = (EventID)4000;
        /// <summary>玩家治疗</summary>
        public static readonly EventID OnPlayerHealed = (EventID)4001;
        /// <summary>玩家死亡</summary>
        public static readonly EventID OnPlayerDeath = (EventID)4002;
        /// <summary>金币变化</summary>
        public static readonly EventID OnGoldChanged = (EventID)4003;
        /// <summary>武器变化</summary>
        public static readonly EventID OnWeaponChanged = (EventID)4004;
        /// <summary>获得遗物</summary>
        public static readonly EventID OnRelicAcquired = (EventID)4005;
        /// <summary>获得卡牌</summary>
        public static readonly EventID OnCardAcquired = (EventID)4006;

        // 5000-5999: 战利品
        /// <summary>战利品掉落</summary>
        public static readonly EventID OnLootDropped = (EventID)5000;
        /// <summary>战利品拾取</summary>
        public static readonly EventID OnLootPickedUp = (EventID)5001;
        /// <summary>波次开始</summary>
        public static readonly EventID OnWaveStarted = (EventID)5002;
        /// <summary>波次完成</summary>
        public static readonly EventID OnWaveCleared = (EventID)5003;

        // 6000-6999: Meta 进度
        /// <summary>Meta 保存</summary>
        public static readonly EventID OnMetaSave = (EventID)6000;
        /// <summary>Meta 加载</summary>
        public static readonly EventID OnMetaLoad = (EventID)6001;
        /// <summary>解锁武器</summary>
        public static readonly EventID OnUnlockWeapon = (EventID)6002;
        /// <summary>升级武器</summary>
        public static readonly EventID OnUpgradeWeapon = (EventID)6003;
        /// <summary>升级卡牌</summary>
        public static readonly EventID OnUpgradeCard = (EventID)6004;

        // 7000-7999: Modifiers
        /// <summary>修改器应用</summary>
        public static readonly EventID OnModifierApplied = (EventID)7000;
        /// <summary>修改器移除</summary>
        public static readonly EventID OnModifierRemoved = (EventID)7001;
        /// <summary>修改器层数变化</summary>
        public static readonly EventID OnModifierStackChanged = (EventID)7002;
    }
}
