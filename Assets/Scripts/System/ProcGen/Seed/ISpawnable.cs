namespace ProcGen.Seed
{
    /// <summary>可接受游戏种子的接口
    /// 用于房间内可生成内容的组件（敌人刷新、奖励掉落等）
    /// 实现此接口的组件在房间激活时接收 GameSeed，确保同一 seed 下产生相同内容
    /// </summary>
    public interface ISpawnable
    {
        void SetSeed(GameSeed seed);
    }
}
