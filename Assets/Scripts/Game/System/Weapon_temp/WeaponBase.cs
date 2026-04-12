using UnityEngine;

/// <summary>
/// 武器基类。
/// </summary>
public abstract class WeaponBase
{
    public WeaponType Type { get; protected set; }  //武器类型
    public float Damage { get; protected set; }     //基础伤害
    public float FireRate { get; protected set; }   //射速（秒/发）
    public float ReloadTime { get; protected set; } //装填时间（秒）
    public int MagazineSize { get; protected set; } //弹匣容量
    public int CurrentAmmo { get; protected set; }  //当前弹药
    public bool IsReloading { get; protected set; } //是否在装填

    protected Player _owner; //武器所有者引用
    protected Animator _animator;   //武器所有者 Animator（驱动射击动画）

    protected float _fireCooldown;  // 开火冷却
    protected float _reloadTimer;   // 装填时间计时器

    public WeaponBase(Player owner, Animator animator)
    {
        _owner = owner;
        _animator = animator;
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
        // 播放装填动画
        _animator?.SetTrigger("Reload");
    }

    /// 能否开火
    protected bool CanFire()
    {
        return !IsReloading
            && CurrentAmmo > 0
            && _fireCooldown <= 0f;
    }

    /// 消耗弹药
    protected void ConsumeAmmo()
    {
        CurrentAmmo--;
        _fireCooldown = FireRate;
    }
}