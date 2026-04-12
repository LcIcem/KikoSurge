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

    public GunWeapon(Player owner, Animator animator) : base(owner, animator)
    {
        Type = WeaponType.Gun;
        Damage = 10f;
        FireRate = 0.15f;
        ReloadTime = 1.5f;
        MagazineSize = 30;
        CurrentAmmo = MagazineSize;
    }

    public override void Fire(Vector3 direction)
    {
        if (!CanFire()) return;

        ConsumeAmmo();

        // 驱动射击动画（由 AnimatorController 播放）
        _animator?.SetTrigger("Fire");

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
        // 从对象池获取子弹（字符串 API：prefab名, 位置, 旋转）
        string bulletName = BulletPrefab != null ? BulletPrefab.name : "Bullet";

        Vector3 spawnPos = _owner.transform.position + direction * 1f;
        GameObject bulletObj = ManagerHub.Pool.Get(bulletName, spawnPos, Quaternion.identity);
        var bullet = bulletObj.GetComponent<Bullet>();
        bullet.Init(bulletName, Damage, direction, BulletSpeed, Range);

        // 发布事件
        EventCenter.Instance.Publish(EventID.Combat_BulletSpawned,
            new BulletSpawnParams { bullet = bullet, weaponType = Type });
    }
}