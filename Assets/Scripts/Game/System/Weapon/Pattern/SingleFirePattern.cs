using LcIcemFramework.Managers;
using UnityEngine;

/// <summary>
/// 单发开火模式
/// </summary>
[CreateAssetMenu(fileName = "SingleFirePattern", menuName = "KikoSurge/Fire Pattern/Single Fire")]
public class SingleFirePattern : FirePattern
{
    public override void Fire(GameObject projectile, Transform firePoint)
    {
        Projectile bullet = ManagerHub.Pool.Get<Projectile>(projectile.name, firePoint.position, firePoint.rotation);
    }
}