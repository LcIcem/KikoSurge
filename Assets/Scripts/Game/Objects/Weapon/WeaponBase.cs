using LcIcemFramework.Core;
using UnityEngine;

/// <summary>
/// 武器基类。
/// </summary>
public abstract class WeaponBase
{
    public WeaponType Type { get; protected set; }  // 武器类型
    public float Damage { get; protected set; }     // 基础伤害
    public float FireRate { get; protected set; }   // 射速（秒/发）
    public float ReloadTime { get; protected set; } // 装填时间（秒）
    public int MagazineSize { get; protected set; } // 弹匣容量
    public int CurrentAmmo { get; protected set; }  // 当前弹药
    public bool IsReloading { get; protected set; } // 是否在装填
    public float RecoilForce { get; protected set; } // 后坐力

    protected Player _owner; //武器所有者引用
    protected GameObject _weaponPrefab; // 武器可视化预设体实例

    protected float _fireCooldown;  // 开火冷却
    protected float _reloadTimer;   // 装填时间计时器

    public WeaponBase(Player owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 从配置数据初始化武器属性
    /// </summary>
    public virtual void Init(float damage, float fireRate, float reloadTime,
        int magazineSize, float recoilForce)
    {
        Damage = damage;
        FireRate = fireRate;
        ReloadTime = reloadTime;
        MagazineSize = magazineSize;
        RecoilForce = recoilForce;
        CurrentAmmo = MagazineSize;
    }

    public virtual void Update()
    {
        // 维护装填时间计时器
        _fireCooldown -= Time.deltaTime;

        if (IsReloading)
        {
            _reloadTimer -= Time.deltaTime;
            if (_reloadTimer <= 0f)
            {
                CurrentAmmo = MagazineSize;
                IsReloading = false;
                // 发布装填结束事件
                EventCenter.Instance.Publish(EventID.Combat_Reloaded, this);
            }
        }
    }

    // 开火（由 WeaponHandler 调用）
    public abstract void Fire(Vector3 direction);

    // 开始装填
    public void Reload()
    {
        // 如果正在装填 或 弹匣已满 直接返回
        if (IsReloading || CurrentAmmo == MagazineSize) return;

        // 否则 开始装填
        IsReloading = true;
        _reloadTimer = ReloadTime;
        // 发布装填开始事件
        EventCenter.Instance.Publish(EventID.Combat_Reloading, this);
    }

    /// 能否开火
    public bool CanFire
    {
        get
        {
            return !IsReloading
            && CurrentAmmo > 0
            && _fireCooldown <= 0f;
        }
    }

    /// 消耗弹药
    protected void ConsumeAmmo()
    {
        CurrentAmmo--;
        _fireCooldown = FireRate;
    }

    /// 取消装填
    public void CancelReload()
    {
        if (!IsReloading) return;
        IsReloading = false;
        _reloadTimer = 0f;

        // 发布取消装填事件
        EventCenter.Instance.Publish(EventID.Combat_CancelReload);
    }

    /// <summary>
    /// 设置武器可视化预设体实例
    /// </summary>
    public void SetWeaponPrefab(GameObject prefab)
    {
        _weaponPrefab = prefab;
    }

    /// <summary>
    /// 获取武器可视化预设体实例
    /// </summary>
    public GameObject GetWeaponPrefab()
    {
        return _weaponPrefab;
    }
}