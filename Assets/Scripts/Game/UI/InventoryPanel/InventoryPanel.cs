using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using LcIcemFramework;
using LcIcemFramework.Core;
using Game.Event;

/// <summary>
/// 背包面板
/// <para>显示角色基础数值、所有类型背包，通过页签切换，ScrollView 无限滚动</para>
/// </summary>
public class InventoryPanel : BasePanel
{
    #region 控件名称常量

    private const string BTN_CLOSE = "btn_close";
    private const string IMG_ROLE_ICON = "img_role_icon";
    private const string TXT_ROLE_NAME = "txt_role_name";
    private const string TXT_HEALTH = "txt_health";
    private const string TXT_ATK = "txt_atk";
    private const string TXT_DEF = "txt_def";
    private const string TXT_SPEED = "txt_speed";
    private const string TXT_DASH_SPEED = "txt_dashSpeed";
    private const string TXT_DASH_DURATION = "txt_dashDuration";
    private const string TXT_DASH_GAP = "txt_dashGap";
    private const string TXT_INVINCIBLE = "txt_invincible";
    private const string TXT_HURT_DURATION = "txt_hurtDuration";
    private const string TAB_WEAPON = "tog_tab_weapon";
    private const string TAB_AMMO = "tog_tab_ammo";
    private const string TAB_POTION = "tog_tab_potion";
    private const string TAB_ARMOR = "tog_tab_armor";
    private const string TAB_RELIC = "tog_tab_relic";

    // 已装备武器区域（用于 Find 兜底）
    private const string EQUIP_WEAPON_AREA = "EquipedWeapon";

    #endregion

    #region 序列化字段

    [Header("物品列表")]
    [SerializeField] private RectTransform _content;
    [SerializeField] private RectTransform _contentCurrency;
    [SerializeField] private GameObject _slotPrefab;

    [Header("已装备武器")]
    [SerializeField] private RectTransform _equipedWeaponContainer;
    [SerializeField] private GameObject _equipSlotPrefab;

    #endregion

    #region 字段

    private ItemType _currentTab = ItemType.Weapon;
    private readonly List<ItemSlotUI> _activeSlots = new();
    private readonly List<ItemSlotUI> _activeEquipSlots = new();
    private readonly List<ItemSlotUI> _activeCurrencySlots = new();

    #endregion

    #region BasePanel override

    public override void Show()
    {
        base.Show();

        // 订阅背包变化事件
        EventCenter.Instance.Subscribe<InventoryChangeParams>(GameEventID.OnInventoryChanged, OnInventoryChanged);

        // 刷新数据
        RefreshCharacterInfo();
        RefreshEquipedWeapon();
        RefreshCurrencyContent(); // 货币始终刷新
        RefreshItemList();
    }

    public override void Hide()
    {
        // 取消订阅背包变化事件
        EventCenter.Instance.Unsubscribe<InventoryChangeParams>(GameEventID.OnInventoryChanged, OnInventoryChanged);

        // 回收所有 ItemSlot
        ReleaseAllSlots();
        ReleaseAllEquipSlots();
        ReleaseAllCurrencySlots();

        base.Hide();
    }

    #endregion

    #region 初始化

    #endregion

    #region 事件处理

