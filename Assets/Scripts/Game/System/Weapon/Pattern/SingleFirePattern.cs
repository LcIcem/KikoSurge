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
        Rigidbody2D rb = bullet?.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.right * bullet.Speed;
        }
    }
}