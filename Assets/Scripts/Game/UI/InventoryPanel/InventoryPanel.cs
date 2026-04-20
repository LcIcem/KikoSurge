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
    [SerializeField] private ScrollRect _scrollRectItem;
    [SerializeField] private ScrollRect _scrollRectCurrency;

    [Header("上下文菜单")]
    [SerializeField] private GameObject _contextMenuRoot;
    [SerializeField] private Button _btnUse;
    [SerializeField] private Button _btnSplit;

    [Header("拆分窗口")]
    [SerializeField] private GameObject _splitDialogRoot;
    [SerializeField] private Slider _sliderSplitQuantity;
    [SerializeField] private TMP_Text _txtSplitQuantity;
    [SerializeField] private Button _btnSplitConfirm;
    [SerializeField] private Button _btnSplitCancel;

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

    // 上下文菜单
    private ItemSlotUI _contextMenuSlot;
    private ItemSlotUI _splitContextSlot; // 拆分对话框专用 slot 引用
    private int _splitQuantity;

    #endregion

    #region BasePanel override

    public override void Show()
    {
        base.Show();

        Debug.Log($"[InventoryPanel.Show] _trashSlot={_trashSlot?.name}({_trashSlot?.GetInstanceID()}), _trashArea={_trashArea?.name}({_trashArea?.GetInstanceID()})");

        // 从 SessionData 恢复 pending 物品
        var session = SessionManager.Instance?.CurrentSession;
        if (session?.trashPendingItem != null && !session.trashPendingItem.IsEmpty)
        {
            _pendingDeleteItem = session.trashPendingItem;
            Debug.Log($"[InventoryPanel.Show] Restored trash pending item: {_pendingDeleteItem.itemId}, qty={_pendingDeleteItem.quantity}");
        }
        else
        {
            _pendingDeleteItem = null;
        }

        EventCenter.Instance.Subscribe<InventoryChangeParams>(GameEventID.OnInventoryChanged, OnInventoryChanged);

        // 初始化上下文菜单
        InitContextMenu();

        // 初始化拆分窗口
        InitSplitDialog();

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

        // 隐藏上下文菜单
        HideContextMenu();
        HideSplitDialog();

        // 取消订阅垃圾桶事件
        if (_trashSlot != null)
            _trashSlot.OnSlotClicked -= OnSlotClicked;

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

        // 点击垃圾桶（有 pending 物品）：恢复到背包
        if (slot == _trashSlot && _pendingDeleteItem != null && !_pendingDeleteItem.IsEmpty)
        {
            RestoreFromTrash();
            return;
        }

        // 没有物品被拿起时，只能拿起非空的物品格子
        if (slot.IsPlaceholder || slot.IsEmpty)
            return;

        PickupSlot(slot);
    }

    /// <summary>
    /// 物品格子右键点击事件
    /// </summary>
    private void OnSlotRightClicked(ItemSlotUI slot, Vector2 screenPos)
    {
        if (slot == null)
            return;

        // 如果有物品被拿起，取消拿起状态
        if (_pickedUpSlot != null)
        {
            CancelPickup();
        }

        // 垃圾桶不显示上下文菜单
        if (slot == _trashSlot)
            return;

        // 显示上下文菜单（依附于格子位置）
        ShowContextMenu(slot);
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
        var layoutGroup = _pickedUpParent?.GetComponent<LayoutGroup>();
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
        int targetId = targetSlot != null ? targetSlot.GetInstanceID() : 0;
        int trashId = _trashSlot != null ? _trashSlot.GetInstanceID() : 0;
        Debug.Log($"[PlaceSlot] targetSlot={targetSlot?.name}({targetId}), IsPlaceholder={targetSlot?.IsPlaceholder}, CurrentIndex={targetSlot?.CurrentIndex}");
        Debug.Log($"[PlaceSlot] _trashSlot={_trashSlot?.name}({trashId}), _trashArea={_trashArea?.name}, AreSame={(_trashSlot != null && targetSlot != null) && (_trashSlot == targetSlot)}");

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

        // 垃圾桶：删除物品
        if (targetSlot == _trashSlot)
        {
            Debug.Log("[PlaceSlot] Trash: deleting item");
            TrashItem();
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
    /// 将物品扔进垃圾桶
    /// </summary>
    private void TrashItem()
    {
        // 获取物品数据
        int itemId = _pickedUpSlot.ItemId;
        int quantity = _pickedUpSlot.Quantity;
        ItemType itemType = _pickedUpSlot.ItemType;

        if (itemId == 0 || quantity <= 0)
        {
            Debug.Log("[TrashItem] Cannot trash empty slot");
            CancelPickup();
            return;
        }

        // 从原位置移除物品
        bool removed = false;
        if (_pickedUpParent == _equipedWeaponContainer)
        {
            // 从装备区移除：直接清空装备槽
            var equipped = SessionManager.Instance?.GetEquippedWeaponSlots();
            if (equipped != null && _pickedUpSourceIndex >= 0 && _pickedUpSourceIndex < equipped.Count)
            {
                equipped[_pickedUpSourceIndex] = new ItemSlotData();
                removed = true;

                EventCenter.Instance.Publish(GameEventID.OnInventoryChanged,
                    new InventoryChangeParams(ItemType.Weapon, 0, 0, InventoryChangeType.Remove));
            }
        }
        else
        {
            // 从背包移除
            removed = InventoryManager.Instance?.RemoveItem(itemType, itemId, quantity) ?? false;
        }

        if (removed)
        {
            // 保存到垃圾桶待恢复数据
            _pendingDeleteItem = new ItemSlotData(itemId, quantity);

            // 持久化到 SessionData
            var session = SessionManager.Instance?.CurrentSession;
            if (session != null)
            {
                session.trashPendingItem = _pendingDeleteItem;
            }

            // 更新垃圾桶显示
            _trashSlot.Initialize(itemId, quantity, itemType);
        }

        // 刷新 UI
        RefreshAllViews();
        // 清理拿起状态
        ClearPickup();

        Debug.Log($"[TrashItem] Trashed itemId={itemId}, quantity={quantity}, type={itemType}");
    }

    /// <summary>
    /// 从垃圾桶恢复物品到背包
    /// </summary>
    private void RestoreFromTrash()
    {
        if (_pendingDeleteItem == null || _pendingDeleteItem.IsEmpty)
        {
            Debug.Log("[RestoreFromTrash] Nothing to restore");
            return;
        }

        int itemId = _pendingDeleteItem.itemId;
        int quantity = _pendingDeleteItem.quantity;

        Debug.Log($"[RestoreFromTrash] Restoring itemId={itemId}, quantity={quantity}");

        // 获取物品配置以确定正确的类型
        var config = GameDataManager.Instance?.GetItemConfig(itemId);
        ItemType itemType = config?.Type ?? ItemType.Weapon;

        // 恢复到对应类型的背包
        InventoryManager.Instance?.AddItem(itemType, itemId, quantity);

        // 清空待删除数据
        _pendingDeleteItem = null;

        // 清空 SessionData 中的持久化数据
        var session = SessionManager.Instance?.CurrentSession;
        if (session != null)
        {
            session.trashPendingItem = null;
        }

        // 清空垃圾桶显示（会正确清除图标）
        _trashSlot.Initialize(0, 0, ItemType.Weapon);

        // 刷新 UI
        RefreshAllViews();

        Debug.Log($"[RestoreFromTrash] Restored itemId={itemId}, quantity={quantity}, type={itemType}");
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
        var layoutGroup = _pickedUpParent?.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_pickedUpParent);
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
        var layoutGroup = _pickedUpParent?.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_pickedUpParent);
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
            iconImg.preserveAspect = true;
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
        var layoutGroup = container.GetComponent<GridLayoutGroup>();
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
                slot.OnSlotRightClicked += OnSlotRightClicked;
                _activeEquipSlots.Add(slot);
            }
        }

        // 恢复 LayoutGroup 并强制重新计算
        if (layoutGroup != null)
        {
            layoutGroup.enabled = layoutGroupWasEnabled;
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
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
        var layoutGroup = _content.GetComponent<GridLayoutGroup>();
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
                    slot.OnSlotRightClicked += OnSlotRightClicked;
                    _activeSlots.Add(slot);
                    slotIndex++;
                }
            }
        }

        // 恢复 LayoutGroup 并强制重新计算
        if (layoutGroup != null)
        {
            layoutGroup.enabled = layoutGroupWasEnabled;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
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
        var contentSizeFitter = _contentCurrency.GetComponent<ContentSizeFitter>();
        bool fitterWasEnabled = contentSizeFitter != null && contentSizeFitter.enabled;
        if (contentSizeFitter != null)
            contentSizeFitter.enabled = false;

        // 禁用 LayoutGroup，防止添加时自动排列
        var layoutGroup = _contentCurrency.GetComponent<GridLayoutGroup>();
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
                    slot.OnSlotRightClicked += OnSlotRightClicked;
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
                slot.OnSlotRightClicked += OnSlotRightClicked;
                _activeCurrencySlots.Add(slot);
            }
        }

        // 恢复 LayoutGroup 并强制重新计算
        if (layoutGroup != null)
        {
            layoutGroup.enabled = layoutGroupWasEnabled;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentCurrency);
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

        var gridLayout = content.GetComponent<GridLayoutGroup>();
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
                slot.OnSlotRightClicked -= OnSlotRightClicked;
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
                slot.OnSlotRightClicked -= OnSlotRightClicked;
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
                slot.OnSlotRightClicked -= OnSlotRightClicked;
                PoolManager.Instance.Release(slot.gameObject);
            }
        }
        _activeCurrencySlots.Clear();
    }

    private void UpdateTrashSlotDisplay()
    {
        if (_trashSlot == null)
        {
            Debug.LogWarning("[UpdateTrashSlotDisplay] _trashSlot is null! Check Inspector assignment.");
            return;
        }

        Debug.Log($"[UpdateTrashSlotDisplay] trashSlot={_trashSlot.name}, hasComponent={_trashSlot.GetComponent<ItemSlotUI>() != null}");
        _trashSlot.gameObject.SetActive(true);

        // 订阅垃圾桶点击事件
        _trashSlot.OnSlotClicked += OnSlotClicked;

        if (_pendingDeleteItem != null && !_pendingDeleteItem.IsEmpty)
            _trashSlot.Initialize(_pendingDeleteItem.itemId, _pendingDeleteItem.quantity, ItemType.Weapon);
        else
            _trashSlot.Initialize(0, 0, ItemType.Weapon);
    }

    #region 上下文菜单

    /// <summary>
    /// 初始化上下文菜单按钮事件
    /// </summary>
    private void InitContextMenu()
    {
        if (_contextMenuRoot != null)
            _contextMenuRoot.SetActive(false);

        if (_btnUse != null)
            _btnUse.onClick.RemoveListener(OnContextMenuUse);
        _btnUse?.onClick.AddListener(OnContextMenuUse);

        if (_btnSplit != null)
            _btnSplit.onClick.RemoveListener(OnContextMenuSplit);
        _btnSplit?.onClick.AddListener(OnContextMenuSplit);
    }

    /// <summary>
    /// 显示上下文菜单（依附于格子位置）
    /// </summary>
    private void ShowContextMenu(ItemSlotUI slot)
    {
        if (slot == null || slot.IsEmpty || slot.IsPlaceholder)
            return;

        _contextMenuSlot = slot;
        _contextMenuRoot?.SetActive(true);

        // 设置位置到格子的位置
        if (_contextMenuRoot != null && slot != null)
        {
            var rt = _contextMenuRoot.transform as RectTransform;
            var slotRect = slot.RectTransform;

            // 获取格子在世界坐标系中的屏幕位置
            Vector3[] corners = new Vector3[4];
            slotRect.GetWorldCorners(corners);

            // 使用格子右上角作为菜单位置
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, corners[1]);

            // 调整为屏幕内可见（简单偏移）
            rt.position = screenPos;
        }

        // 根据物品类型显示/隐藏按钮
        bool canUse = CanUseItem(slot.ItemType);
        bool canSplit = slot.Quantity > 1;

        if (_btnUse != null)
            _btnUse.gameObject.SetActive(canUse);

        if (_btnSplit != null)
            _btnSplit.gameObject.SetActive(canSplit);
    }

    /// <summary>
    /// 隐藏上下文菜单
    /// </summary>
    private void HideContextMenu()
    {
        if (_contextMenuRoot != null)
            _contextMenuRoot.SetActive(false);
        _contextMenuSlot = null;
    }

    /// <summary>
    /// 判断物品类型是否可以使用
    /// </summary>
    private bool CanUseItem(ItemType type)
    {
        return type == ItemType.Weapon || type == ItemType.Potion;
    }

    /// <summary>
    /// 上下文菜单 - 使用
    /// </summary>
    private void OnContextMenuUse()
    {
        if (_contextMenuSlot == null)
        {
            HideContextMenu();
            return;
        }

        int itemId = _contextMenuSlot.ItemId;
        ItemType itemType = _contextMenuSlot.ItemType;
        int slotIndex = _contextMenuSlot.CurrentIndex;

        Debug.Log($"[OnContextMenuUse] itemId={itemId}, type={itemType}, slotIndex={slotIndex}");

        bool used = false;

        switch (itemType)
        {
            case ItemType.Weapon:
                // 装备武器：找到第一个空装备槽
                used = TryEquipWeapon(slotIndex);
                break;

            case ItemType.Potion:
                // 使用药水
                used = UsePotion(slotIndex);
                break;

            default:
                Debug.Log($"[OnContextMenuUse] Cannot use item type {itemType}");
                break;
        }

        HideContextMenu();

        if (used)
            RefreshAllViews();
    }

    /// <summary>
    /// 上下文菜单 - 拆分
    /// </summary>
    private void OnContextMenuSplit()
    {
        Debug.Log($"[OnContextMenuSplit] _contextMenuSlot={_contextMenuSlot?.name}, _btnSplit={_btnSplit?.name}, _splitDialogRoot={_splitDialogRoot?.name}");

        if (_contextMenuSlot == null)
        {
            Debug.LogWarning("[OnContextMenuSplit] _contextMenuSlot is null!");
            HideContextMenu();
            return;
        }

        if (_contextMenuSlot.Quantity <= 1)
        {
            Debug.Log("[OnContextMenuSplit] Cannot split single item");
            HideContextMenu();
            return;
        }

        Debug.Log($"[OnContextMenuSplit] Showing split dialog, quantity={_contextMenuSlot.Quantity}");

        // 保存 slot 引用，因为 HideContextMenu 会清空它
        var slotForSplit = _contextMenuSlot;

        // 显示拆分对话框
        ShowSplitDialog(slotForSplit);
        HideContextMenu();
    }

    /// <summary>
    /// 尝试装备武器
    /// </summary>
    private bool TryEquipWeapon(int inventorySlotIndex)
    {
        var equipped = SessionManager.Instance?.GetEquippedWeaponSlots();
        if (equipped == null)
            return false;

        // 找到第一个空装备槽
        for (int i = 0; i < equipped.Count; i++)
        {
            if (equipped[i].IsEmpty)
            {
                InventoryManager.Instance?.EquipFromInventory(inventorySlotIndex, i);
                return true;
            }
        }

        // 没有空槽，尝试和第一个装备交换
        if (equipped.Count > 0)
        {
            InventoryManager.Instance?.EquipFromInventory(inventorySlotIndex, 0);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 使用药水
    /// </summary>
    private bool UsePotion(int slotIndex)
    {
        var slots = InventoryManager.Instance?.GetInventory(ItemType.Potion);
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Count)
            return false;

        var slot = slots[slotIndex];
        if (slot.IsEmpty)
            return false;

        int itemId = slot.itemId;
        int quantity = slot.quantity;

        // TODO: 实现药水使用效果
        // 目前只是移除一个
        InventoryManager.Instance?.RemoveItem(ItemType.Potion, itemId, 1);

        Debug.Log($"[UsePotion] Used potion itemId={itemId}");
        return true;
    }

    #endregion

    #region 拆分对话框

    /// <summary>
    /// 初始化拆分对话框
    /// </summary>
    private void InitSplitDialog()
    {
        if (_splitDialogRoot != null)
            _splitDialogRoot.SetActive(false);

        if (_sliderSplitQuantity != null)
        {
            _sliderSplitQuantity.onValueChanged.RemoveListener(OnSplitSliderChanged);
            _sliderSplitQuantity.onValueChanged.AddListener(OnSplitSliderChanged);
        }

        if (_btnSplitConfirm != null)
            _btnSplitConfirm.onClick.RemoveListener(OnSplitConfirm);
        _btnSplitConfirm?.onClick.AddListener(OnSplitConfirm);

        if (_btnSplitCancel != null)
            _btnSplitCancel.onClick.RemoveListener(OnSplitCancel);
        _btnSplitCancel?.onClick.AddListener(OnSplitCancel);
    }

    /// <summary>
    /// 显示拆分对话框
    /// </summary>
    private void ShowSplitDialog(ItemSlotUI slot)
    {
        Debug.Log($"[ShowSplitDialog] slot={slot?.name}, IsEmpty={slot?.IsEmpty}, Quantity={slot?.Quantity}");

        if (slot == null || slot.IsEmpty)
        {
            Debug.LogWarning("[ShowSplitDialog] slot is null or empty, cannot show dialog");
            return;
        }

        _splitContextSlot = slot;
        _splitQuantity = (slot.Quantity + 1) / 2; // 默认一半

        Debug.Log($"[ShowSplitDialog] _sliderSplitQuantity={_sliderSplitQuantity?.name}, _splitDialogRoot={_splitDialogRoot?.name}");

        if (_sliderSplitQuantity != null)
        {
            _sliderSplitQuantity.minValue = 1;
            _sliderSplitQuantity.maxValue = slot.Quantity - 1;
            _sliderSplitQuantity.value = _splitQuantity;
        }

        UpdateSplitQuantityText();

        if (_splitDialogRoot != null)
        {
            _splitDialogRoot.SetActive(true);
            Debug.Log("[ShowSplitDialog] Split dialog shown");
        }
        else
        {
            Debug.LogWarning("[ShowSplitDialog] _splitDialogRoot is null! Check Inspector assignment.");
        }
    }

    /// <summary>
    /// 隐藏拆分对话框
    /// </summary>
    private void HideSplitDialog()
    {
        if (_splitDialogRoot != null)
            _splitDialogRoot.SetActive(false);
        _splitContextSlot = null;
    }

    /// <summary>
    /// 更新拆分数量文本
    /// </summary>
    private void UpdateSplitQuantityText()
    {
        if (_txtSplitQuantity != null)
            _txtSplitQuantity.text = _splitQuantity.ToString();
    }

    /// <summary>
    /// 拆分滑块值改变
    /// </summary>
    private void OnSplitSliderChanged(float value)
    {
        _splitQuantity = Mathf.RoundToInt(value);
        UpdateSplitQuantityText();
    }

    /// <summary>
    /// 确认拆分
    /// </summary>
    private void OnSplitConfirm()
    {
        Debug.Log($"[OnSplitConfirm] Called! _splitContextSlot={_splitContextSlot?.name}, _splitQuantity={_splitQuantity}");

        if (_splitContextSlot == null || _splitQuantity <= 0)
        {
            Debug.LogWarning("[OnSplitConfirm] Early exit: _splitContextSlot is null or _splitQuantity <= 0");
            HideSplitDialog();
            return;
        }

        int originalQuantity = _splitContextSlot.Quantity;
        int itemId = _splitContextSlot.ItemId;
        ItemType itemType = _splitContextSlot.ItemType;
        int slotIndex = _splitContextSlot.CurrentIndex;

        if (_splitQuantity >= originalQuantity)
        {
            Debug.Log($"[OnSplitConfirm] Split quantity { _splitQuantity} >= original {originalQuantity}");
            HideSplitDialog();
            return;
        }

        Debug.Log($"[OnSplitConfirm] Splitting itemId={itemId}, type={itemType}, original={originalQuantity}, split={_splitQuantity}, remain={originalQuantity - _splitQuantity}");

        // 剩余数量
        int remainQuantity = originalQuantity - _splitQuantity;

        // 直接操作 InventoryManager 的底层数据，拆分物品
        // 1. 在原位置保留 remainQuantity
        // 2. 找一个空格子放置 splitQuantity
        InventoryManager.Instance?.SplitItem(slotIndex, itemType, itemId, originalQuantity, _splitQuantity);

        HideSplitDialog();
        RefreshAllViews();
    }

    /// <summary>
    /// 取消拆分
    /// </summary>
    private void OnSplitCancel()
    {
        HideSplitDialog();
    }

    #endregion

    #endregion

    #region 日志

    private void Log(string msg) => Debug.Log($"[InventoryPanel] {msg}");
    private void LogError(string msg) => Debug.LogError($"[InventoryPanel] {msg}");

    #endregion
}