    /// <summary>
    /// 背包内容变化时刷新列表
    /// </summary>
    private void OnInventoryChanged(InventoryChangeParams p)
    {
        if (p.itemType == _currentTab)
        {
            RefreshItemList();
        }
        if (p.itemType == ItemType.Currency)
        {
            RefreshCurrencyContent();
        }
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case BTN_CLOSE:
                GameLifecycleManager.Instance.CloseCurrentPanel();
                break;
        }
    }

    protected override void OnTogValueChanged(string togName, bool value)
    {
        if (!value)
            return;

        // 解析 tab 类型
        ItemType? type = ParseTabName(togName);
        if (type.HasValue)
        {
            SwitchTab(type.Value);
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 解析 Tab 名称为 ItemType
    /// </summary>
    private ItemType? ParseTabName(string name)
    {
        return name switch
        {
            TAB_WEAPON => ItemType.Weapon,
            TAB_AMMO => ItemType.Ammo,
            TAB_POTION => ItemType.Potion,
            TAB_ARMOR => ItemType.Armor,
            TAB_RELIC => ItemType.Relic,
            _ => null
        };
    }

    /// <summary>
    /// 切换当前 Tab
    /// </summary>
    private void SwitchTab(ItemType type)
    {
        _currentTab = type;
        RefreshItemList();
    }

    /// <summary>
    /// 刷新角色信息显示
    /// <para>有活跃 Session：使用运行时数据（SessionManager）</para>
    /// <para>无活跃 Session：使用静态数据（RoleStaticData）</para>
    /// </summary>
    private void RefreshCharacterInfo()
    {
        PlayerRuntimeData playerData;
        RoleStaticData roleData;
        bool hasActiveSession = SessionManager.Instance?.HasActiveSession == true;

        if (hasActiveSession)
        {
            // 有 Session：从 SessionManager 获取运行时数据
            playerData = SessionManager.Instance?.GetPlayerData();
            roleData = playerData != null
                ? GameDataManager.Instance?.GetRoleStaticData(playerData.id)
                : null;
        }
        else
        {
            // 无 Session：使用静态数据（LastSelectedRoleId）
            int roleId = SaveLoadManager.Instance?.LastSelectedRoleId ?? 0;
            roleData = GameDataManager.Instance?.GetRoleStaticData(roleId);
            playerData = roleData != null ? PlayerRuntimeData.CreateBasic(roleData) : null;
        }

        if (playerData == null || roleData == null)
        {
            Debug.LogWarning($"[InventoryPanel] RefreshCharacterInfo failed: playerData={playerData == null}, roleData={roleData == null}");
            return;
        }

        // 角色头像
        var iconImg = GetControl<Image>(IMG_ROLE_ICON);
        if (iconImg != null)
        {
            iconImg.sprite = roleData?.roleIcon;
            iconImg.enabled = iconImg.sprite != null;
        }

        // 角色名
        var nameText = GetControl<TMP_Text>(TXT_ROLE_NAME);
        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(playerData.name) ? roleData?.roleName : playerData.name;

        // 生命
        var healthText = GetControl<TMP_Text>(TXT_HEALTH);
        if (healthText != null)
            healthText.text = $"生命: {playerData.Health:F0}/{playerData.maxHealth:F0}";

        // 攻击
        var atkText = GetControl<TMP_Text>(TXT_ATK);
        if (atkText != null)
            atkText.text = $"攻击: {playerData.atk:F1}";

        // 防御
        var defText = GetControl<TMP_Text>(TXT_DEF);
        if (defText != null)
            defText.text = $"防御: {playerData.def:F1}";

        // 速度
        var speedText = GetControl<TMP_Text>(TXT_SPEED);
        if (speedText != null)
            speedText.text = $"速度: {playerData.moveSpeed:F1}";

        // 冲刺速度
        var dashSpeedText = GetControl<TMP_Text>(TXT_DASH_SPEED);
        if (dashSpeedText != null)
            dashSpeedText.text = $"冲刺速度: {playerData.dashSpeed:F1}";

        // 冲刺持续时间
        var dashDurationText = GetControl<TMP_Text>(TXT_DASH_DURATION);
        if (dashDurationText != null)
            dashDurationText.text = $"冲刺持续: {playerData.dashDuration:F2}s";

        // 冲刺间隔
        var dashGapText = GetControl<TMP_Text>(TXT_DASH_GAP);
        if (dashGapText != null)
            dashGapText.text = $"冲刺间隔: {playerData.dashGap:F2}s";

        // 无敌持续时间
        var invincibleText = GetControl<TMP_Text>(TXT_INVINCIBLE);
        if (invincibleText != null)
            invincibleText.text = $"无敌: {playerData.invincibleDuration:F2}s";

        // 受伤持续时间
        var hurtDurationText = GetControl<TMP_Text>(TXT_HURT_DURATION);
        if (hurtDurationText != null)
            hurtDurationText.text = $"受伤持续: {playerData.hurtDuration:F2}s";
    }

    /// <summary>
    /// 刷新已装备武器显示
    /// <para>Playing 状态：显示 session 中已装备的武器</para>
    /// <para>Lobby 状态：显示角色初始武器</para>
    /// </summary>
    private void RefreshEquipedWeapon()
    {
        bool hasActiveSession = SessionManager.Instance?.HasActiveSession == true;

        int roleId;
        int maxSlots;
        List<int> weaponIds;

        if (hasActiveSession)
        {
            // 有 Session：从 SessionData 获取
            var sessionData = SessionManager.Instance?.CurrentSession;
            var playerData = SessionManager.Instance?.GetPlayerData();
            if (sessionData == null || playerData == null)
                return;

            roleId = playerData.id;
            maxSlots = GameDataManager.Instance?.GetRoleStaticData(roleId)?.maxWeaponSlots ?? 2;
            weaponIds = sessionData.equippedWeaponIds;
        }
        else
        {
            // 无 Session：使用静态数据（初始武器）
            roleId = SaveLoadManager.Instance?.LastSelectedRoleId ?? 0;
            var roleData = GameDataManager.Instance?.GetRoleStaticData(roleId);
            if (roleData == null)
                return;

            maxSlots = roleData.maxWeaponSlots;
            weaponIds = roleData.initialWeaponIds;
        }

        // 获取容器（优先用序列化字段，否则用 Find）
        var container = _equipedWeaponContainer;
        if (container == null)
        {
            var equipWeaponArea = transform.Find(EQUIP_WEAPON_AREA);
            container = equipWeaponArea?.Find("EquipedWeapon_container")?.GetComponent<RectTransform>();
        }

        if (container == null)
            return;

        if (_equipSlotPrefab == null)
        {
            LogError("EquipSlotPrefab is not assigned!");
            return;
        }

        // 回收旧的装备槽
        ReleaseAllEquipSlots();

        // 根据 maxSlots 生成对应数量的装备槽
        for (int i = 0; i < maxSlots; i++)
        {
            var slotObj = PoolManager.Instance.Get(_equipSlotPrefab, Vector3.zero, Quaternion.identity);
            var slot = slotObj.GetComponent<ItemSlotUI>();

            if (slot != null)
            {
                // 如果该槽位有武器，显示武器图标；否则显示默认图标（ItemSlotUI 自带）
                if (weaponIds != null && i < weaponIds.Count && weaponIds[i] > 0)
                {
                    int weaponId = weaponIds[i];
                    slot.Initialize(weaponId, 1, ItemType.Weapon);
                }
                else
                {
                    // 无武器时传 0，显示默认图标
                    slot.Initialize(0, 0, ItemType.Weapon);
                }

                slot.transform.SetParent(container, false);
                _activeEquipSlots.Add(slot);
            }
        }

        Debug.Log($"[InventoryPanel] container pivot:{container.pivot}, anchor:{container.anchorMin}-{container.anchorMax}, sizeDelta:{container.sizeDelta}");
    }

    /// <summary>
    /// 刷新物品列表
    /// <para>有活跃 Session：从 InventoryManager 获取最新数据（SessionData）</para>
    /// <para>无活跃 Session：不显示物品背包</para>
    /// </summary>
    private void RefreshItemList()
    {
        bool hasActiveSession = SessionManager.Instance?.HasActiveSession == true;

        // 无 Session 不显示物品背包
        if (!hasActiveSession)
            return;

        if (_slotPrefab == null)
        {
            LogError("Slot prefab is not set.");
            return;
        }

        // 回收当前所有 Slot
        ReleaseAllSlots();

        // 获取当前 Tab 的物品
        var itemIds = InventoryManager.Instance?.GetInventory(_currentTab);
        if (itemIds == null || itemIds.Count == 0)
            return;

        if (_content == null)
            return;

        // 按 ID 分组统计堆叠
        var grouped = itemIds.GroupBy(id => id).ToList();

        // 生成 ItemSlot（根据 maxStack 决定是否分格显示）
        foreach (var group in grouped)
        {
            int itemId = group.Key;
            int totalCount = group.Count();
            var config = GameDataManager.Instance?.GetItemConfig(itemId);
            int maxStack = config?.MaxStack ?? 1;

            // 根据 maxStack 分配格子
            int remaining = totalCount;
            while (remaining > 0)
            {
                int stackCount = Mathf.Min(remaining, maxStack);
                remaining -= stackCount;

                var slotObj = PoolManager.Instance.Get(_slotPrefab, Vector3.zero, Quaternion.identity);
                var slot = slotObj.GetComponent<ItemSlotUI>();

                if (slot != null)
                {
                    slot.Initialize(itemId, stackCount, _currentTab);
                    slot.transform.SetParent(_content, false);
                    _activeSlots.Add(slot);
                }
            }
        }

        // 手动计算 Content 高度（适配 GridLayoutGroup 的 CellSize 和 Spacing）
        UpdateContentHeightForGrid(_content, _activeSlots.Count);
    }

    /// <summary>
    /// 刷新货币页签内容
    /// <para>Playing 状态：显示背包中的货币物品</para>
    /// </summary>
    private void RefreshCurrencyContent()
    {
        if (_contentCurrency == null)
        {
            LogError("_contentCurrency is null!");
            return;
        }

        if (_slotPrefab == null)
        {
            LogError("Slot prefab is not set.");
            return;
        }

        // 回收旧的货币 Slot
        ReleaseAllCurrencySlots();

        // 获取货币物品列表
        var currencyIds = InventoryManager.Instance?.GetInventory(ItemType.Currency);
        if (currencyIds == null || currencyIds.Count == 0)
            return;

        // 按 ID 分组统计堆叠
        var grouped = currencyIds.GroupBy(id => id).ToList();

        // 生成货币 ItemSlot（根据 maxStack 决定是否分格显示）
        foreach (var group in grouped)
        {
            int itemId = group.Key;
            int totalCount = group.Count();
            var config = GameDataManager.Instance?.GetItemConfig(itemId);
            int maxStack = config?.MaxStack ?? 1;

            // 根据 maxStack 分配格子
            int remaining = totalCount;
            while (remaining > 0)
            {
                int stackCount = Mathf.Min(remaining, maxStack);
                remaining -= stackCount;

                var slotObj = PoolManager.Instance.Get(_slotPrefab, Vector3.zero, Quaternion.identity);
                var slot = slotObj.GetComponent<ItemSlotUI>();

                if (slot != null)
                {
                    slot.Initialize(itemId, stackCount, ItemType.Currency);
                    slot.transform.SetParent(_contentCurrency, false);
                    _activeCurrencySlots.Add(slot);
                }
            }
        }

        // 更新 Content 高度
        UpdateContentHeightForGrid(_contentCurrency, _activeCurrencySlots.Count);
    }

    /// <summary>
    /// 根据 GridLayoutGroup 设置和 ItemSlot 数量更新 Content 高度
    /// </summary>
    /// <param name="content">Content 的 RectTransform</param>
    /// <param name="itemCount">Item 数量</param>
    private void UpdateContentHeightForGrid(RectTransform content, int itemCount)
    {
        if (content == null || itemCount == 0)
            return;

        var gridLayout = content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (gridLayout == null)
            return;

        // 获取 GridLayoutGroup 的设置
        Vector2 cellSize = gridLayout.cellSize;
        Vector2 spacing = gridLayout.spacing;
        float paddingTop = gridLayout.padding.top;
        float paddingBottom = gridLayout.padding.bottom;

        // 获取 Content 的宽度
        float contentWidth = content.rect.width;

        // 计算列数（每行能放几个）
        float availableWidth = contentWidth - gridLayout.padding.left - gridLayout.padding.right;
        int columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing.x) / (cellSize.x + spacing.x)));

        // 计算行数
        int rows = Mathf.CeilToInt((float)itemCount / columns);

        // 计算总高度
        float totalHeight = paddingTop + paddingBottom + rows * cellSize.y + (rows - 1) * spacing.y;

        // 更新 Content 高度
        var sizeDelta = content.sizeDelta;
        sizeDelta.y = totalHeight;
        content.sizeDelta = sizeDelta;
    }

    /// <summary>
    /// 回收所有活跃的 ItemSlot
    /// </summary>
    private void ReleaseAllSlots()
    {
        foreach (var slot in _activeSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                PoolManager.Instance.Release(slot.gameObject);
            }
        }
        _activeSlots.Clear();
    }

    /// <summary>
    /// 回收所有已装备武器槽
    /// </summary>
    private void ReleaseAllEquipSlots()
    {
        foreach (var slot in _activeEquipSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                PoolManager.Instance.Release(slot.gameObject);
            }
        }
        _activeEquipSlots.Clear();
    }

    /// <summary>
    /// 回收所有货币槽
    /// </summary>
    private void ReleaseAllCurrencySlots()
    {
        foreach (var slot in _activeCurrencySlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                PoolManager.Instance.Release(slot.gameObject);
            }
        }
        _activeCurrencySlots.Clear();
    }

    #endregion

    #region 日志

    private void Log(string msg) => Debug.Log($"[InventoryPanel] {msg}");
    private void LogError(string msg) => Debug.LogError($"[InventoryPanel] {msg}");

    #endregion
}
