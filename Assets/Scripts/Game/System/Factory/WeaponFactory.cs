using UnityEngine;
using UnityEngine.Events;
using LcIcemFramework.Core;
using LcIcemFramework;

/// <summary>
/// 武器工厂：根据武器Id创建武器实例
/// </summary>
public class WeaponFactory : SingletonMono<WeaponFactory>
{
    protected override void Init()
    {
        // 懒初始化，无需额外操作
    }

    /// <summary>
    /// 创建武器（不走对象池，用于玩家装备）
    /// </summary>
    /// <param name="weaponId">武器Id</param>
    /// <param name="parent">父对象Transform</param>
    /// <param name="onCreated">创建完成回调</param>
    public void Create(int weaponId, Transform parent, UnityAction<WeaponBase> onCreated)
    {
        GunConfig config = GetWeaponConfig(weaponId);
        if (config == null)
        {
            onCreated?.Invoke(null);
            return;
        }

        if (config.gunPrefab == null)
        {
            Debug.LogError($"[WeaponFactory] 武器配置 {config.gunName} 没有指定预设体");
            onCreated?.Invoke(null);
            return;
        }

        var weaponObj = Object.Instantiate(config.gunPrefab, parent);
        weaponObj.SetActive(false);

        var weapon = weaponObj.GetComponent<WeaponBase>();
        if (weapon == null)
        {
            Debug.LogError($"[WeaponFactory] 武器预设体 {config.gunName} 上没有 WeaponBase 组件");
            onCreated?.Invoke(null);
            return;
        }

        weapon.Init(config);
        onCreated?.Invoke(weapon);
    }

    /// <summary>
    /// 创建武器（走对象池，用于掉落物）
    /// </summary>
    /// <param name="weaponId">武器Id</param>
    /// <param name="position">生成位置</param>
    /// <param name="onCreated">创建完成回调</param>
    public void CreateByPool(int weaponId, Vector3 position, UnityAction<WeaponBase> onCreated)
    {
        GunConfig config = GetWeaponConfig(weaponId);
        if (config == null)
        {
            onCreated?.Invoke(null);
            return;
        }

        if (config.gunPrefab == null)
        {
            Debug.LogError($"[WeaponFactory] 武器配置 {config.gunName} 没有指定预设体");
            onCreated?.Invoke(null);
            return;
        }

        WeaponBase weapon = ManagerHub.Pool.Get<WeaponBase>(
            config.gunPrefab, position, Quaternion.identity);

        if (weapon == null)
        {
            Debug.LogError($"[WeaponFactory] 从池获取武器失败: {config.gunName}");
            onCreated?.Invoke(null);
            return;
        }

        weapon.Init(config);
        onCreated?.Invoke(weapon);
    }

    /// <summary>
    /// 释放武器到对象池
    /// </summary>
    public void Release(WeaponBase weapon)
    {
        if (weapon == null) return;
        ManagerHub.Pool.Release(weapon.gameObject);
    }

    private GunConfig GetWeaponConfig(int weaponId)
    {
        GunConfig config = GameDataManager.Instance.GetWeaponConfig(weaponId);
        if (config == null)
        {
            Debug.LogError($"[WeaponFactory] 未找到武器配置: Id={weaponId}");
            return null;
        }
        return config;
    }
}
