namespace LcIcemFramework.Core
{
    /// <summary>
    /// 框架层事件 ID 枚举
    /// <para>仅包含框架级基础事件（Audio、Input），游戏业务事件请使用 GameEventID</para>
    /// </summary>
    public enum EventID
    {
        // ==================== Audio ====================
        PlayBGM,
        PlaySFX,

        // ==================== Input ====================
        ShootPerformed,
    }
}
