namespace ProcGen.Seed
{
    /// <summary>可接受随机数生成器的接口
    /// 用于房间内可生成内容的组件（敌人刷新、奖励掉落等）
    /// 实现此接口的组件在房间激活时接收 GameRandom，确保同一种子下产生相同内容
    /// </summary>
    public interface ISpawnable
    {
        void SetRng(GameRandom rng);
    }
}
