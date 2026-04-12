using LcIcemFramework.Managers;
using UnityEngine;

/// <summary>
/// 三连发开火模式
/// </summary>
[CreateAssetMenu(fileName = "TripleFirePattern", menuName = "KikoSurge/Fire Pattern/Triple Fire")]
public class TripleFirePattern : FirePattern
{
    [SerializeField]private float _spreadAngle = 15f;
    public override void Fire(GameObject projectile, Transform firePoint)
    {
        for (int i = -1; i <= 1; i++)
        {
            Quaternion spreadRotation = firePoint.rotation * Quaternion.AngleAxis(i * _spreadAngle, Vector3.forward);
            ManagerHub.Pool.Get<Projectile>(projectile.name, firePoint.position, spreadRotation);
        }
    }
}