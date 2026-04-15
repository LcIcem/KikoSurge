using LcIcemFramework.Managers;
using UnityEngine;

/// <summary>
/// 霰弹枪武器（多发散射）。
/// </summary>
public class ShotgunWeapon : GunWeapon
{
    public int PelletCount { get; set; } = 6;       // 弹丸数量

    public float SpreadAngle { get; set; } = 30f;   //散布总角度（度）

    public float FalloffStart { get; set; } = 5f;   // 伤害衰减开始距离

    public ShotgunWeapon(Player owner) : base(owner)
    {
    }

    /// <summary>
    /// 从配置数据初始化武器属性
    /// </summary>
    public void Init(string name, float damage, float fireRate, float reloadTime,
        int magazineSize, float recoilForce, Sprite icon,
        GameObject bulletPrefab, float bulletSpeed,
        int pelletCount, float spreadAngle, float falloffStart, float range)
    {
        base.Init(name, damage, fireRate, reloadTime, magazineSize, recoilForce, icon,
            bulletPrefab, bulletSpeed, 0, range);  // spread 不用于霰弹枪

        BulletSpeed = bulletSpeed;
        PelletCount = pelletCount;
        SpreadAngle = spreadAngle;
        FalloffStart = falloffStart;
        Range = range;

        Type = WeaponType.Shotgun;
    }

    protected override void SpawnBullet(Vector3 direction)
    {
        float stepAngle = SpreadAngle / (PelletCount - 1);
        float startAngle = -SpreadAngle / 2f;

        string bulletName = BulletPrefab != null ? BulletPrefab.name : "Bullet";

        for (int i = 0; i < PelletCount; i++)
        {
            float angle = startAngle + stepAngle * i;
            Vector3 pelletDir = Quaternion.Euler(0, 0, angle) * direction;
            Vector3 spawnPos = _owner.transform.position + pelletDir * 0.5f;

            GameObject pelletObj = ManagerHub.Pool.Get(bulletName, spawnPos, Quaternion.identity);
            var pellet = pelletObj.GetComponent<Bullet>();
            pellet.Init(pelletDir, BulletSpeed, Range, 0, this);
        }
    }
}