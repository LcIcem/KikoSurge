using UnityEngine;

/// <summary>
/// 子弹配置 - 定义子弹的行为和属性
/// </summary>
[CreateAssetMenu(fileName = "BulletConfig", menuName = "KikoSurge/武器/子弹配置")]
public class BulletConfig : ScriptableObject
{
    [Header("弹道设置")]
    public BulletType bulletType = BulletType.Straight;
    public GameObject bulletPrefab;

    [Header("飞行参数")]
    public float bulletSpeed = 20f;
    public float maxDistance = 50f;

    [Header("伤害")]
    public int damage = 10;

    [Header("命中效果")]
    public HitEffect hitEffect = HitEffect.None;
    public float effectValue = 1f;

    [Header("穿透")]
    public int penetrateCount = 0;

    [Header("追踪（仅追踪弹）")]
    public float homingRange = 10f;
    public float homingStrength = 5f;  // 追踪转向速度
}
