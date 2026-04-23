using LcIcemFramework;
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
    public static void Spawn(WeaponBase gun, float angleOffset, float randomSpread, string ownerTag)
    {
        Spawn(gun, angleOffset, randomSpread, ownerTag, null);
    }

    /// <summary>
    /// 创建子弹（带玩家数据，用于伤害计算）
    /// </summary>
    public static void Spawn(WeaponBase gun, float angleOffset, float randomSpread, string ownerTag, PlayerRuntimeData playerData)
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

        // 设置子弹归属
        bullet.SetOwnerTag(ownerTag);

        // 组装伤害参数
        BulletDamageParams damageParams = null;
        if (playerData != null && gun.Config != null)
        {
            damageParams = new BulletDamageParams
            {
                bulletBaseDamage = config.baseDamage + playerData.atk + gun.Config.weaponDamage,
                playerCritRate = playerData.critRate,
                playerCritMultiplier = playerData.critMultiplier,
                playerDamageBonus = playerData.damageBonus,
                playerDefBreak = playerData.defBreak,
                weaponDamage = 0f,  // weaponDamage 已合并到 bulletBaseDamage
                weaponCritRate = gun.Config.weaponCritRate,
                weaponCritMultiplier = gun.Config.weaponCritMultiplier,
                weaponDamageBonus = gun.Config.weaponDamageBonus
            };
        }

        bullet.Init(config, dir, damageParams, gun.Config.penetrateCount);
    }

    /// <summary>
    /// 更新子弹移动（由 Bullet.Update 每帧调用）
    /// 直线和追踪子弹使用 Rigidbody2D.MovePosition 物理引擎驱动
    /// 抛物线子弹由 Rigidbody2D 物理引擎自动处理（velocity + gravityScale）
    /// </summary>
    public static void Move(Bullet bullet)
    {
        // 抛物线子弹由物理引擎自动处理（velocity + gravityScale），不需要 MovePosition
        if (bullet.BulletType == BulletType.Parabola)
            return;

        switch (bullet.BulletType)
        {
            case BulletType.Straight:
                MoveStraight(bullet);
                break;
            case BulletType.Homing:
                MoveHoming(bullet);
                break;
        }

        // 超距检测
        if (bullet.IsExceedMaxDistance)
        {
            ManagerHub.Pool.Release(bullet.gameObject);
        }
    }

    /// <summary>
    /// 直线移动 - 使用物理引擎 MovePosition
    /// </summary>
    private static void MoveStraight(Bullet bullet)
    {
        Vector3 nextPos = bullet.transform.position + bullet.Direction * bullet.Speed * Time.deltaTime;
        bullet.Rigidbody.MovePosition(nextPos);
    }

    /// <summary>
    /// 追踪移动 - 使用物理引擎 MovePosition
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

        Vector3 nextPos = bullet.transform.position + bullet.CurrentDir * bullet.Speed * Time.deltaTime;
        bullet.Rigidbody.MovePosition(nextPos);

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
