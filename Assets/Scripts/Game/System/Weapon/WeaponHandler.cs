using UnityEngine;

/// <summary>
/// 武器处理器：管理玩家当前武器，执行射击和切换。
/// </summary>
public class WeaponHandler
{
    private Player _owner;
    private Animator _animator;
    private WeaponBase _currentWeapon;

    public WeaponBase CurrentWeapon => _currentWeapon;

    public WeaponHandler(Player owner, Animator animator)
    {
        _owner = owner;
        _animator = animator;
    }

    /// <summary>
    /// 初始化武器（由 Player 在 Start 时调用）
    /// </summary>
    public void Initialize(WeaponBase weapon)
    {
        _currentWeapon = weapon;
    }

    /// <summary>
    /// 执行射击
    /// </summary>
    public void Fire(Vector3 direction)
    {
        _currentWeapon?.Fire(direction);
    }

    /// <summary>
    /// 切换武器
    /// </summary>
    public void SwitchWeapon(WeaponBase newWeapon)
    {
        _currentWeapon = newWeapon;
    }

    /// <summary>
    /// 每帧更新（用于维护武器状态，如冷却、装填）
    /// </summary>
    public void Update()
    {
        _currentWeapon?.Update();
    }
}
