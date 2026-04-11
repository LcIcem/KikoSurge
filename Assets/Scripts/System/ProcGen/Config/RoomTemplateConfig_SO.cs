using UnityEngine;
using ProcGen.Core;
using System.Collections.Generic;

namespace ProcGen.Config
{
    /// <summary>房间模板配置（ScriptableObject）
    /// 持有所有 RoomType 对应的尺寸配置，可批量在 Inspector 中编辑
    /// </summary>
    [CreateAssetMenu(fileName = "RoomTemplateConfig_SO", menuName = "KikoSurge/Dungeon/房间模板配置")]
    public class RoomTemplateConfig_SO : ScriptableObject
    {
        [Header("房间模板列表")]
        [Tooltip("各类房间的配置数据，每种 RoomType 对应一条")]
        public List<RoomConfigData> templates;

        /// <summary>根据房间类型获取对应的第一条配置数据（兼容旧代码）</summary>
        public RoomConfigData GetTemplate(RoomType roomType)
        {
            var list = GetTemplates(roomType);
            return list.Count > 0 ? list[0] : default;
        }

        /// <summary>根据房间类型获取对应的所有配置数据（支持同一类型多条配置）</summary>
        public List<RoomConfigData> GetTemplates(RoomType roomType)
        {
            var result = new List<RoomConfigData>();
            if (templates == null)
                return result;

            foreach (var data in templates)
            {
                if (data.roomType == roomType)
                    result.Add(data);
            }
            return result;
        }
    }
}
