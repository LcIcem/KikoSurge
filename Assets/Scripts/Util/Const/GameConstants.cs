namespace Game.Util.Const
{
/// <summary>
/// 全局常量集中管理类
/// </summary>
public static class GameConstants
{
    // 物理
    public const float GRAVITY = 0f; // 俯视角无重力
    public const float PIXEL_PER_UNIT = 16f; // PPU = 16

    // 游戏数值
    public const int MAX_ENEMY_COUNT = 30;
    public const int MAX_BULLET_COUNT = 800;
    public const int BULLET_POOL_INITIAL = 200;

    // 输入
    public const float MOUSE_SENSITIVITY = 1f; // 鼠标灵敏度
}
}