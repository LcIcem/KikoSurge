using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    [SerializeField] private GameObject _emptySlotPrefab;
    [SerializeField] private GameObject _placeholderPrefab;

    [Header("已装备武器")]
    [SerializeField] private RectTransform _equipedWeaponContainer;
    [SerializeField] private GameObject _equipSlotPrefab;

    [Header("垃圾桶")]
    [SerializeField] private RectTransform _trashArea;
    [SerializeField] private ItemSlotUI _trashSlot;

    [Header("ScrollView")]
    [SerializeField] private UnityEngine.UI.ScrollRect _scrollRectItem;
    [SerializeField] private UnityEngine.UI.ScrollRect _scrollRectCurrency;

    #endregion

    #region 字段

    private ItemType _currentTab = ItemType.Weapon;
    private readonly List<ItemSlotUI> _activeSlots = new();
    private readonly List<ItemSlotUI> _activeEquipSlots = new();
    private readonly List<ItemSlotUI> _activeCurrencySlots = new();

    // 拿起状态
    private ItemSlotUI _pickedUpSlot;
    private ItemSlotUI _pickedUpPlaceholder;
    private int _pickedUpSourceIndex;
    private RectTransform _pickedUpParent;
    private ItemType _pickedUpItemType;

    // 垃圾桶
    private ItemSlotData _pendingDeleteItem;

    #endregion

    #region BasePanel override

    public override void Show()
    {
        base.Show();

        EventCenter.Instance.Subscribe<InventoryChangeParams>(GameEventID.OnInventoryChanged, OnInventoryChanged);

        RefreshCharacterInfo();
        RefreshEquipedWeapon();
        RefreshCurrencyContent();
        RefreshItemList();
        UpdateTrashSlotDisplay();
    }

    public override void Hide()
    {
        EventCenter.Instance.Unsubscribe<InventoryChangeParams>(GameEventID.OnInventoryChanged, OnInventoryChanged);

        ReleaseAllSlots();
        ReleaseAllEquipSlots();
        ReleaseAllCurrencySlots();
        CancelPickup();

        base.Hide();
    }

    #endregion

    #region 点击交互

    private void Update()
    {
        // 让拿起的物品跟随鼠标
        if (_pickedUpSlot != null)
        {
            _pickedUpSlot.RectTransform.position = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
    }

    /// <summary>
    /// 物品格子点击事件
    /// </summary>
    private void OnSlotClicked(ItemSlotUI slot)
    {
        if (slot == null)
            return;

        // 如果有物品被拿起
        if (_pickedUpSlot != null)
        {
            PlaceSlot(slot);
            return;
        }

        // 没有物品被拿起时，只能拿起非空的物品格子
        if (slot.IsPlaceholder || slot.IsEmpty)
            return;

        PickupSlot(slot);
    }

    /// <summary>
    /// 拿起格子中的物品
    /// </summary>
    private void PickupSlot(ItemSlotUI slot)
    {
        _pickedUpSourceIndex = slot.CurrentIndex;
        _pickedUpParent = slot.transform.parent as RectTransform;
        _pickedUpItemType = slot.ItemType;

        // 禁用 LayoutGroup，防止移除时重新排列
        var layoutGroup = _pickedUpParent?.GetComponent<UnityEngine.UI.LayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        // 记录原始 worldPosition
        Vector3 originalWorldPos = slot.RectTransform.position;

        // 将格子移到 Canvas 顶层
        slot.transform.SetParent(transform.root, false);

        // 设置高亮和禁用 raycast
        _pickedUpSlot = slot;
        _pickedUpSlot.SetHighlight(true);
        var canvasGroup = _pickedUpSlot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = _pickedUpSlot.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        // 在原位置创建占位符
        CreatePlaceholder(originalWorldPos);
    }

    /// <summary>
    /// 在目标格子放置物品
    /// </summary>
    private void PlaceSlot(ItemSlotUI targetSlot)
    {
        Debug.Log($"[PlaceSlot] targetSlot={targetSlot?.name}, IsPlaceholder={targetSlot?.IsPlaceholder}, CurrentIndex={targetSlot?.CurrentIndex}");

        if (_pickedUpSlot == null)
        {
            Debug.Log("[PlaceSlot] FAIL: _pickedUpSlot is null");
            return;
        }

        // 点击的是 placeholder（源位置），取消拿起
        if (targetSlot != null && targetSlot.IsPlaceholder && targetSlot.CurrentIndex == _pickedUpSourceIndex)
        {
            Debug.Log("[PlaceSlot] CancelPickup: clicked placeholder at source index");
            CancelPickup();
            return;
        }

        // 不能放在 placeholder 上（其他位置的 placeholder）
        if (targetSlot == null || targetSlot.IsPlaceholder)
        {
            Debug.Log("[PlaceSlot] CancelPickup: target is null or placeholder");
            CancelPickup();
            return;
        }

        // 执行交换/移动（TrySwapOrMove 会检查类型兼容性）
        bool success = TrySwapOrMove(targetSlot);
        Debug.Log($"[PlaceSlot] TrySwapOrMove returned: {success}");

        if (success)
        {
            // 刷新 UI
            RefreshAllViews();
            // 清理拿起状态
            ClearPickup();
        }
        else
        {
            // 无效的移动（如跨类型放置），取消拿起回到原位
            Debug.Log("[PlaceSlot] CancelPickup: TrySwapOrMove failed");
            CancelPickup();
        }
    }

    /// <summary>
    /// 尝试交换或移动物品
    /// </summary>
    private bool TrySwapOrMove(ItemSlotUI targetSlot)
    {
        int sourceIndex = _pickedUpSourceIndex;
        int targetIndex = targetSlot.CurrentIndex;
        ItemType sourceType = _pickedUpItemType;
        ItemType targetType = targetSlot.ItemType;

        bool sourceIsEquip = _pickedUpParent == _equipedWeaponContainer;
        bool targetIsEquip = targetSlot.transform.parent as RectTransform == _equipedWeaponContainer;

        Debug.Log($"[TrySwapOrMove] sourceIsEquip={sourceIsEquip}, targetIsEquip={targetIsEquip}, sourceType={sourceType}, targetType={targetType}, sourceIndex={sourceIndex}, targetIndex={targetIndex}");

        // 装备区 < -> 装备区：交换
        if (sourceIsEquip && targetIsEquip)
        {
            Debug.Log("[TrySwapOrMove] Case: 装备区 < -> 装备区 交换");
            InventoryManager.Instance?.SwapEquippedSlots(sourceIndex, targetIndex);
            return true;
        }

        // 背包 -> 装备区（仅武器）
        if (!sourceIsEquip && targetIsEquip && sourceType == ItemType.Weapon)
        {
            Debug.Log("[TrySwapOrMove] Case: 背包 -> 装备区");
            InventoryManager.Instance?.EquipFromInventory(sourceIndex, targetIndex);
            return true;
        }

        // 装备区 -> 背包（仅武器区域）
        if (sourceIsEquip && !targetIsEquip && targetType == ItemType.Weapon)
        {
            Debug.Log("[TrySwapOrMove] Case: 装备区 -> 背包");
            InventoryManager.Instance?.UnequipWeapon(sourceIndex, targetIndex);
            return true;
        }

        // 背包内移动：交换数据（必须是同性之间）
        if (sourceType == targetType)
        {
            Debug.Log("[TrySwapOrMove] Case: 背包内移动");
            InventoryManager.Instance?.MoveSlot(sourceType, sourceIndex, targetIndex);
            return true;
        }

        Debug.Log("[TrySwapOrMove] Case: 无匹配，返回 false");
        return false;
    }

    /// <summary>
    /// 创建占位符
    /// </summary>
    private void CreatePlaceholder(Vector3 worldPos)
    {
        if (_pickedUpPlaceholder != null)
        {
            PoolManager.Instance.Release(_pickedUpPlaceholder.gameObject);
        }

        if (_placeholderPrefab == null)
        {
            LogError($"_placeholderPrefab is null! Please assign in Inspector.");
            return;
        }

        var obj = PoolManager.Instance.Get(_placeholderPrefab, Vector3.zero, Quaternion.identity);
        _pickedUpPlaceholder = obj.GetComponent<ItemSlotUI>();

        if (_pickedUpPlaceholder != null)
        {
            _pickedUpPlaceholder.SetAsPlaceholder(true);
            _pickedUpPlaceholder.transform.SetParent(_pickedUpParent, false);
            _pickedUpPlaceholder.RectTransform.position = worldPos;
            _pickedUpPlaceholder.CurrentIndex = _pickedUpSourceIndex;
            _pickedUpPlaceholder.OnSlotClicked += OnSlotClicked;
        }
    }

    /// <summary>
    /// 取消拿起状态
    /// </summary>
    private void CancelPickup()
    {
        if (_pickedUpSlot != null)
        {
            _pickedUpSlot.transform.SetParent(_pickedUpParent, false);
            _pickedUpSlot.transform.SetSiblingIndex(_pickedUpSourceIndex);
            _pickedUpSlot.SetHighlight(false);

            var canvasGroup = _pickedUpSlot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;
        }

        if (_pickedUpPlaceholder != null)
        {
            _pickedUpPlaceholder.OnSlotClicked -= OnSlotClicked;
            PoolManager.Instance.Release(_pickedUpPlaceholder.gameObject);
            _pickedUpPlaceholder = null;
        }

        // 重新启用 LayoutGroup
        var layoutGroup = _pickedUpParent?.GetComponent<UnityEngine.UI.LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_pickedUpParent);
        }

        _pickedUpSlot = null;
        _pickedUpParent = null;
    }

    /// <summary>
    /// 清理拿起状态
    /// </summary>
    private void ClearPickup()
    {
        if (_pickedUpPlaceholder != null)
        {
            _pickedUpPlaceholder.OnSlotClicked -= OnSlotClicked;
            PoolManager.Instance.Release(_pickedUpPlaceholder.gameObject);
            _pickedUpPlaceholder = null;
        }

        if (_pickedUpSlot != null)
        {
            var canvasGroup = _pickedUpSlot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            // 释放到对象池（因为这个 slot 已经被 RefreshAllViews 释放了原始列表中的引用）
            PoolManager.Instance.Release(_pickedUpSlot.gameObject);
        }

        // 重新启用 LayoutGroup 并强制重新计算
        var layoutGroup = _pickedUpParent?.GetComponent<UnityEngine.UI.LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_pickedUpParent);
        }

        _pickedUpSlot = null;
        _pickedUpParent = null;
    }

    /// <summary>
    /// 刷新所有视图
    /// </summary>
    private void RefreshAllViews()
    {
        RefreshItemList();
        // 没有 session 时不刷新装备区（因为数据源是空的，会覆盖 UI 层面的交换）
        if (SessionManager.Instance?.HasActiveSession == true)
        {
            RefreshEquipedWeapon();
        }
        RefreshCurrencyContent();
    }

    #endregion

    #region 事件处理

    private void OnInventoryChanged(InventoryChangeParams p)
    {
        RefreshAllViews();
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

        ItemType? type = ParseTabName(togName);
        if (type.HasValue)
        {
            _currentTab = type.Value;
            RefreshItemList();
        }
    }

    #endregion

    #region 私有方法

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

    private void RefreshCharacterInfo()
    {
        PlayerRuntimeData playerData;
        RoleStaticData roleData;
        bool hasActiveSession = SessionManager.Instance?.HasActiveSession == true;

        if (hasActiveSession)
        {
            playerData = SessionManager.Instance?.GetPlayerData();
            roleData = playerData != null ? GameDataManager.Instance?.GetRoleStaticData(playerData.id) : null;
        }
        else
        {
            int roleId = SaveLoadManager.Instance?.LastSelectedRoleId ?? 0;
            roleData = GameDataManager.Instance?.GetRoleStaticData(roleId);
            playerData = roleData != null ? PlayerRuntimeData.CreateBasic(roleData) : null;
        }

        if (playerData == null || roleData == null)
            return;

        var iconImg = GetControl<Image>(IMG_ROLE_ICON);
        if (iconImg != null)
        {
            iconImg.sprite = roleData.roleIcon;
            iconImg.enabled = iconImg.sprite != null;
        }

        var nameText = GetControl<TMP_Text>(TXT_ROLE_NAME);
        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(playerData.name) ? roleData.roleName : playerData.name;

        SetText(TXT_HEALTH, $"生命: {playerData.Health:F0}/{playerData.maxHealth:F0}");
        SetText(TXT_ATK, $"攻击: {playerData.atk:F1}");
        SetText(TXT_DEF, $"防御: {playerData.def:F1}");
        SetText(TXT_SPEED, $"速度: {playerData.moveSpeed:F1}");
        SetText(TXT_DASH_SPEED, $"冲刺速度: {playerData.dashSpeed:F1}");
        SetText(TXT_DASH_DURATION, $"冲刺持续: {playerData.dashDuration:F2}s");
        SetText(TXT_DASH_GAP, $"冲刺间隔: {playerData.dashGap:F2}s");
        SetText(TXT_INVINCIBLE, $"无敌: {playerData.invincibleDuration:F2}s");
        SetText(TXT_HURT_DURATION, $"受伤持续: {playerData.hurtDuration:F2}s");
    }

    private void SetText(string controlName, string text)
    {
        var txt = GetControl<TMP_Text>(controlName);
        if (txt != null)
            txt.text = text;
    }

    private void RefreshEquipedWeapon()
    {
        bool hasActiveSession = SessionManager.Instance?.HasActiveSession == true;

        int roleId;
        int maxSlots;
        List<int> weaponIds;

        if (hasActiveSession)
        {
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
            roleId = SaveLoadManager.Instance?.LastSelectedRoleId ?? 0;
            var roleData = GameDataManager.Instance?.GetRoleStaticData(roleId);
            if (roleData == null)
                return;

            maxSlots = roleData.maxWeaponSlots;
            weaponIds = roleData.initialWeaponIds;
        }

        var container = _equipedWeaponContainer;
        if (container == null)
        {
            var equipWeaponArea = transform.Find(EQUIP_WEAPON_AREA);
            container = equipWeaponArea?.Find("EquipedWeapon_container")?.GetComponent<RectTransform>();
        }

        if (container == null)
        {
            LogError($"Equipped weapon container is null!");
            return;
        }
        if (_equipSlotPrefab == null)
        {
            LogError($"_equipSlotPrefab is null! Please assign in Inspector.");
            return;
        }

        ReleaseAllEquipSlots();

        // 禁用 LayoutGroup，防止添加时自动排列
        var layoutGroup = container.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        bool layoutGroupWasEnabled = layoutGroup != null && layoutGroup.enabled;
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        for (int i = 0; i < maxSlots; i++)
        {
            bool hasWeapon = weaponIds != null && i < weaponIds.Count && weaponIds[i] > 0;
            GameObject prefab = hasWeapon ? _equipSlotPrefab : (_emptySlotPrefab ?? _equipSlotPrefab);
            var slotObj = PoolManager.Instance.Get(prefab, Vector3.zero, Quaternion.identity);
            var slot = slotObj.GetComponent<ItemSlotUI>();

            if (slot != null)
            {
                if (hasWeapon)
                    slot.Initialize(weaponIds[i], 1, ItemType.Weapon, i);
                else
                    slot.Initialize(0, 0, ItemType.Weapon, i);

                slot.transform.SetParent(container, false);
                slot.transform.SetSiblingIndex(i);
                slot.OnSlotClicked += OnSlotClicked;
                _activeEquipSlots.Add(slot);
            }
        }

        // 恢复 LayoutGroup 并强制重新计算
        if (layoutGroup != null)
        {
            layoutGroup.enabled = layoutGroupWasEnabled;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }
    }

    private void RefreshItemList()
    {
        if (!SessionManager.Instance?.HasActiveSession == true)
            return;

        if (_slotPrefab == null)
        {
            LogError($"_slotPrefab is null! Please assign in Inspector.");
            return;
        }

        ReleaseAllSlots();

        var itemIds = InventoryManager.Instance?.GetInventory(_currentTab) ?? new List<ItemSlotData>();

        if (itemIds.Count == 0)
        {
            for (int i = 0; i < 20; i++)
                itemIds.Add(new ItemSlotData());
        }

        if (_content == null)
            return;

        // 禁用 LayoutGroup，防止添加时自动排列
        var layoutGroup = _content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        bool layoutGroupWasEnabled = layoutGroup != null && layoutGroup.enabled;
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        int slotIndex = 0;
        foreach (var slotData in itemIds)
        {
            int itemId = slotData.itemId;
            int quantity = slotData.quantity;
            bool isEmpty = itemId == 0 || quantity <= 0;

            int maxStack = 1;
            if (!isEmpty)
            {
                var config = GameDataManager.Instance?.GetItemConfig(itemId);
                maxStack = config?.MaxStack ?? 1;
            }

            int remaining = quantity;
            if (isEmpty)
            {
                remaining = 1;
                maxStack = 1;
            }

            while (remaining > 0)
            {
                int stackCount = Mathf.Min(remaining, maxStack);
                remaining -= stackCount;

                GameObject prefab = isEmpty ? (_emptySlotPrefab ?? _slotPrefab) : _slotPrefab;
                var slotObj = PoolManager.Instance.Get(prefab, Vector3.zero, Quaternion.identity);
                var slot = slotObj.GetComponent<ItemSlotUI>();

                if (slot != null)
                {
                    slot.Initialize(itemId, isEmpty ? 0 : stackCount, _currentTab, slotIndex);
                    slot.transform.SetParent(_content, false);
                    slot.transform.SetSiblingIndex(slotIndex);
                    slot.OnSlotClicked += OnSlotClicked;
                    _activeSlots.Add(slot);
                    slotIndex++;
                }
            }
        }

        // 恢复 LayoutGroup 并强制重新计算
        if (layoutGroup != null)
        {
            layoutGroup.enabled = layoutGroupWasEnabled;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        UpdateContentHeightForGrid(_content, _activeSlots.Count);
    }

    private void RefreshCurrencyContent()
    {
        if (_contentCurrency == null)
        {
            LogError($"_contentCurrency is null! Please assign in Inspector.");
            return;
        }
        if (_slotPrefab == null)
        {
            LogError($"_slotPrefab is null! Please assign in Inspector.");
            return;
        }

        ReleaseAllCurrencySlots();

        var currencyIds = InventoryManager.Instance?.GetInventory(ItemType.Currency) ?? new List<ItemSlotData>();

        // 确保 ScrollRect 的 content 引用正确
        if (_scrollRectCurrency != null && _scrollRectCurrency.content != _contentCurrency)
        {
            _scrollRectCurrency.content = _contentCurrency;
        }

        // 禁用 ContentSizeFitter，防止干扰手动高度设置
        var contentSizeFitter = _contentCurrency.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        bool fitterWasEnabled = contentSizeFitter != null && contentSizeFitter.enabled;
        if (contentSizeFitter != null)
            contentSizeFitter.enabled = false;

        // 禁用 LayoutGroup，防止添加时自动排列
        var layoutGroup = _contentCurrency.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        bool layoutGroupWasEnabled = layoutGroup != null && layoutGroup.enabled;
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        // 先添加所有有物品的格子
        int currencySlotIndex = 0;
        foreach (var slotData in currencyIds)
        {
            int itemId = slotData.itemId;
            int quantity = slotData.quantity;
            bool isEmpty = itemId == 0 || quantity <= 0;
            if (isEmpty)
                continue;

            int maxStack = GameDataManager.Instance?.GetItemConfig(itemId)?.MaxStack ?? 1;
            int remaining = quantity;

            while (remaining > 0)
            {
                int stackCount = Mathf.Min(remaining, maxStack);
                remaining -= stackCount;

                var slotObj = PoolManager.Instance.Get(_slotPrefab, Vector3.zero, Quaternion.identity);
                var slot = slotObj.GetComponent<ItemSlotUI>();

                if (slot != null)
                {
                    slot.Initialize(itemId, stackCount, ItemType.Currency, currencySlotIndex);
                    slot.transform.SetParent(_contentCurrency, false);
                    slot.transform.SetSiblingIndex(currencySlotIndex);
                    slot.OnSlotClicked += OnSlotClicked;
                    _activeCurrencySlots.Add(slot);
                    currencySlotIndex++;
                }
            }
        }

        // 基础空格子数量（用于填充和扩容）
        int baseEmptyCount = 5;
        int totalSlots = currencySlotIndex + baseEmptyCount;

        // 如果已有物品，添加额外空格子用于扩容（基于实际物品类型数量）
        if (currencySlotIndex > 0)
        {
            // 每个物品类型最多显示 N 个空格子用于扩容
            int extraEmptyPerItem = 3;
            totalSlots = Mathf.Max(totalSlots, currencySlotIndex * extraEmptyPerItem + baseEmptyCount);
        }

        for (int i = currencySlotIndex; i < totalSlots; i++)
        {
            GameObject prefab = _emptySlotPrefab ?? _slotPrefab;
            var slotObj = PoolManager.Instance.Get(prefab, Vector3.zero, Quaternion.identity);
            var slot = slotObj.GetComponent<ItemSlotUI>();

            if (slot != null)
            {
                slot.Initialize(0, 0, ItemType.Currency, i);
                slot.transform.SetParent(_contentCurrency, false);
                slot.transform.SetSiblingIndex(i);
                slot.OnSlotClicked += OnSlotClicked;
                _activeCurrencySlots.Add(slot);
            }
        }

        // 恢复 LayoutGroup 并强制重新计算
        if (layoutGroup != null)
        {
            layoutGroup.enabled = layoutGroupWasEnabled;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_contentCurrency);
        }

        UpdateContentHeightForGrid(_contentCurrency, _activeCurrencySlots.Count);

        // 恢复 ContentSizeFitter
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = fitterWasEnabled;
        }
    }

    private void UpdateContentHeightForGrid(RectTransform content, int itemCount)
    {
        if (content == null)
            return;

        var gridLayout = content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (gridLayout == null)
            return;

        Vector2 cellSize = gridLayout.cellSize;
        Vector2 spacing = gridLayout.spacing;
        float paddingTop = gridLayout.padding.top;
        float paddingBottom = gridLayout.padding.bottom;

        // 尝试获取 content 的宽度
        float contentWidth = content.rect.width;
        if (contentWidth <= 0)
        {
            // 尝试通过 LayoutElement 或父对象计算
            var parent = content.parent as RectTransform;
            if (parent != null)
            {
                contentWidth = parent.rect.width - gridLayout.padding.left - gridLayout.padding.right;
            }
        }
        else
        {
            contentWidth -= gridLayout.padding.left + gridLayout.padding.right;
        }

        float availableWidth = contentWidth;
        int columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing.x) / (cellSize.x + spacing.x)));

        int rows = Mathf.CeilToInt((float)itemCount / columns);
        if (rows < 1) rows = 1;

        float totalHeight = paddingTop + paddingBottom + rows * cellSize.y + (rows - 1) * spacing.y;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
    }

    private void ReleaseAllSlots()
    {
        foreach (var slot in _activeSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                slot.OnSlotClicked -= OnSlotClicked;
                PoolManager.Instance.Release(slot.gameObject);
            }
        }
        _activeSlots.Clear();
    }

    private void ReleaseAllEquipSlots()
    {
        foreach (var slot in _activeEquipSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                slot.OnSlotClicked -= OnSlotClicked;
                PoolManager.Instance.Release(slot.gameObject);
            }
        }
        _activeEquipSlots.Clear();
    }

    private void ReleaseAllCurrencySlots()
    {
        foreach (var slot in _activeCurrencySlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                slot.OnSlotClicked -= OnSlotClicked;
                PoolManager.Instance.Release(slot.gameObject);
            }
        }
        _activeCurrencySlots.Clear();
    }

    private void UpdateTrashSlotDisplay()
    {
        if (_trashSlot == null)
            return;

        _trashSlot.gameObject.SetActive(true);

        if (_pendingDeleteItem != null && !_pendingDeleteItem.IsEmpty)
            _trashSlot.Initialize(_pendingDeleteItem.itemId, _pendingDeleteItem.quantity, ItemType.Weapon);
        else
            _trashSlot.Initialize(0, 0, ItemType.Weapon);
    }

    #endregion

    #region 日志

    private void Log(string msg) => Debug.Log($"[InventoryPanel] {msg}");
    private void LogError(string msg) => Debug.LogError($"[InventoryPanel] {msg}");

    #endregion
}
