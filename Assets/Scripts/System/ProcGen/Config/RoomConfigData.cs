using UnityEngine;
using ProcGen.Core;
using ProcGen.Seed;

namespace ProcGen.Config
{
    /// <summary>房间配置数据（可序列化类）
    /// 每个 RoomType 对应一条配置，定义该类型房间的尺寸约束与距离限制
    /// </summary>
    [System.Serializable]
    public class RoomConfigData
    {
        [Tooltip("房间显示名称（用于区分同名类型的多个配置）")]
        public string displayName = "";

        [Tooltip("房间类型")]
        public RoomType roomType = RoomType.Normal;

        [Tooltip("房间最小尺寸（格）")]
        public Vector2Int minSize = new Vector2Int(5, 5);

        [Tooltip("房间最大尺寸（格）")]
        public Vector2Int maxSize = new Vector2Int(10, 10);

        [Tooltip("距起点房间中心的最小曼哈顿距离（格）。0 = 不限制下界（可紧邻起点）；>=1 = 房间中心距起点至少该距离")]
        public int minDistFromStart = 0;

        [Tooltip("距起点房间中心的最大曼哈顿距离（格）。-1 = 不限制上界（可放置在任意远处）；0 = 必须在起点处")]
        public int maxDistFromStart = -1;

        /// <summary>根据配置生成随机尺寸（使用 Unity 全局随机，不推荐用于种子机制）</summary>
        public Vector2Int GetRandomSize()
        {
            int w = Random.Range(minSize.x, maxSize.x + 1);
            int h = Random.Range(minSize.y, maxSize.y + 1);
            return new Vector2Int(w, h);
        }

        /// <summary>根据配置生成随机尺寸（使用种子驱动的随机数生成器）</summary>
        public Vector2Int GetRandomSize(GameSeed seed)
        {
            int w = seed.Range(minSize.x, maxSize.x + 1);
            int h = seed.Range(minSize.y, maxSize.y + 1);
            return new Vector2Int(w, h);
        }
    }
}
