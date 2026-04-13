using LcIcemFramework.Managers;
using UnityEngine;

/// <summary>
/// 霰弹枪武器（多发散射）。
/// </summary>
public class ShotgunWeapon : GunWeapon
{
    /// <summary>弹丸数量</summary>
    public int PelletCount { get; set; } = 6;

    /// <summary>散布总角度（度）</summary>
    public float SpreadAngle { get; set; } = 30f;

    /// <summary>伤害衰减开始距离</summary>
    public float FalloffStart { get; set; } = 5f;

    public ShotgunWeapon(Player owner) : base(owner)
    {
        Type = WeaponType.Shotgun;
        Damage = 8f;
        FireRate = 0.8f;
        ReloadTime = 2.5f;
        MagazineSize = 6;
        CurrentAmmo = MagazineSize;
        PelletCount = 6;
        SpreadAngle = 30f;
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
            pellet.Init(Damage, pelletDir, BulletSpeed, Range);
        }
    }
}