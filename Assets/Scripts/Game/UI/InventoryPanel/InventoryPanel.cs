using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    private const string TXT_MAX_HEALTH = "txt_maxHealth";
    private const string TXT_ATK = "txt_atk";
    private const string TXT_DEF = "txt_def";
    private const string TXT_SPEED = "txt_speed";
    private const string TXT_DASH_SPEED = "txt_dashSpeed";
    private const string TXT_DASH_DURATION = "txt_dashDuration";
    private const string TXT_DASH_GAP = "txt_dashGap";
    private const string TXT_INVINCIBLE = "txt_invincible";
    private const string TXT_HURT_DURATION = "txt_hurtDuration";
    private const string TXT_CRIT_RATE = "txt_critRate";
    private const string TXT_CRIT_MULT = "txt_critMult";
    private const string TXT_DAMAGE_BONUS = "txt_damageBonus";
    private const string TXT_DEF_BREAK = "txt_defBreak";
    private const string TAB_WEAPON = "tog_tab_weapon";
    private const string TAB_POTION = "tog_tab_potion";
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
    [SerializeField] private TMP_Text _txtUseButton;
    [SerializeField] private Button _btnSplit;

    [Header("拆分窗口")]
    [SerializeField] private GameObject _splitDialogRoot;
    [SerializeField] private Slider _sliderSplitQuantity;
    [SerializeField] private TMP_Text _txtSplitQuantity;
    [SerializeField] private Button _btnSplitConfirm;
    [SerializeField] private Button _btnSplitCancel;

    [Header("物品提示")]
    [SerializeField] private GameObject _tooltipRoot;
    [SerializeField] private TMP_Text _tooltipTitle;
    [SerializeField] private TMP_Text _tooltipDescription;

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

    // 刷新计时器
    private Coroutine _buffRefreshCoroutine;
    private const float BUFF_REFRESH_INTERVAL = 0.2f; // 每0.2秒刷新一次buff显示

    // 物品提示
    private Coroutine _tooltipCoroutine;
    private const float TOOLTIP_DELAY = 0.3f; // 悬停0.3秒后显示提示
    private ItemSlotUI _hoveredSlot;

    #endregion

    #region BasePanel override

    public override void Show()
    {
        base.Show();

        // 初始化时隐藏 tooltip
        HideTooltip();


        // 从 SessionData 恢复 pending 物品
        var session = SessionManager.Instance?.CurrentSession;
        if (session?.trashPendingItem != null && !session.trashPendingItem.IsEmpty)
        {
            _pendingDeleteItem = session.trashPendingItem;
        }
        else
        {
            _pendingDeleteItem = null;
        }

        EventCenter.Instance.Subscribe<InventoryChangeParams>(GameEventID.OnInventoryChanged, OnInventoryChanged);
        EventCenter.Instance.Subscribe(GameEventID.OnBuffChanged, OnBuffChanged);

        // 启动buff刷新计时器
        _buffRefreshCoroutine = StartCoroutine(BuffRefreshLoop());

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
        EventCenter.Instance.Unsubscribe(GameEventID.OnBuffChanged, OnBuffChanged);

        // 停止buff刷新计时器
        if (_buffRefreshCoroutine != null)
        {
            StopCoroutine(_buffRefreshCoroutine);
            _buffRefreshCoroutine = null;
        }

        // 停止提示计时器并隐藏提示
        if (_tooltipCoroutine != null)
        {
            StopCoroutine(_tooltipCoroutine);
            _tooltipCoroutine = null;
        }
        HideTooltip();

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

        // 检测左键点击来关闭 context menu
        if (_contextMenuRoot != null && _contextMenuRoot.activeSelf)
        {
            if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                // 如果点击在 context menu 范围内，不关闭（让按钮处理）
                if (IsPointOverRectTransform(_contextMenuRoot, mousePos))
                    return;
                HideContextMenu();
            }
        }
    }

    /// <summary>
    /// 检测屏幕坐标是否在 RectTransform 范围内
    /// </summary>
    private bool IsPointOverRectTransform(GameObject obj, Vector2 screenPos)
    {
        if (obj == null)
            return false;
        var rt = obj.transform as RectTransform;
        if (rt == null)
            return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
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

        if (_pickedUpSlot == null)
        {
            return;
        }

        // 点击的是 placeholder（源位置），取消拿起
        if (targetSlot != null && targetSlot.IsPlaceholder && targetSlot.CurrentIndex == _pickedUpSourceIndex)
        {
            CancelPickup();
            return;
        }

        // 不能放在 placeholder 上（其他位置的 placeholder）
        if (targetSlot == null || targetSlot.IsPlaceholder)
        {
            CancelPickup();
            return;
        }

        // 垃圾桶：删除物品
        if (targetSlot == _trashSlot)
        {
            TrashItem();
            return;
        }

        // 执行交换/移动（TrySwapOrMove 会检查类型兼容性）
        bool success = TrySwapOrMove(targetSlot);

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

    }

    /// <summary>
    /// 从垃圾桶恢复物品到背包
    /// </summary>
    private void RestoreFromTrash()
    {
        if (_pendingDeleteItem == null || _pendingDeleteItem.IsEmpty)
        {
                return;
        }

        int itemId = _pendingDeleteItem.itemId;
        int quantity = _pendingDeleteItem.quantity;


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


        // 装备区 < -> 装备区：交换
        if (sourceIsEquip && targetIsEquip)
        {
            InventoryManager.Instance?.SwapEquippedSlots(sourceIndex, targetIndex);
            return true;
        }

        // 背包 -> 装备区（仅武器）
        if (!sourceIsEquip && targetIsEquip && sourceType == ItemType.Weapon)
        {
            InventoryManager.Instance?.EquipFromInventory(sourceIndex, targetIndex);
            return true;
        }

        // 装备区 -> 背包（仅武器区域）
        if (sourceIsEquip && !targetIsEquip && targetType == ItemType.Weapon)
        {
            InventoryManager.Instance?.UnequipWeapon(sourceIndex, targetIndex);
            return true;
        }

        // 背包内移动：交换数据（必须是同性之间）
        if (sourceType == targetType)
        {
            InventoryManager.Instance?.MoveSlot(sourceType, sourceIndex, targetIndex);
            return true;
        }

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
        RefreshCharacterInfo();
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

    private void OnBuffChanged()
    {
        RefreshCharacterInfo();
    }

    /// <summary>
    /// buff刷新循环（定时刷新以显示剩余时间）
    /// </summary>
    private IEnumerator BuffRefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(BUFF_REFRESH_INTERVAL);
            RefreshCharacterInfo();
        }
    }

    #region 物品提示

    /// <summary>
    /// 格子悬停进入
    /// </summary>
    private void OnSlotHoverEnter(ItemSlotUI slot)
    {
        if (slot == null || slot.IsEmpty || slot.IsPlaceholder)
            return;

        _hoveredSlot = slot;
        // 延迟显示提示
        if (_tooltipCoroutine != null)
            StopCoroutine(_tooltipCoroutine);
        _tooltipCoroutine = StartCoroutine(ShowTooltipDelayed(slot));
    }

    /// <summary>
    /// 格子悬停退出
    /// </summary>
    private void OnSlotHoverExit(ItemSlotUI slot)
    {
        _hoveredSlot = null;
        if (_tooltipCoroutine != null)
        {
            StopCoroutine(_tooltipCoroutine);
            _tooltipCoroutine = null;
        }
        HideTooltip();
    }

    /// <summary>
    /// 延迟显示提示
    /// </summary>
    private IEnumerator ShowTooltipDelayed(ItemSlotUI slot)
    {
        yield return new WaitForSeconds(TOOLTIP_DELAY);

        // 检查是否仍然悬停在同一格子
        if (_hoveredSlot != slot)
            yield break;

        ShowTooltip(slot);
    }

    /// <summary>
    /// 显示物品提示
    /// </summary>
    private void ShowTooltip(ItemSlotUI slot)
    {
        if (_tooltipRoot == null || slot.IsEmpty)
            return;

        // 获取物品配置
        var config = GameDataManager.Instance?.GetItemConfig(slot.ItemId);
        if (config == null)
            return;

        // 构建标题和描述
        string title = $"<B>{config.Name ?? "Unknown"}</B>";
        if (slot.Quantity > 1)
            title += $" <color=#FFFFFF>x{slot.Quantity}</color>";

        string description = BuildItemDescription(config);

        // 设置提示内容
        if (_tooltipTitle != null)
            _tooltipTitle.text = title;
        if (_tooltipDescription != null)
            _tooltipDescription.text = description;

        // 显示提示
        _tooltipRoot.SetActive(true);

        // 禁用 tooltip 的射线检测，防止遮挡 item slot 触发 PointerExit
        var canvasGroup = _tooltipRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = _tooltipRoot.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        // 更新位置
        UpdateTooltipPosition();
    }

    /// <summary>
    /// 计算 tooltip 显示位置（显示在鼠标左下）
    /// </summary>
    private void UpdateTooltipPosition()
    {
        if (_tooltipRoot == null)
            return;

        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        var rt = _tooltipRoot.transform as RectTransform;
        if (rt == null || rt.parent == null)
            return;

        // 将屏幕坐标转换为 tooltip 父对象内的局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform,
            mousePos,
            null, // Camera 对于 Screen Space Overlay 填 null
            out Vector2 localPoint
        );

        // 偏移到鼠标左下角（向左偏移宽度，向下偏移高度）
        // 由于 pivot 是 (0.5, 0.5) 中心对齐，需要偏移半个尺寸
        float tooltipWidth = rt.rect.width;
        float tooltipHeight = rt.rect.height;
        localPoint.x -= tooltipWidth * 0.5f + 10f; // 向左偏移
        localPoint.y -= tooltipHeight * 0.5f + 10f; // 向下偏移

        // 设置 anchoredPosition 来定位 tooltip
        rt.anchoredPosition = localPoint;
    }

    /// <summary>
    /// 隐藏物品提示
    /// </summary>
    private void HideTooltip()
    {
        if (_tooltipRoot != null)
            _tooltipRoot.SetActive(false);
    }

    /// <summary>
    /// 根据物品配置构建描述信息（复用 LootItem 的逻辑）
    /// </summary>
    private string BuildItemDescription(ItemConfig config)
    {
        if (config == null)
            return "No description available.";

        switch (config.Type)
        {
            case ItemType.Weapon:
                var weaponConfig = GameDataManager.Instance?.GetWeaponConfig(config.Id);
                if (weaponConfig != null)
                {
                    return BuildWeaponDescription(weaponConfig, config);
                }
                return config.Description ?? "";

            case ItemType.Potion:
                return BuildPotionDescription(config);

            case ItemType.Relic:
                return BuildRelicDescription(config);

            default:
                return config.Description ?? "";
        }
    }

    /// <summary>
    /// 构建武器描述信息
    /// </summary>
    private string BuildWeaponDescription(WeaponConfig weaponConfig, ItemConfig itemConfig)
    {
        var sb = new StringBuilder();

        // 武器类型
        string fireModeName = weaponConfig.fireMode switch
        {
            FireMode.Single => "单发",
            FireMode.Spread => "霰弹",
            FireMode.Burst => "连发",
            FireMode.Continuous => "激光",
            FireMode.Charge => "蓄力",
            _ => weaponConfig.fireMode.ToString()
        };
        sb.AppendLine($"类型: {fireModeName}");
        sb.AppendLine($"射速: {1f / weaponConfig.fireRate:F1}/秒");

        // 子弹属性
        if (weaponConfig.bulletConfig != null)
        {
            sb.AppendLine($"子弹伤害: {weaponConfig.bulletConfig.baseDamage}");
            sb.AppendLine($"子弹速度: {weaponConfig.bulletConfig.bulletSpeed:F0}");
        }

        sb.AppendLine($"弹夹: {weaponConfig.magazineSize} 发");

        // 霰弹模式
        if (weaponConfig.fireMode == FireMode.Spread)
        {
            if (weaponConfig.bulletCount > 1)
                sb.AppendLine($"弹丸: {weaponConfig.bulletCount}");
            if (weaponConfig.shotgunSpreadAngle > 0)
                sb.AppendLine($"散布: {weaponConfig.shotgunSpreadAngle}°");
        }

        // 连发模式
        if (weaponConfig.fireMode == FireMode.Burst)
        {
            sb.AppendLine($"连发: {weaponConfig.burstCount} 发");
            if (weaponConfig.burstSpeed > 0)
                sb.AppendLine($"连发间隔: {weaponConfig.burstSpeed:F2}秒");
        }

        // 蓄力模式
        if (weaponConfig.fireMode == FireMode.Charge && weaponConfig.chargeTime > 0)
        {
            sb.AppendLine($"蓄力: {weaponConfig.chargeTime:F1}秒");
        }

        // 散布
        if (weaponConfig.randomSpreadAngle > 0)
            sb.AppendLine($"散布: ±{weaponConfig.randomSpreadAngle}°");

        // 穿透
        if (weaponConfig.penetrateCount > 0)
            sb.AppendLine($"穿透: {weaponConfig.penetrateCount}层");

        // 后坐力
        if (weaponConfig.recoilForce > 0)
            sb.AppendLine($"后坐力: {weaponConfig.recoilForce:F1}");

        sb.AppendLine($"换弹: {weaponConfig.reloadTime:F1}秒");

        // 伤害属性
        bool hasDamageStats = weaponConfig.weaponDamage > 0
            || weaponConfig.weaponCritRate > 0
            || weaponConfig.weaponCritMultiplier > 0
            || weaponConfig.weaponDamageBonus > 0;

        if (hasDamageStats)
        {
            sb.AppendLine();
            sb.AppendLine("伤害属性:");
            if (weaponConfig.weaponDamage > 0)
                sb.AppendLine($"  武器伤害: +{weaponConfig.weaponDamage:F1}");
            if (weaponConfig.weaponCritRate > 0)
                sb.AppendLine($"  暴击率: +{weaponConfig.weaponCritRate:P0}");
            if (weaponConfig.weaponCritMultiplier > 0)
                sb.AppendLine($"  暴击倍率: +{weaponConfig.weaponCritMultiplier:P0}");
            if (weaponConfig.weaponDamageBonus > 0)
                sb.AppendLine($"  伤害加成: +{weaponConfig.weaponDamageBonus:P0}");
        }

        sb.AppendLine();
        if (!string.IsNullOrEmpty(itemConfig.Description))
        {
            sb.Append(itemConfig.Description);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建药水描述信息
    /// </summary>
    private string BuildPotionDescription(ItemConfig config)
    {
        var potionConfig = config as PotionItemConfig;
        if (potionConfig == null)
            return config.Description ?? "Unknown potion";

        var sb = new StringBuilder();

        // 即时效果
        if (potionConfig.instantEffectType != PotionInstantEffectType.None
            && potionConfig.instantEffectValue > 0)
        {
            string effectName = potionConfig.instantEffectType switch
            {
                PotionInstantEffectType.Heal => "恢复生命",
                _ => potionConfig.instantEffectType.ToString()
            };
            sb.AppendLine($"立即: {effectName} +{potionConfig.instantEffectValue:F0}");
        }

        // 限时效果
        if (potionConfig.timedEffectType != PotionTimedEffectType.None
            && potionConfig.timedEffectDuration > 0
            && potionConfig.timedEffectValue > 0)
        {
            string effectName = potionConfig.timedEffectType switch
            {
                PotionTimedEffectType.Shield => "护盾",
                PotionTimedEffectType.SpeedBoost => "加速",
                _ => potionConfig.timedEffectType.ToString()
            };

            if (potionConfig.timedEffectType == PotionTimedEffectType.SpeedBoost)
            {
                sb.AppendLine($"限时: {effectName} +{potionConfig.timedEffectValue:F0}% 速度");
            }
            else
            {
                sb.AppendLine($"限时: {effectName} +{potionConfig.timedEffectValue:F1}");
            }
            sb.AppendLine($"持续: {potionConfig.timedEffectDuration:F1}秒");
        }

        sb.AppendLine();
        if (!string.IsNullOrEmpty(config.Description))
        {
            sb.Append(config.Description);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建遗物描述信息
    /// </summary>
    private string BuildRelicDescription(ItemConfig config)
    {
        var relicConfig = config as RelicConfig;
        if (relicConfig == null)
            return config.Description ?? "Unknown relic";

        var sb = new StringBuilder();

        // 属性加成（只显示有值的）
        var validModifiers = relicConfig.modifiers?.FindAll(m => m.value != 0f) ?? new List<ModifierData>();
        if (validModifiers.Count > 0)
        {
            sb.AppendLine("属性加成:");
            foreach (var mod in validModifiers)
            {
                string modName = GetModifierDisplayName(mod.type);
                string valueStr = IsPercentModifier(mod.type)
                    ? $"{mod.value * 100:F0}%"
                    : $"+{mod.value:F1}";
                sb.AppendLine($"  {modName}: {valueStr}");
            }
        }

        // 遗物效果（只显示有值的）
        var validEffects = relicConfig.effects?.FindAll(e => !string.IsNullOrEmpty(GetRelicEffectDescription(e))) ?? new List<RelicEffect>();
        if (validEffects.Count > 0)
        {
            sb.AppendLine("遗物效果:");
            foreach (var effect in validEffects)
            {
                string effectDesc = GetRelicEffectDescription(effect);
                if (!string.IsNullOrEmpty(effectDesc))
                {
                    sb.AppendLine($"  {effectDesc}");
                }
            }
        }

        sb.AppendLine();
        if (!string.IsNullOrEmpty(config.Description))
        {
            sb.Append(config.Description);
        }

        return sb.ToString();
    }

    #endregion

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
            TAB_POTION => ItemType.Potion,
            TAB_RELIC => ItemType.Relic,
            _ => null
        };
    }

    /// <summary>
    /// 获取修饰符显示名称
    /// </summary>
    private string GetModifierDisplayName(ModifierType type)
    {
        return type switch
        {
            ModifierType.MaxHealth => "最大生命",
            ModifierType.Attack => "攻击力",
            ModifierType.Defense => "防御力",
            ModifierType.MoveSpeed => "移动速度",
            ModifierType.DashSpeed => "冲刺速度",
            ModifierType.DashDuration => "冲刺持续",
            ModifierType.InvincibleDuration => "无敌时间",
            ModifierType.HurtDuration => "受伤持续",
            ModifierType.CritRate => "暴击率",
            ModifierType.CritMultiplier => "暴击倍率",
            ModifierType.DamageBonus => "伤害加成",
            ModifierType.DefBreak => "防御穿透",
            _ => type.ToString()
        };
    }

    /// <summary>
    /// 判断是否为百分比修饰符
    /// </summary>
    private bool IsPercentModifier(ModifierType type)
    {
        return type is ModifierType.CritRate
            or ModifierType.CritMultiplier
            or ModifierType.DamageBonus
            or ModifierType.DefBreak;
    }

    /// <summary>
    /// 获取遗物效果描述
    /// </summary>
    private string GetRelicEffectDescription(RelicEffect effect)
    {
        return effect switch
        {
            DungeonGenerationEffect dge when dge.extraEliteChance > 0 || dge.extraTreasureChance > 0 =>
                $"额外精英怪 +{dge.extraEliteChance}%, 额外宝箱 +{dge.extraTreasureChance}%",
            RoomBehaviorEffect rbe when rbe.enemyCountBonus != 0 || rbe.eliteChanceBonus > 0 || rbe.lootMultiplier > 1f =>
                $"敌人波次 +{rbe.enemyCountBonus}, 精英概率 +{rbe.eliteChanceBonus * 100:F0}%, 掉落 +{(rbe.lootMultiplier - 1f) * 100:F0}%",
            _ => null
        };
    }

    #endregion

    #region 角色信息刷新

    private void RefreshCharacterInfo()
    {
        PlayerRuntimeData playerData;
        RoleStaticData roleData;
        PlayerMetaData metaData;
        List<ModifierData> modifiers;
        bool hasActiveSession = SessionManager.Instance?.HasActiveSession == true;

        if (hasActiveSession)
        {
            // 直接从 Player._playerData 获取（与 HeartSystem 使用同一份数据）
            var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
            playerData = player?.RuntimeData;
            if (playerData == null)
            {
                playerData = SessionManager.Instance?.GetPlayerData();
            }
            roleData = playerData != null ? GameDataManager.Instance?.GetRoleStaticData(playerData.id) : null;
            metaData = SaveLoadManager.Instance?.CurrentSaveData?.metaData;
            modifiers = SessionManager.Instance?.GetModifiers();
        }
        else
        {
            int roleId = SaveLoadManager.Instance?.LastSelectedRoleId ?? 0;
            roleData = GameDataManager.Instance?.GetRoleStaticData(roleId);
            playerData = roleData != null ? PlayerRuntimeData.CreateBasic(roleData) : null;
            metaData = SaveLoadManager.Instance?.CurrentSaveData?.metaData;
            modifiers = null;
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

        // 计算各属性加成
        float metaHealthBonus = metaData?.globalMaxHealthBonus ?? 0f;
        float metaAtkBonus = metaData?.globalAtkBonus ?? 0f;
        float metaDefBonus = metaData?.globalDefBonus ?? 0f;
        float metaCritRateBonus = metaData?.globalCritRateBonus ?? 0f;
        float metaCritMultBonus = metaData?.globalCritMultiplierBonus ?? 0f;
        float metaDamageBonus = metaData?.globalDamageBonus ?? 0f;
        float metaDefBreakBonus = metaData?.globalDefBreakBonus ?? 0f;

        // 基础值
        float baseMaxHealth = roleData.baseMaxHealth;
        float baseAtk = roleData.baseAtk;
        float baseDef = roleData.baseDef;
        float baseMoveSpeed = roleData.baseMoveSpeed;
        float baseDashSpeed = roleData.dashSpeed;
        float baseDashDuration = roleData.dashDuration;
        float baseDashGap = roleData.dashGap;
        float baseInvincibleDuration = roleData.invincibleDuration;
        float baseHurtDuration = roleData.hurtDuration;
        float baseCritRate = roleData.baseCritRate;
        float baseCritMult = roleData.baseCritMultiplier;
        float baseDamageBonus = roleData.baseDamageBonus;
        float baseDefBreak = roleData.baseDefBreak;

        // Modifier 加成
        float modMaxHealth = GetModifierBonus(ModifierType.MaxHealth, modifiers);
        float modAtk = GetModifierBonus(ModifierType.Attack, modifiers);
        float modDef = GetModifierBonus(ModifierType.Defense, modifiers);
        float modMoveSpeed = GetModifierBonus(ModifierType.MoveSpeed, modifiers);
        float modDashSpeed = GetModifierBonus(ModifierType.DashSpeed, modifiers);
        float modDashDuration = GetModifierBonus(ModifierType.DashDuration, modifiers);
        float modInvincible = GetModifierBonus(ModifierType.InvincibleDuration, modifiers);
        float modHurtDuration = GetModifierBonus(ModifierType.HurtDuration, modifiers);
        float modCritRate = GetModifierBonus(ModifierType.CritRate, modifiers);
        float modCritMult = GetModifierBonus(ModifierType.CritMultiplier, modifiers);
        float modDamageBonus = GetModifierBonus(ModifierType.DamageBonus, modifiers);
        float modDefBreak = GetModifierBonus(ModifierType.DefBreak, modifiers);

        // 药水持续效果
        var potionEffects = GetPotionTimedEffects();

        // 当前生命值（无加成）
        SetText(TXT_HEALTH, $"生命: {playerData.Health:F1}");

        // 最大生命值（白色基础 + 黄色(meta+mod)）
        SetText(TXT_MAX_HEALTH, $"最大生命: <color=#FFFFFF>{baseMaxHealth:F0}</color>{BuildBonusText(metaHealthBonus, modMaxHealth)}");

        // 普通数值属性：白色基础值 + 黄色加成 + 紫色(potion效果)
        SetText(TXT_ATK, $"攻击: <color=#FFFFFF>{baseAtk:F1}</color>{BuildBonusText(metaAtkBonus, modAtk)}");
        SetText(TXT_DEF, $"防御: <color=#FFFFFF>{baseDef:F1}</color>{BuildBonusText(metaDefBonus, modDef)}{BuildPotionEffectText(potionEffects, BuffType.Shield)}");
        SetText(TXT_SPEED, $"速度: <color=#FFFFFF>{baseMoveSpeed:F1}</color>{BuildBonusText(0, modMoveSpeed)}{BuildPotionEffectText(potionEffects, BuffType.SpeedBoost)}");
        SetText(TXT_DASH_SPEED, $"冲刺速度: <color=#FFFFFF>{baseDashSpeed:F1}</color>{BuildBonusText(0, modDashSpeed)}");
        SetText(TXT_DASH_DURATION, $"冲刺持续: <color=#FFFFFF>{baseDashDuration:F2}s</color>{BuildBonusText(0, modDashDuration, "s")}");
        SetText(TXT_DASH_GAP, $"冲刺间隔: <color=#FFFFFF>{baseDashGap:F2}s</color>");
        SetText(TXT_INVINCIBLE, $"无敌: <color=#FFFFFF>{baseInvincibleDuration:F2}s</color>{BuildBonusText(0, modInvincible, "s")}");
        SetText(TXT_HURT_DURATION, $"受伤持续: <color=#FFFFFF>{baseHurtDuration:F2}s</color>{BuildBonusText(0, modHurtDuration, "s")}");

        // 战斗属性（百分比）：白色基础值 + 黄色加成
        SetText(TXT_CRIT_RATE, $"暴击率: <color=#FFFFFF>{baseCritRate:P0}</color>{BuildPercentBonusText(metaCritRateBonus, modCritRate)}");
        SetText(TXT_CRIT_MULT, $"暴击倍率: <color=#FFFFFF>{baseCritMult:P0}</color>{BuildPercentBonusText(metaCritMultBonus, modCritMult)}");
        SetText(TXT_DAMAGE_BONUS, $"伤害加成: <color=#FFFFFF>{baseDamageBonus:P0}</color>{BuildPercentBonusText(metaDamageBonus, modDamageBonus)}");
        SetText(TXT_DEF_BREAK, $"防御穿透: <color=#FFFFFF>{baseDefBreak:P0}</color>{BuildPercentBonusText(metaDefBreakBonus, modDefBreak)}");
    }

    /// <summary>
    /// 构建加成文本（黄色显示）
    /// </summary>
    private string BuildBonusText(float metaBonus, float modBonus, string suffix = "")
    {
        float totalBonus = metaBonus + modBonus;
        if (Mathf.Abs(totalBonus) < 0.001f)
            return "";

        string sign = totalBonus >= 0 ? "+" : "";
        return $"<color=#FFFF00>{sign}{totalBonus:F2}{suffix}</color>";
    }

    /// <summary>
    /// 构建百分比加成文本（黄色显示）
    /// </summary>
    private string BuildPercentBonusText(float metaBonus, float modBonus)
    {
        float totalBonus = metaBonus + modBonus;
        if (Mathf.Abs(totalBonus) < 0.001f)
            return "";

        string sign = totalBonus >= 0 ? "+" : "";
        return $"<color=#FFFF00>{sign}{totalBonus:P0}</color>";
    }

    /// <summary>
    /// 构建药水效果文本（根据类型获取对应效果）
    /// </summary>
    private string BuildPotionEffectText(List<PotionEffectInfo> effects, BuffType targetType)
    {
        var effect = effects.Find(e => e.type == targetType);
        if (effect == null)
            return "";

        float value = effect.value;
        float remainingTime = effect.remainingTime;

        // 根据类型计算显示值
        float displayValue = value;
        string suffix = "";

        if (targetType == BuffType.SpeedBoost)
        {
            // SpeedBoost 的 value 是倍率，如 1.5 表示 +50%
            displayValue = (value - 1f) * 100f;
            suffix = "%";
        }

        if (Mathf.Abs(displayValue) < 0.001f)
            return "";

        string sign = displayValue >= 0 ? "+" : "";
        string timeStr = remainingTime > 0 ? $" <color=#FF00FF>({remainingTime:F0}s)</color>" : "";
        return $"<color=#FF00FF>{sign}{displayValue:F0}{suffix}</color>{timeStr}";
    }

    /// <summary>
    /// 获取指定类型的修饰器总加成
    /// </summary>
    private float GetModifierBonus(ModifierType type, List<ModifierData> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
            return 0f;

        float total = 0f;
        foreach (var mod in modifiers)
        {
            if (mod.type == type)
                total += mod.value;
        }
        return total;
    }

    /// <summary>
    /// 获取药水限时效果列表
    /// </summary>
    private List<PotionEffectInfo> GetPotionTimedEffects()
    {
        if (BuffManager.Instance == null)
            return new List<PotionEffectInfo>();

        var effects = new List<PotionEffectInfo>();

        var activeBuffs = BuffManager.Instance.GetAllActiveBuffs();
        foreach (var buff in activeBuffs)
        {
            if (buff.IsExpired)
                continue;

            // 从药水来源的buff中提取效果值
            if (buff.sourceId.StartsWith("potion_"))
            {
                effects.Add(new PotionEffectInfo
                {
                    type = buff.type,
                    value = buff.value,
                    remainingTime = buff.remainingTime
                });
            }
        }

        return effects;
    }

    private void SetText(string controlName, string text)
    {
        var txt = GetControl<TMP_Text>(controlName);
        if (txt != null)
            txt.text = text;
    }

    #endregion

    #region 物品列表刷新

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
                slot.OnSlotHoverEnter += OnSlotHoverEnter;
                slot.OnSlotHoverExit += OnSlotHoverExit;
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
                    slot.OnSlotHoverEnter += OnSlotHoverEnter;
                    slot.OnSlotHoverExit += OnSlotHoverExit;
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
                    slot.OnSlotHoverEnter += OnSlotHoverEnter;
                    slot.OnSlotHoverExit += OnSlotHoverExit;
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
                slot.OnSlotHoverEnter += OnSlotHoverEnter;
                slot.OnSlotHoverExit += OnSlotHoverExit;
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
                slot.OnSlotHoverEnter -= OnSlotHoverEnter;
                slot.OnSlotHoverExit -= OnSlotHoverExit;
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
                slot.OnSlotHoverEnter -= OnSlotHoverEnter;
                slot.OnSlotHoverExit -= OnSlotHoverExit;
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
                slot.OnSlotHoverEnter -= OnSlotHoverEnter;
                slot.OnSlotHoverExit -= OnSlotHoverExit;
                PoolManager.Instance.Release(slot.gameObject);
            }
        }
        _activeCurrencySlots.Clear();
    }

    private void UpdateTrashSlotDisplay()
    {
        if (_trashSlot == null)
        {
            return;
        }

        _trashSlot.gameObject.SetActive(true);

        // 订阅垃圾桶点击事件
        _trashSlot.OnSlotClicked += OnSlotClicked;

        if (_pendingDeleteItem != null && !_pendingDeleteItem.IsEmpty)
            _trashSlot.Initialize(_pendingDeleteItem.itemId, _pendingDeleteItem.quantity, ItemType.Weapon);
        else
            _trashSlot.Initialize(0, 0, ItemType.Weapon);
    }

    #endregion

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
    /// 显示上下文菜单（显示在鼠标右边）
    /// </summary>
    private void ShowContextMenu(ItemSlotUI slot)
    {
        if (slot == null || slot.IsEmpty || slot.IsPlaceholder)
            return;

        _contextMenuSlot = slot;
        _contextMenuRoot?.SetActive(true);

        // 设置位置到鼠标右边
        if (_contextMenuRoot != null)
        {
            var rt = _contextMenuRoot.transform as RectTransform;

            // 获取鼠标屏幕位置
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

            // 转换为父对象内的局部坐标
            if (rt.parent != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rt.parent as RectTransform,
                    mousePos,
                    null,
                    out Vector2 localPos
                );

                // 偏移到鼠标右边
                float menuWidth = rt.rect.width;
                localPos.x += menuWidth * 0.5f + 10f;

                rt.anchoredPosition = localPos;
            }
            else
            {
                rt.position = mousePos;
            }
        }

        // 判断是否来自装备区
        bool isFromEquipArea = slot.transform.parent as RectTransform == _equipedWeaponContainer;

        // 根据物品类型和位置设置按钮文本
        var (canUse, buttonText) = GetContextMenuButtonInfo(slot.ItemType, isFromEquipArea);
        bool canSplit = slot.Quantity > 1 && CanSplitItem(slot.ItemType);

        if (_btnUse != null)
        {
            _btnUse.gameObject.SetActive(canUse);
            if (canUse && _txtUseButton != null)
                _txtUseButton.text = buttonText;
        }

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
    /// 判断物品类型是否可以使用，并返回按钮文本
    /// </summary>
    private (bool canUse, string buttonText) GetContextMenuButtonInfo(ItemType type, bool isFromEquipArea)
    {
        switch (type)
        {
            case ItemType.Weapon:
                // 装备区的武器 → 卸下，背包的武器 → 装备
                return (true, isFromEquipArea ? "卸下" : "装备");

            case ItemType.Potion:
                // 药水 → 使用
                return (true, "使用");

            default:
                // 遗物在背包中自动生效，金币不可使用
                return (false, "");
        }
    }

    /// <summary>
    /// 判断物品类型是否可以拆分
    /// </summary>
    private bool CanSplitItem(ItemType type)
    {
        return type == ItemType.Potion || type == ItemType.Relic || type == ItemType.Currency;
    }

    /// <summary>
    /// 判断物品类型是否可以使用
    /// </summary>
    private bool CanUseItem(ItemType type)
    {
        return type == ItemType.Weapon || type == ItemType.Potion;
    }

    /// <summary>
    /// 上下文菜单 - 使用/装备/卸下
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
        bool isFromEquipArea = _contextMenuSlot.transform.parent as RectTransform == _equipedWeaponContainer;


        bool used = false;

        switch (itemType)
        {
            case ItemType.Weapon:
                if (isFromEquipArea)
                    used = TryUnequipWeapon(slotIndex);
                else
                    used = TryEquipWeapon(slotIndex);
                break;

            case ItemType.Potion:
                // 使用药水
                used = UsePotion(slotIndex);
                break;

            default:
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

        if (_contextMenuSlot == null)
        {
            HideContextMenu();
            return;
        }

        if (_contextMenuSlot.Quantity <= 1)
        {
            HideContextMenu();
            return;
        }


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
    /// 尝试卸下武器
    /// </summary>
    private bool TryUnequipWeapon(int equipSlotIndex)
    {
        var equipped = SessionManager.Instance?.GetEquippedWeaponSlots();
        if (equipped == null || equipSlotIndex < 0 || equipSlotIndex >= equipped.Count)
            return false;

        if (equipped[equipSlotIndex].IsEmpty)
            return false;

        // 卸下到背包
        InventoryManager.Instance?.UnequipWeapon(equipSlotIndex);
        return true;
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

        // 获取药水配置
        var config = GameDataManager.Instance?.GetItemConfig(itemId) as PotionItemConfig;
        if (config == null)
        {
            return false;
        }

        // 即时效果
        switch (config.instantEffectType)
        {
            case PotionInstantEffectType.Heal:
                float healAmount = config.instantEffectValue;
                var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
                if (player != null)
                {
                    float currentHealth = player.RuntimeData.Health;
                    float maxHealth = player.RuntimeData.maxHealth;
                    float newHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
                    player.RuntimeData.Health = newHealth;
                    SessionManager.Instance?.SetPlayerHealth(newHealth);
                    // 通知 HeartSystem 更新显示（使用 Player._playerData 引用）
                    EventCenter.Instance.Publish(GameEventID.UpdateHeartDisplay, player.RuntimeData);
                }
                break;
        }

        // 限时效果 - 通过BuffManager
        if (config.timedEffectType == PotionTimedEffectType.Shield)
        {
            BuffManager.Instance.AddBuff(
                BuffType.Shield,
                config.timedEffectDuration,
                config.timedEffectValue,
                $"potion_{itemId}"
            );
        }
        else if (config.timedEffectType == PotionTimedEffectType.SpeedBoost)
        {
            // SpeedBoost 的 value 是倍率：配置值 10 表示 +10%，需要转换为 1.1
            float speedMultiplier = 1f + config.timedEffectValue / 100f;
            BuffManager.Instance.AddBuff(
                BuffType.SpeedBoost,
                config.timedEffectDuration,
                speedMultiplier,
                $"potion_{itemId}"
            );
        }

        // 移除药水
        InventoryManager.Instance?.RemoveItem(ItemType.Potion, itemId, 1);
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

        if (slot == null || slot.IsEmpty)
        {
            return;
        }

        _splitContextSlot = slot;
        _splitQuantity = (slot.Quantity + 1) / 2; // 默认一半


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
        }
        else
        {
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

        if (_splitContextSlot == null || _splitQuantity <= 0)
        {
            HideSplitDialog();
            return;
        }

        int originalQuantity = _splitContextSlot.Quantity;
        int itemId = _splitContextSlot.ItemId;
        ItemType itemType = _splitContextSlot.ItemType;
        int slotIndex = _splitContextSlot.CurrentIndex;

        if (_splitQuantity >= originalQuantity)
        {
            HideSplitDialog();
            return;
        }


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

    #region 日志

    private void Log(string msg) => Debug.Log($"[InventoryPanel] {msg}");
    private void LogError(string msg) => Debug.LogError($"[InventoryPanel] {msg}");

    #endregion
}
