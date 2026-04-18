using System.Collections.Generic;
using UnityEngine;
using LcIcemFramework.Core;
using Game.Event;

/// <summary>
/// 武器处理器：管理玩家当前武器，执行射击和切换。
/// </summary>
public class WeaponHandler
{
    private Player _owner;
    private List<WeaponBase> _weapons = new();
    private int _currentWeaponIndex = -1; // -1 表示未装备任何武器

    public WeaponBase CurrentWeapon => _currentWeaponIndex >= 0 ? _weapons[_currentWeaponIndex] : null;
    public int CurrentWeaponIndex => _currentWeaponIndex;
    public int WeaponCount => _weapons.Count;

    public WeaponHandler(Player owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 初始化武器（由 Player 调用，从 RoleStaticData 配置）
    /// </summary>
    public void InitializeWeapons(List<int> weaponIds)
    {
        foreach (var weaponId in weaponIds)
        {
            CreateWeapon(weaponId);
        }
    }

    /// <summary>
    /// 根据 Id 创建武器（玩家装备，不走对象池）
    /// </summary>
    public void CreateWeapon(int weaponId)
    {
        WeaponFactory.Instance.Create(weaponId, _owner.WeaponPivot, (weapon) =>
        {
            if (weapon == null) return;
            AddWeapon(weapon);
        });
    }

    /// <summary>
    /// 添加武器到列表
    /// </summary>
    public void AddWeapon(WeaponBase weapon)
    {
        _weapons.Add(weapon);

        // 如果当前没有装备武器，自动装备第一把
        if (_currentWeaponIndex < 0)
        {
            EquipWeaponInternal(0);
        }
    }

    /// <summary>
    /// 装备指定索引的武器
    /// </summary>
    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Count) return;
        EquipWeaponInternal(index);
    }

    /// <summary>
    /// 内部装备方法，负责切换逻辑
    /// </summary>
    private void EquipWeaponInternal(int index)
    {
        // 隐藏当前武器预设体
        if (_currentWeaponIndex >= 0)
        {
            var previousWeapon = _weapons[_currentWeaponIndex];
            previousWeapon.gameObject.SetActive(false);

            if (previousWeapon.IsReloading)
                previousWeapon.CancelReload();
        }

        _currentWeaponIndex = index;

        // 显示新武器预设体
        var newWeapon = _weapons[index];
        newWeapon.gameObject.SetActive(true);

        if (newWeapon.CurrentAmmo <= 0)
            newWeapon.Reload();

        // 发布武器切换事件
        EventCenter.Instance.Publish(GameEventID.OnCurrentWeaponChanged, newWeapon);
    }

    /// <summary>
    /// 切换到下一把武器
    /// </summary>
    public void SwitchToNextWeapon()
    {
        if (_weapons.Count <= 1) return;
        int nextIndex = (_currentWeaponIndex + 1) % _weapons.Count;
        EquipWeapon(nextIndex);
    }

    /// <summary>
    /// 执行射击
    /// </summary>
    public void Fire(Vector3 direction)
    {
        CurrentWeapon?.Fire(direction);
    }

    /// <summary>
    /// 清理所有武器（切换层或游戏结束时调用）
    /// </summary>
    public void ClearAllWeapons()
    {
        foreach (var weapon in _weapons)
        {
            if (weapon == null) continue;
            WeaponFactory.Instance.Release(weapon);
        }
        _weapons.Clear();
        _currentWeaponIndex = -1;
    }
}
