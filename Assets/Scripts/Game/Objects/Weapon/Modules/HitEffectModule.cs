using UnityEngine;

/// <summary>
/// 命中效果模块 - 处理子弹命中后的效果
/// TODO: 后续扩展完整实现（爆炸、冰冻、燃烧等）
/// </summary>
public static class HitEffectModule
{
    public static void Apply(GameObject target, HitEffect effect, float value)
    {
        switch (effect)
        {
            case HitEffect.None:
                break;
            case HitEffect.Explode:
                // TODO: 爆炸效果实现
                Debug.Log($"[HitEffect] 爆炸造成 {value} 范围伤害");
                break;
            case HitEffect.Freeze:
                // TODO: 冰冻效果实现
                Debug.Log($"[HitEffect] 冰冻目标 {value} 秒");
                break;
            case HitEffect.Burn:
                // TODO: 燃烧效果实现
                Debug.Log($"[HitEffect] 燃烧造成 {value} 持续伤害");
                break;
            case HitEffect.Bounce:
                // TODO: 反弹效果实现
                Debug.Log($"[HitEffect] 子弹反弹");
                break;
            case HitEffect.Pierce:
                // 穿透在 Bullet.cs 中通过 penetrateCount 实现
                Debug.Log($"[HitEffect] 穿透");
                break;
        }
    }
}
