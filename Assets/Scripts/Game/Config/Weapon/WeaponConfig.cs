using UnityEngine;

/// <summary>
/// 武器配置
/// </summary>
[CreateAssetMenu(fileName = "WeaponConfig", menuName = "KikoSurge/武器/武器配置")]
public class WeaponConfig : ScriptableObject
{
    [Tooltip("关联的物品定义")]
    public ItemConfig itemConfig;

    [Tooltip("武器预制体")]
    public GameObject prefab;

    [Tooltip("开火模式")]
    public FireMode fireMode = FireMode.Single;

    [Tooltip("射速（秒）")]
    public float fireRate = 0.5f;

    [Tooltip("随机散布角度")]
    public float randomSpreadAngle = 0f;

    [Tooltip("霰弹子弹数量")]
    [Min(1)]
    public int bulletCount = 1;

    [Tooltip("霰弹散布角度")]
    public float shotgunSpreadAngle = 0f;

    [Tooltip("连发子弹数量")]
    [Min(2)]
    public int burstCount = 3;

    [Tooltip("连发间隔（秒）")]
    public float burstSpeed = 0.05f;

    [Tooltip("蓄力时间（秒）")]
    public float chargeTime = 1f;

    [Tooltip("弹夹容量")]
    public int magazineSize = 30;

    [Tooltip("换弹时间（秒）")]
    public float reloadTime = 2f;

    [Tooltip("后坐力")]
    public float recoilForce = 1f;

    [Header("伤害属性")]
    [Tooltip("武器固定伤害加成")]
    public float weaponDamage = 0f;

    [Tooltip("武器暴击率加成")]
    public float weaponCritRate = 0f;

    [Tooltip("武器暴击倍率加成")]
    public float weaponCritMultiplier = 0f;

    [Tooltip("武器伤害加成%")]
    public float weaponDamageBonusPercent = 0f;

    [Tooltip("子弹配置")]
    public BulletConfig bulletConfig;
}
