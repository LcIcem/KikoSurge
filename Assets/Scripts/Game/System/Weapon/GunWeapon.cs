using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using UnityEngine;

/// <summary>
/// 枪械武器（单发射击）。
/// </summary>
public class GunWeapon : WeaponBase
{
    public GameObject BulletPrefab { get; set; }    // 子弹 Prefab
    public float BulletSpeed { get; set; } = 20f;   // 子弹速度
    public float Spread { get; set; } = 2f;         // 散布角度（度）
    public float Range { get; set; } = 50f;         // 射程

    public GunWeapon(Player owner) : base(owner)
    {
        Type = WeaponType.Gun;
        Damage = 10f;
        FireRate = 0.2f;
        ReloadTime = 1.5f;
        MagazineSize = 30;
        CurrentAmmo = MagazineSize;
    }

    public override void Fire(Vector3 direction)
    {
        if (!CanFire) return;

        ConsumeAmmo();

        // 应用散布
        float spreadRad = Spread * Mathf.Deg2Rad;
        float randomAngle = Random.Range(-spreadRad, spreadRad);
        Vector3 spreadDir = Quaternion.Euler(0, 0, randomAngle * Mathf.Rad2Deg) * direction;

        SpawnBullet(spreadDir);

        // 自动装填
        if (CurrentAmmo <= 0)
            Reload();
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
        EventCenter.Instance.Publish(EventID.Combat_BulletSpawned,
            new BulletSpawnParams { bullet = bullet, weaponType = Type });
    }
}