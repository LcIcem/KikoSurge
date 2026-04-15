using UnityEngine;

/// <summary>
/// 武器配置 - 定义武器的开火方式和引用子弹配置
/// </summary>
[CreateAssetMenu(fileName = "GunConfig", menuName = "KikoSurge/武器/武器配置")]
public class GunConfig : ScriptableObject
{
    [Header("基础信息")]
    public string gunName;
    public GameObject gunPrefab;
    public Sprite icon;

    [Header("开火模式")]
    public FireMode fireMode = FireMode.Single;

    [Header("射速与散布")]
    public float fireRate = 0.5f;
    public float randomSpreadAngle = 0f;

    [Header("=== 霰弹（Spread专用）===")]
    [Min(1)] public int bulletCount = 1;
    public float shotgunSpreadAngle = 0f;

    [Header("=== 连发（Burst专用）===")]
    [Min(2)] public int burstCount = 3;
    public float burstSpeed = 0.05f;

    [Header("=== 蓄力（Charge专用）===")]
    public float chargeTime = 1f;

    [Header("弹药")]
    public int magazineSize = 30;
    public float reloadTime = 2f;

    [Header("后坐力")]
    public float recoilForce = 1f;

    [Header("随机权重")]
    public int weight = 10;

    [Header("子弹配置")]
    public BulletConfig bulletConfig;
}
