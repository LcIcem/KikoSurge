using UnityEngine;

/// <summary>
/// 武器配置 - 定义武器的开火方式和引用子弹配置
/// </summary>
[CreateAssetMenu(fileName = "GunConfig", menuName = "KikoSurge/武器/武器配置")]
public class GunConfig : ScriptableObject
{
    [Header("基础信息")]
    public string gunName;
    public GameObject gunPrefab;    // 武器预设体（表现层）
    public Sprite icon;

    [Header("开火设置")]
    public FireMode fireMode = FireMode.Single;
    public float fireRate = 0.5f;       // 每次射击之间的间隔
    public int burstCount = 3;           // 连发子弹数量
    public float burstSpeed = 0.05f;    // 连发子弹之间的间隔

    [Header("随机散布")]
    public float randomSpreadAngle = 0f;  // 每颗子弹的随机散布角度（所有武器适用）

    [Header("霰弹设置")]
    public int bulletCount = 1;              // 霰弹子弹数量
    public float shotgunSpreadAngle = 0f;    // 霰弹扇形扩散角度

    [Header("子弹配置")]
    public BulletConfig bulletConfig;

    [Header("弹药")]
    public int magazineSize = 30;
    public float reloadTime = 2f;

    [Header("后坐力")]
    public float recoilForce = 1f;

    [Header("随机权重")]
    public int weight = 10;
}
