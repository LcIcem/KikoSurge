namespace LcIcemFramework.Util.Const
{
/// <summary>
/// 框架层常量集中管理
/// </summary>
public static class Constants
{
    // 对象池
    public const int MAX_POOL_SIZE = 500;

    // 音效池
    public const int SFX_POOL_SIZE = 10;

    // 相机
    public const float CAMERA_SMOOTH_SPEED = 5f; // 相机平滑移动速度
    public const float CAMERA_OFFSET_Z = -10f; // 相机Z轴偏移

    // UI
    public const string UI_PATH = "";    // Addressables分组路径

    // 存档
    public const int MAX_SLOT = 3;
    public const string SAVE_ENCRYPTION_KEY = "kikoSurge2026";
}
}