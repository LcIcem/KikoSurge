using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
    private const string TAB_CURRENCY = "tog_tab_currency";

    #endregion

    #region 序列化字段

    [Header("物品列表")]
    [SerializeField] private RectTransform _content;
    [SerializeField] private RectTransform _contentCurrency;
    [SerializeField] private ItemSlotUI _slotPrefab;

    #endregion

    #region 字段

    private ItemType _currentTab = ItemType.Weapon;
    private bool _isCurrencyTab;
    private readonly List<ItemSlotUI> _activeSlots = new();

    #endregion

    #region BasePanel override

    public override void Show()
    {
        base.Show();

        // 订阅背包变化事件
        EventCenter.Instance.Subscribe<InventoryChangeParams>(GameEventID.OnInventoryChanged, OnInventoryChanged);

        // 刷新数据
        RefreshCharacterInfo();
        RefreshItemList();
    }

    public override void Hide()
    {
        // 取消订阅背包变化事件
        EventCenter.Instance.Unsubscribe<InventoryChangeParams>(GameEventID.OnInventoryChanged, OnInventoryChanged);

        // 回收所有 ItemSlot
        ReleaseAllSlots();

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
            TAB_CURRENCY => ItemType.Currency,
            _ => null
        };
    }

    /// <summary>
    /// 切换当前 Tab
    /// </summary>
    private void SwitchTab(ItemType type)
    {
        _currentTab = type;
        _isCurrencyTab = (type == ItemType.Currency);
        RefreshContentVisibility();
        RefreshItemList();
    }

    /// <summary>
    /// 刷新 Content 区域显示状态
    /// </summary>
    private void RefreshContentVisibility()
    {
        if (_content != null)
            _content.gameObject.SetActive(!_isCurrencyTab);
        if (_contentCurrency != null)
            _contentCurrency.gameObject.SetActive(_isCurrencyTab);
    }

    /// <summary>
    /// 刷新角色信息显示
    /// </summary>
    private void RefreshCharacterInfo()
    {
        var playerData = SessionManager.Instance?.GetPlayerData();
        if (playerData == null)
            return;

        // 角色头像
        var iconImg = GetControl<Image>(IMG_ROLE_ICON);
        if (iconImg != null)
        {
            var roleData = GameDataManager.Instance?.GetRoleStaticData(playerData.id);
            iconImg.sprite = roleData?.roleIcon;
            iconImg.enabled = iconImg.sprite != null;
        }

        // 角色名
        var nameText = GetControl<Text>(TXT_ROLE_NAME);
        if (nameText != null)
            nameText.text = playerData.name;

        // 生命
        var healthText = GetControl<Text>(TXT_HEALTH);
        if (healthText != null)
            healthText.text = $"生命: {playerData.Health:F0}/{playerData.maxHealth:F0}";

        // 攻击
        var atkText = GetControl<Text>(TXT_ATK);
        if (atkText != null)
            atkText.text = $"攻击: {playerData.atk:F1}";

        // 防御
        var defText = GetControl<Text>(TXT_DEF);
        if (defText != null)
            defText.text = $"防御: {playerData.def:F1}";

        // 速度
        var speedText = GetControl<Text>(TXT_SPEED);
        if (speedText != null)
            speedText.text = $"速度: {playerData.moveSpeed:F1}";

        // 冲刺速度
        var dashSpeedText = GetControl<Text>(TXT_DASH_SPEED);
        if (dashSpeedText != null)
            dashSpeedText.text = $"冲刺速度: {playerData.dashSpeed:F1}";

        // 冲刺持续时间
        var dashDurationText = GetControl<Text>(TXT_DASH_DURATION);
        if (dashDurationText != null)
            dashDurationText.text = $"冲刺持续: {playerData.dashDuration:F2}s";

        // 冲刺间隔
        var dashGapText = GetControl<Text>(TXT_DASH_GAP);
        if (dashGapText != null)
            dashGapText.text = $"冲刺间隔: {playerData.dashGap:F2}s";

        // 无敌持续时间
        var invincibleText = GetControl<Text>(TXT_INVINCIBLE);
        if (invincibleText != null)
            invincibleText.text = $"无敌: {playerData.invincibleDuration:F2}s";

        // 受伤持续时间
        var hurtDurationText = GetControl<Text>(TXT_HURT_DURATION);
        if (hurtDurationText != null)
            hurtDurationText.text = $"受伤持续: {playerData.hurtDuration:F2}s";
    }

    /// <summary>
    /// 刷新物品列表
    /// </summary>
    private void RefreshItemList()
    {
        if (_slotPrefab == null)
        {
            LogError("Slot prefab is not set.");
            return;
        }

        // 回收当前所有 Slot
        ReleaseAllSlots();

        // 货币页签不需要生成 ItemSlot
        if (_isCurrencyTab)
            return;

        // 获取当前 Tab 的物品
        var itemIds = InventoryManager.Instance?.GetInventory(_currentTab);
        if (itemIds == null || itemIds.Count == 0)
            return;

        if (_content == null)
            return;

        // 按 ID 分组统计堆叠
        var grouped = itemIds.GroupBy(id => id);

        // 生成 ItemSlot
        foreach (var group in grouped)
        {
            var slotObj = PoolManager.Instance.Get(_slotPrefab.gameObject, Vector3.zero, Quaternion.identity);
            var slot = slotObj.GetComponent<ItemSlotUI>();

            if (slot != null)
            {
                slot.Initialize(group.Key, group.Count(), _currentTab);
                slot.transform.SetParent(_content, false);
                _activeSlots.Add(slot);
            }
        }
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

    #endregion

    #region 日志

    private void Log(string msg) => Debug.Log($"[InventoryPanel] {msg}");
    private void LogError(string msg) => Debug.LogError($"[InventoryPanel] {msg}");

    #endregion
}
