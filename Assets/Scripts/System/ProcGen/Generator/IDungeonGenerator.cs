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
        /// <summary>根据配置生成地牢图结构</summary>
        /// <param name="config">地牢生成配置</param>
        /// <param name="seed">游戏种子（为空则使用默认随机种子）</param>
        /// <returns>包含所有房间和走廊的地牢图</returns>
        DungeonGraph Generate(DungeonModel_SO config, GameSeed seed = null);
    }
}
