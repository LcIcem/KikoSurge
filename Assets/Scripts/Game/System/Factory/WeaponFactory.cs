using UnityEngine;
using LcIcemFramework.Managers;
using UnityEngine.Events;

/// <summary>
/// 武器工厂：负责根据配置创建武器实例
/// </summary>
public class WeaponFactory
{
    /// <summary>
    /// 根据武器配置创建武器实例
    /// </summary>
    public void CreateWeapon(WeaponDefBase config, Player owner, UnityAction<WeaponBase> onCreated)
    {
        if (config == null)
        {
            Debug.LogError("[WeaponFactory] 武器配置为 null");
            onCreated?.Invoke(null);
            return;
        }

        // 加载武器可视化预设体
        LoadWeaponPrefab(config.WeaponPrefabAddress, owner, weaponObj =>
        {
            // 加载子弹预设体
            LoadBulletPrefab(config.BulletPrefabAddress, bulletObj =>
            {
                var weapon = CreateWeaponCore(config, owner, bulletObj);

                // 设置武器可视化预设体
                if (weaponObj != null)
                {
                    weapon.SetWeaponPrefab(weaponObj);
                }

                onCreated?.Invoke(weapon);
            });
        });
    }

    /// <summary>
    /// 加载武器可视化预设体并实例化 到 玩家的WeaponPivot
    /// </summary>
    private void LoadWeaponPrefab(string address, Player owner, UnityAction<GameObject> onLoaded)
    {
        if (string.IsNullOrEmpty(address))
        {
            onLoaded?.Invoke(null);
            return;
        }

        ManagerHub.Addressables.LoadAsync<GameObject>(address, weaponObj =>
        {
            if (weaponObj == null)
            {
                onLoaded?.Invoke(null);
                return;
            }

            // 实例化武器预设体并挂在到玩家的 weaponPivot
            Transform weaponPivot = owner.transform.Find("WeaponPivot");
            if (weaponPivot == null)
            {
                Debug.LogWarning("[WeaponFactory] 玩家身上未找到 WeaponPivot");
                onLoaded?.Invoke(null);
                return;
            }

            var instance = UnityEngine.Object.Instantiate(weaponObj, weaponPivot);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(false); // 默认隐藏，装备时显示

            onLoaded?.Invoke(instance);
        });
    }

    /// <summary>
    /// 加载子弹预设体
    /// </summary>
    private void LoadBulletPrefab(string address, UnityAction<GameObject> onLoaded)
    {
        if (string.IsNullOrEmpty(address))
        {
            onLoaded?.Invoke(null);
            return;
        }

        ManagerHub.Addressables.LoadAsync<GameObject>(address, bullet =>
        {
            onLoaded?.Invoke(bullet);
        });
    }

    /// <summary>
    /// 根据配置创建武器实例
    /// </summary>
    private WeaponBase CreateWeaponCore(WeaponDefBase config, Player owner, GameObject bulletPrefab)
    {
        WeaponBase weapon = null;

        switch (config)
        {
            case GunWeaponDef_SO gun:
                var gunWeapon = new GunWeapon(owner);
                gunWeapon.Init(gun.WeaponName, gun.Damage, gun.FireRate, gun.ReloadTime,
                    gun.MagazineSize, gun.RecoilForce, gun.Icon,
                    bulletPrefab, gun.BulletSpeed, gun.Spread, gun.Range);
                weapon = gunWeapon;
                break;

            case ShotgunWeaponDef_SO shotgun:
                var shotgunWeapon = new ShotgunWeapon(owner);
                shotgunWeapon.Init(shotgun.WeaponName, shotgun.Damage, shotgun.FireRate, shotgun.ReloadTime,
                    shotgun.MagazineSize, shotgun.RecoilForce, shotgun.Icon,
                    bulletPrefab, shotgun.BulletSpeed,
                    shotgun.PelletCount, shotgun.SpreadAngle, shotgun.FalloffStart, shotgun.Range);
                weapon = shotgunWeapon;
                break;

            default:
                Debug.LogError($"[WeaponFactory] 未知的武器配置类型: {config.GetType()}");
                return null;
        }

        return weapon;
    }
}
