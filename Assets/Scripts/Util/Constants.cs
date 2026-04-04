/// <summary>
/// 框架层常量集中管理
/// </summary>
public static class Constants
{
    // 对象池
    public const int MAX_POOL_SIZE = 500;

    // 键位相关
    public const string SO_DEFAULT_PATH = "Config/InputConfig_SO"; // SO键位默认配置文件 路径(相对目录为Resources)
    public const string KEY_BINDINGS_PATH = "keybindings.json"; // 键位绑定文件 保存路径

    // 相机
    public const float CAMERA_SMOOTH_SPEED = 5f;
    public const float CAMERA_OFFSET_Z = -10f; // 2D 正交相机标准偏移
}