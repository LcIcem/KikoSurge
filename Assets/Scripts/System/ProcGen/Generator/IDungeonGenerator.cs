using ProcGen.Config;
using ProcGen.Core;
using ProcGen.Seed;

namespace ProcGen.Generator
{
    /// <summary>地牢生成器接口
    /// 所有生成算法实现此接口，实现 Generate 方法
    /// </summary>
    public interface IDungeonGenerator
    {
        /// <summary>根据配置生成地牢图结构及瓦片数据</summary>
        /// <param name="config">地牢生成配置</param>
        /// <param name="rng">游戏随机数生成器</param>
        /// <returns>tuple: Item1=地牢图结构, Item2=瓦片预计算数据</returns>
        (DungeonGraph graph, DungeonTileData tileData) Generate(DungeonModel_SO config, GameRandom rng = null);
    }
}
