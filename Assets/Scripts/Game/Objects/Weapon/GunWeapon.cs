using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using UnityEngine;
using Game.Event;

/// <summary>
/// 枪械武器（单发射击）。
/// </summary>
public class GunWeapon : WeaponBase
{
    public float BulletSpeed { get; set; } = 20f;   // 子弹速度
    public float Spread { get; set; } = 2f;         // 散布角度（度）
    public float Range { get; set; } = 50f;         // 射程

    public GunWeapon(Player owner) : base(owner)
    {
    }

    /// <summary>
    /// 从配置数据初始化武器属性
    /// </summary>
    public void Init(string name, float damage, float fireRate, float reloadTime,
        int magazineSize, float recoilForce, Sprite icon,
        GameObject bulletPrefab, float bulletSpeed, float spread, float range)
    {
        base.Init(name, damage, fireRate, reloadTime, magazineSize, recoilForce, icon);

        this.BulletPrefab = bulletPrefab;
        BulletSpeed = bulletSpeed;
        Spread = spread;
        Range = range;

        Type = WeaponType.Gun;
    }

    public override void Fire(Vector3 direction)
    {
        if (!CanFire)
        {
            return;
        }

        ConsumeAmmo();

        // 应用散布
        float spreadRad = Spread * Mathf.Deg2Rad;
        float randomAngle = Random.Range(-spreadRad, spreadRad);
        Vector3 spreadDir = Quaternion.Euler(0, 0, randomAngle * Mathf.Rad2Deg) * direction;

        SpawnBullet(spreadDir);

        // 自动装填
        if (CurrentAmmo <= 0)
        {
            Reload();
        }
    }

    protected virtual void SpawnBullet(Vector3 direction)
    {
        if (BulletPrefab == null)
        {
            Debug.LogError("[GunWeapon] 子弹预设体为null");
            return;
        }

        // 从对象池获取子弹（字符串 API：prefab名, 位置, 旋转）
        string bulletName = BulletPrefab.name;
        Vector3 spawnPos = _owner.transform.position + direction * 1f;
        GameObject bulletObj = ManagerHub.Pool.Get(bulletName, spawnPos, Quaternion.identity);
        if (bulletObj == null) {
            Debug.LogError("[GunWeapon] 子弹获取失败");
            return;
        }

        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogError($"[GunWeapon] Bullet component not found on pooled object '{bulletName}'.");
            return;
        }
        bullet.Init(Damage, direction, BulletSpeed, Range);

        // 发布事件
        EventCenter.Instance.Publish(GameEventID.Combat_BulletSpawned,
            new BulletSpawnParams { bullet = bullet, weaponType = Type });
    }
}