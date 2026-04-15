using LcIcemFramework.Managers;
using UnityEngine;

/// <summary>
/// 弹道模块 - 负责创建子弹和处理子弹移动
/// 根据弹道类型决定子弹怎么飞
/// </summary>
public static class BulletModule
{
    /// <summary>
    /// 创建子弹（统一入口，内部判断弹道类型）
    /// </summary>
    public static void Spawn(WeaponBase gun, float angleOffset, float randomSpread)
    {
        BulletConfig config = gun.Config.bulletConfig;
        if (config == null) return;

        Transform muzzle = gun.Muzzle;

        // 计算带随机散布的方向
        float spreadRad = Random.Range(-randomSpread, randomSpread) * Mathf.Deg2Rad;
        float totalAngle = angleOffset + spreadRad;
        Vector3 dir = gun.transform.rotation * Quaternion.Euler(0, 0, totalAngle) * Vector3.right;
        Vector3 spawnPos = muzzle != null ? muzzle.position : gun.transform.position;

        // 从对象池获取子弹
        GameObject bulletObj = ManagerHub.Pool.Get(config.bulletPrefab, spawnPos, Quaternion.identity);
        if (bulletObj == null) return;

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null) return;

        bullet.Init(config, dir);
    }

    /// <summary>
    /// 更新子弹移动（由 Bullet.Update 每帧调用）
    /// </summary>
    public static void Move(Bullet bullet)
    {
        switch (bullet.BulletType)
        {
            case BulletType.Straight:
                MoveStraight(bullet);
                break;
            case BulletType.Parabola:
                MoveParabola(bullet);
                break;
            case BulletType.Homing:
                MoveHoming(bullet);
                break;
        }

        // 超距检测
        if (Vector3.Distance(bullet.transform.position, bullet.SpawnPos) > bullet.MaxDistance)
        {
            ManagerHub.Pool.Release(bullet.gameObject);
        }
    }

    /// <summary>
    /// 直线移动
    /// </summary>
    private static void MoveStraight(Bullet bullet)
    {
        bullet.transform.position += bullet.Direction * bullet.Speed * Time.deltaTime;
    }

    /// <summary>
    /// 抛物线移动
    /// </summary>
    private static void MoveParabola(Bullet bullet)
    {
        bullet.transform.position += bullet.Direction * bullet.Speed * Time.deltaTime;
    }

    /// <summary>
    /// 追踪移动（持续检测范围内目标，有目标则转向）
    /// </summary>
    private static void MoveHoming(Bullet bullet)
    {
        // 持续检测范围内最近敌人
        Transform target = FindNearestEnemy(bullet.transform.position, bullet.HomingRange);

        if (target != null)
        {
            // 有目标，转向目标
            Vector3 targetDir = (target.position - bullet.transform.position).normalized;
            bullet.CurrentDir = Vector3.Lerp(bullet.CurrentDir, targetDir, bullet.HomingStrength * Time.deltaTime).normalized;
        }

        // 按当前方向移动
        bullet.transform.position += bullet.CurrentDir * bullet.Speed * Time.deltaTime;

        // 朝移动方向旋转
        float angle = Mathf.Atan2(bullet.CurrentDir.y, bullet.CurrentDir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 查找最近敌人
    /// </summary>
    private static Transform FindNearestEnemy(Vector3 from, float range)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(from, range);
        if (hits.Length == 0) return null;

        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            // 使用 tag 判断是否是敌人
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(from, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hit.transform;
                }
            }
        }

        return nearest;
    }
}
