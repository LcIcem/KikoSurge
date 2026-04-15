using UnityEngine;
using LcIcemFramework.Managers;

/// <summary>
/// 开火模块 - 根据开火模式决定发射几发、怎么发
/// </summary>
public static class FireModule
{
    /// <summary>
    /// 执行开火（由 WeaponBase.Fire 调用）
    /// WeaponBase 已经检查过 CanFire
    /// 冷却由各模式自行处理（连发模式需等全部射出后才开始冷却）
    /// </summary>
    public static void Fire(WeaponBase gun)
    {
        GunConfig config = gun.Config;

        switch (config.fireMode)
        {
            case FireMode.Single:
                FireSingle(gun);
                // 单发：立即设置冷却
                gun.SetFireCooldown(config.fireRate);
                break;
            case FireMode.Spread:
                FireSpread(gun);
                // 散射：立即设置冷却
                gun.SetFireCooldown(config.fireRate);
                break;
            case FireMode.Burst:
                FireBurst(gun);
                // 连发：冷却由 FireBurst 内部在最后一发射出后设置
                break;
            case FireMode.Continuous:
                FireSingle(gun);
                // 持续模式：立即设置冷却
                gun.SetFireCooldown(config.fireRate);
                break;
            case FireMode.Charge:
                FireSingle(gun);
                // 蓄力模式：立即设置冷却
                gun.SetFireCooldown(config.fireRate);
                break;
        }
    }

    /// <summary>
    /// 单发
    /// </summary>
    private static void FireSingle(WeaponBase gun)
    {
        GunConfig config = gun.Config;

        // 消耗弹药并发射
        gun.ConsumeAmmo();
        BulletModule.Spawn(gun, 0, config.randomSpreadAngle);
    }

    /// <summary>
    /// 散射（霰弹）
    /// </summary>
    private static void FireSpread(WeaponBase gun)
    {
        GunConfig config = gun.Config;
        int bulletCount = config.bulletCount;
        float shotgunAngle = config.shotgunSpreadAngle;
        float randomSpread = config.randomSpreadAngle;

        // 如果只有一颗子弹，直接发射
        if (bulletCount <= 1)
        {
            FireSingle(gun);
            return;
        }

        // 扇形分布子弹，同时每颗应用随机散布
        float step = shotgunAngle * 2f / (bulletCount - 1);
        float startAngle = -shotgunAngle;

        gun.ConsumeAmmo();
        
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = startAngle + step * i;
            // 霰弹的每颗子弹都有随机散布
            BulletModule.Spawn(gun, angle, randomSpread);
        }
    }

    /// <summary>
    /// 连发（三连发等）- 带延迟，每颗子弹检查并消耗弹药
    /// 冷却在最后一发射出后才开始计算
    /// </summary>
    private static void FireBurst(WeaponBase gun)
    {
        GunConfig config = gun.Config;
        float burstSpeed = config.burstSpeed;
        float randomSpread = config.randomSpreadAngle;
        int burstCount = config.burstCount;

        // 进入连发状态，阻止长按期间再次触发开火
        gun.SetBursting(true);

        // 第一发立即发射
        if (gun.CurrentAmmo > 0)
        {
            gun.ConsumeAmmo();
            BulletModule.Spawn(gun, 0, randomSpread);
        }

        // 后续子弹带延迟，最后一发射出后设置冷却并解除连发状态
        for (int i = 1; i < burstCount; i++)
        {
            float delay = burstSpeed * i;
            bool isLastShot = (i == burstCount - 1);
            ManagerHub.Timer.AddTimeOut(delay, () =>
            {
                if (gun.CurrentAmmo <= 0)
                {
                    gun.SetBursting(false);
                    return;
                }
                gun.ConsumeAmmo();
                BulletModule.Spawn(gun, 0, randomSpread);

                if (isLastShot)
                {
                    gun.SetFireCooldown(config.fireRate);
                    gun.SetBursting(false);
                }
            });
        }

        // burstCount == 1 时上面的循环不执行，立即设置冷却并解除连发状态
        if (burstCount <= 1)
        {
            gun.SetFireCooldown(config.fireRate);
            gun.SetBursting(false);
        }
    }
}
