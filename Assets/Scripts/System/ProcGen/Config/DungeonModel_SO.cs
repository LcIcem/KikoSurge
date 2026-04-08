using System.Collections.Generic;
using UnityEngine;
using ProcGen.Core;

namespace ProcGen.Config
{
    /// <summary>地牢生成总配置（ScriptableObject）
    /// 引用房间模板配置 SO，以及地牢级别的生成参数
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonModel_", menuName = "KikoSurge/Dungeon/地牢配置")]
    public class DungeonModel_SO : ScriptableObject
    {
        [Header("地图尺寸")]
        [Tooltip("地图宽度（格）")]
        public int mapWidth = 80;
        [Tooltip("地图高度（格）")]
        public int mapHeight = 60;
        [Tooltip("地图边界留空（格）")]
        public int borderSize = 2;

        [Header("房间模板配置")]
        [Tooltip("房间模板配置 SO（在该 SO 中批量编辑各类房间的尺寸约束）")]
        public RoomTemplateConfig_SO roomTemplateConfig;

        [Header("走廊")]
        [Tooltip("走廊宽度（格）")]
        public int corridorWidth = 2;

        [Header("普通房间")]
        [Tooltip("保证生成的普通房间数量")]
        public int normalRoomCount = 15;
        [Range(0, 100)]
        [Tooltip("额外生成普通房间的概率（0=不额外生成）")]
        public int normalExtraChance = 50;

        [Header("精英房（Elite）")]
        [Tooltip("保证生成的精英房数量")]
        public int eliteRoomCount = 0;
        [Range(0, 100)]
        [Tooltip("额外生成精英房的概率（0=不额外生成）")]
        public int eliteExtraChance = 30;

        [Header("宝藏间（Treasure）")]
        [Tooltip("保证生成的宝藏间数量")]
        public int treasureRoomCount = 3;
        [Range(0, 100)]
        [Tooltip("额外生成宝藏间的概率（0=不额外生成）")]
        public int treasureExtraChance = 0;

        [Header("商店（Shop）")]
        [Tooltip("保证生成的商店数量")]
        public int shopRoomCount = 1;
        [Range(0, 100)]
        [Tooltip("额外生成商店的概率（0=不额外生成）")]
        public int shopExtraChance = 0;

        [Header("休息室（Rest）")]
        [Tooltip("保证生成的休息室数量")]
        public int restRoomCount = 1;
        [Range(0, 100)]
        [Tooltip("额外生成休息室的概率（0=不额外生成）")]
        public int restExtraChance = 0;

        [Header("事件房（Event）")]
        [Tooltip("保证生成的事件房数量")]
        public int eventRoomCount = 1;
        [Range(0, 100)]
        [Tooltip("额外生成事件房的概率（0=不额外生成）")]
        public int eventExtraChance = 0;

        [Header("Boss房（Boss）")]
        [Tooltip("保证生成的Boss房数量")]
        public int bossRoomCount = 1;
        [Range(0, 100)]
        [Tooltip("额外生成Boss房的概率（0=不额外生成）")]
        public int bossExtraChance = 0;

        // ==================== 便利查询方法 ====================

        /// <summary>获取地图有效范围（排除边界）</summary>
        public RectInt GetMapBounds()
        {
            return new RectInt(
                -mapWidth / 2,
                -mapHeight / 2,
                mapWidth - borderSize * 2,
                mapHeight - borderSize * 2
            );
        }

        /// <summary>根据房间类型获取对应的第一条配置数据（兼容旧代码）</summary>
        public RoomConfigData GetRoomConfigData(RoomType roomType)
        {
            return roomTemplateConfig != null
                ? roomTemplateConfig.GetTemplate(roomType)
                : null;
        }

        /// <summary>根据房间类型获取对应的所有配置数据（支持同一类型多条配置）</summary>
        public List<RoomConfigData> GetTemplates(RoomType roomType)
        {
            return roomTemplateConfig != null
                ? roomTemplateConfig.GetTemplates(roomType)
                : new List<RoomConfigData>();
        }

        /// <summary>获取普通房间的所有配置数据（用于生成普通房间）</summary>
        public List<RoomConfigData> normalData => GetTemplates(RoomType.Normal);
    }
}
