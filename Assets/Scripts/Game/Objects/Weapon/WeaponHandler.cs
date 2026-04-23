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
    /// 切换到下一把武器（直接从 SessionData 读取并更新）
    /// </summary>
    public void SwitchToNextWeapon()
    {
        var sessionData = SessionManager.Instance?.CurrentSession;
        if (sessionData == null)
            return;

        var equippedSlots = sessionData.equippedWeaponSlots;
        if (equippedSlots == null || equippedSlots.Count == 0)
            return;

        // 找到当前正在使用的武器索引（第一个非空槽）
        int currentEquipIndex = -1;
        for (int i = 0; i < equippedSlots.Count; i++)
        {
            if (!equippedSlots[i].IsEmpty)
            {
                currentEquipIndex = i;
                break;
            }
        }

        if (currentEquipIndex < 0)
            return;

        // 找下一个非空槽
        int nextIndex = -1;
        for (int i = 1; i < equippedSlots.Count; i++)
        {
            int checkIndex = (currentEquipIndex + i) % equippedSlots.Count;
            if (!equippedSlots[checkIndex].IsEmpty)
            {
                nextIndex = checkIndex;
                break;
            }
        }

        if (nextIndex < 0 || nextIndex == currentEquipIndex)
            return;

        // 保存当前武器的弹药到 ItemSlotData
        if (CurrentWeapon != null)
            equippedSlots[currentEquipIndex].ammo = CurrentWeapon.CurrentAmmo;

        // 交换 SessionData 中的两个槽位
        var temp = equippedSlots[currentEquipIndex];
        equippedSlots[currentEquipIndex] = equippedSlots[nextIndex];
        equippedSlots[nextIndex] = temp;

        // 同步更新 WeaponHandler 内部的武器列表
        SyncWeaponListFromSession(equippedSlots);

        // 发布事件通知 UI 更新
        EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
            new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Swap));

        Debug.Log($"[WeaponHandler] SwitchToNextWeapon: swapped slot {currentEquipIndex} with {nextIndex}");
    }

    /// <summary>
    /// 根据 SessionData 同步内部武器列表
    /// </summary>
    private void SyncWeaponListFromSession(List<ItemSlotData> equippedSlots)
    {
        // 取消当前武器的换弹状态，防止切换后卡在 Reload 状态
        if (_currentWeaponIndex >= 0 && _currentWeaponIndex < _weapons.Count)
        {
            var currentWeapon = _weapons[_currentWeaponIndex];
            if (currentWeapon != null && currentWeapon.IsReloading)
                currentWeapon.CancelReload();
        }

        // 收集当前非空槽的 itemId 列表（按槽位顺序）
        var currentWeaponIds = new List<int>();
        foreach (var slot in equippedSlots)
        {
            if (!slot.IsEmpty)
                currentWeaponIds.Add(slot.itemId);
        }

        // 根据 itemId 重新排序 _weapons 列表，使其与 equippedSlots 顺序一致
        _weapons.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int indexA = currentWeaponIds.IndexOf(a.Config.itemConfig.Id);
            int indexB = currentWeaponIds.IndexOf(b.Config.itemConfig.Id);
            return indexA.CompareTo(indexB);
        });

        // 隐藏所有武器
        foreach (var weapon in _weapons)
        {
            if (weapon != null)
                weapon.gameObject.SetActive(false);
        }

        // 激活第一个非空槽的武器
        _currentWeaponIndex = 0;
        for (int i = 0; i < equippedSlots.Count; i++)
        {
            if (!equippedSlots[i].IsEmpty && _currentWeaponIndex < _weapons.Count)
            {
                var weapon = _weapons[_currentWeaponIndex];
                weapon.gameObject.SetActive(true);
                // 从 ItemSlotData 恢复弹药
                weapon.SetAmmo(equippedSlots[i].ammo);
                break;
            }
            if (!equippedSlots[i].IsEmpty)
                _currentWeaponIndex++;
        }

        if (_currentWeaponIndex >= 0 && _currentWeaponIndex < _weapons.Count)
        {
            EventCenter.Instance.Publish(GameEventID.OnCurrentWeaponChanged, _weapons[_currentWeaponIndex]);
        }

        Debug.Log($"[WeaponHandler] SyncWeaponListFromSession: {_weapons.Count} weapons synced");
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

    /// <summary>
    /// 从 SessionData 同步武器列表（当背包中武器切换时调用）
    /// </summary>
    public void SyncFromSessionData(List<int> equippedWeaponIds, Transform weaponPivot)
    {
        // 清空现有武器
        ClearAllWeapons();

        // 根据 equippedWeaponIds 重新创建武器
        foreach (var weaponId in equippedWeaponIds)
        {
            if (weaponId <= 0) continue;
            WeaponFactory.Instance.Create(weaponId, weaponPivot, (weapon) =>
            {
                if (weapon == null) return;
                AddWeapon(weapon);
            });
        }

        Debug.Log($"[WeaponHandler] Synced weapons from SessionData: {equippedWeaponIds.Count} weapons");
    }

    /// <summary>
    /// 从 SessionData 同步武器列表（带 ItemSlotData 以恢复弹药）
    /// </summary>
    public void SyncFromSessionData(List<ItemSlotData> equippedSlots, Transform weaponPivot)
    {
        // 清空现有武器
        ClearAllWeapons();

        // 根据 equippedSlots 重新创建武器
        foreach (var slot in equippedSlots)
        {
            if (slot.IsEmpty) continue;
            WeaponFactory.Instance.Create(slot.itemId, weaponPivot, (weapon) =>
            {
                if (weapon == null) return;
                // 先设置弹药（必须在 AddWeapon 之前，因为 AddWeapon 会触发 EquipWeaponInternal）
                weapon.SetAmmo(slot.ammo);
                AddWeapon(weapon);
            });
        }

        Debug.Log($"[WeaponHandler] Synced weapons from SessionData: {equippedSlots.Count} slots");
    }
}
