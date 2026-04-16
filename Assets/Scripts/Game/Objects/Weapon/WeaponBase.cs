using LcIcemFramework.Core;
using UnityEngine;
using Game.Event;

/// <summary>
/// 武器基类 - 配置驱动架构
/// 所有武器共用此类，通过 GunConfig 配置不同行为
/// </summary>
public class WeaponBase : MonoBehaviour
{
    /// <summary>
    /// 武器配置（核心）
    /// </summary>
    public GunConfig Config { get; private set; }

    /// <summary>
    /// 武器所有者
    /// </summary>
    public Player Owner { get; private set; }

    /// <summary>
    /// 枪口位置（子弹生成点）
    /// </summary>
    [SerializeField] private Transform _muzzle;

    // 运行时状态
    public int CurrentAmmo { get; private set; }
    public bool IsReloading { get; private set; }
    public bool CanFire => !IsReloading && !_isBursting && CurrentAmmo > 0 && _fireCooldown <= 0f;

    private float _fireCooldown;
    private float _reloadTimer;
    private bool _isBursting;

    private void Awake()
    {
        Owner = GetComponentInParent<Player>();
    }

    /// <summary>
    /// 从配置初始化武器
    /// </summary>
    public void Init(GunConfig config)
    {
        Config = config;
        CurrentAmmo = config.magazineSize;
    }

    /// <summary>
    /// 枪口位置（子弹生成点）
    /// </summary>
    public Transform Muzzle => _muzzle != null ? _muzzle : transform;

    private void Update()
    {
        _fireCooldown -= Time.deltaTime;

        if (IsReloading)
        {
            _reloadTimer -= Time.deltaTime;
            if (_reloadTimer <= 0f)
            {
                CurrentAmmo = Config.magazineSize;
                IsReloading = false;
                EventCenter.Instance.Publish(GameEventID.Combat_Reloaded, this);
                EventCenter.Instance.Publish(GameEventID.OnAmmoChanged, this);
            }
            else
            {
                float progress = 1f - (_reloadTimer / Config.reloadTime);
                EventCenter.Instance.Publish(GameEventID.OnReloadProgress,
                    new ReloadProgressParams { weapon = this, progress = progress });
            }
        }
    }

    /// <summary>
    /// 开火
    /// </summary>
    public void Fire(Vector3 direction)
    {
        if (!CanFire) return;

        // 调用开火模块（弹药消耗由模块内部处理）
        // 冷却由模块内部处理（连发模式需要等全部射出后才开始冷却）
        FireModule.Fire(this);
    }

    /// <summary>
    /// 消耗一颗子弹的弹药
    /// </summary>
    public void ConsumeAmmo()
    {
        CurrentAmmo--;
        EventCenter.Instance.Publish(GameEventID.OnAmmoChanged, this);

        if (CurrentAmmo <= 0)
            Reload();
    }

    /// <summary>
    /// 开始装填
    /// </summary>
    public void Reload()
    {
        if (IsReloading || CurrentAmmo == Config.magazineSize) return;

        IsReloading = true;
        _reloadTimer = Config.reloadTime;
        EventCenter.Instance.Publish(GameEventID.Combat_Reloading, this);
    }

    /// <summary>
    /// 设置开火冷却（由 FireModule 调用）
    /// </summary>
    public void SetFireCooldown(float cooldown)
    {
        _fireCooldown = cooldown;
    }

    /// <summary>
    /// 设置连发状态（由 FireModule 调用）
    /// </summary>
    public void SetBursting(bool bursting)
    {
        _isBursting = bursting;
    }

    /// <summary>
    /// 取消装填
    /// </summary>
    public void CancelReload()
    {
        if (!IsReloading) return;
        IsReloading = false;
        _reloadTimer = 0f;
        EventCenter.Instance.Publish(GameEventID.Combat_CancelReload);
    }
}
