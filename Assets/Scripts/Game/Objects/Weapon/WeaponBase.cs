using LcIcemFramework;
using LcIcemFramework.Core;
using UnityEngine;
using Game.Event;

/// <summary>
/// 武器基类 - 配置驱动架构
/// 所有武器共用此类，通过 WeaponConfig 配置不同行为
/// 实现 IPoolable 支持对象池
/// </summary>
public class WeaponBase : MonoBehaviour, IPoolable
{
    /// <summary>
    /// 武器配置（核心）
    /// </summary>
    public WeaponConfig Config { get; private set; }

    /// <summary>
    /// 武器所有者
    /// </summary>
    public Player Owner { get; private set; }

    /// <summary>
    /// 枪口位置（子弹生成点）
    /// </summary>
    [SerializeField] private Transform _muzzle;

    /// <summary>
    /// 射击音效
    /// </summary>
    [SerializeField] private AudioClip _shootSFX;

    /// <summary>
    /// 武器动画控制器
    /// </summary>
    [SerializeField] private Animator _weaponAnimator;

    /// <summary>
    /// 换弹音效（音效时长应与武器的 reloadTime 一致）
    /// </summary>
    [SerializeField] private AudioClip _reloadSFX;

    // 运行时状态
    public int CurrentAmmo { get; private set; }

    /// <summary>
    /// 设置当前弹药（由 WeaponHandler 在切换武器时恢复弹药使用）
    /// </summary>
    public void SetAmmo(int ammo) => CurrentAmmo = ammo;
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
    public void Init(WeaponConfig config)
    {
        Config = config;
        CurrentAmmo = config.magazineSize;
    }

    /// <summary>
    /// 从会话数据恢复弹药状态
    /// </summary>
    public void RestoreAmmo(int ammo)
    {
        CurrentAmmo = ammo;
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
                if (_weaponAnimator != null)
                    _weaponAnimator.SetBool("isReload", false);
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

        // 播放射击音效
        if (_shootSFX != null)
            ManagerHub.Audio.PlaySFX(_shootSFX);

        // 播放射击动画
        if (_weaponAnimator != null)
            _weaponAnimator.SetTrigger("shoot");

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

        // 播放换弹音效
        if (_reloadSFX != null)
            ManagerHub.Audio.PlaySFX(_reloadSFX);

        // 播放换弹动画（用 bool 保持到换弹结束）
        if (_weaponAnimator != null)
            _weaponAnimator.SetBool("isReload", true);

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
        if (_weaponAnimator != null)
            _weaponAnimator.SetBool("isReload", false);
        EventCenter.Instance.Publish(GameEventID.Combat_CancelReload);
    }

    // ========== IPoolable 实现 ==========

    /// <summary>
    /// 对象池取出时重置运行时状态（由 PoolManager actionOnGet 调用）
    /// </summary>
    public void OnSpawn()
    {
        // 重置所有运行时状态，防止池化残留状态干扰
        _fireCooldown = 0f;
        _reloadTimer = 0f;
        _isBursting = false;
        IsReloading = false;
        // CurrentAmmo 和 Config 由 WeaponFactory.CreateByPool 的 Init() 设置，此处不覆盖
    }

    /// <summary>
    /// 对象池归还时清理（由 PoolManager actionOnRelease 调用）
    /// </summary>
    public void OnDespawn()
    {
        // 归还池时取消所有运行时状态
        _fireCooldown = 0f;
        _reloadTimer = 0f;
        _isBursting = false;
        IsReloading = false;
    }
}
