using UnityEngine;

/// <summary>
/// 瞄准输入开关
/// <para>UI 打开时设置为 false 禁用武器旋转和人物翻转跟随鼠标</para>
/// </summary>
public static class AimInput
{
    private static bool _enabled = true;

    public static bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }
}
