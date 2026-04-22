using System.Collections;
using System.Collections.Generic;
using Game.Manager;
using LcIcemFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店面板
/// </summary>
public class ShopPanel : BasePanel
{
    [Header("商店面板控件")]
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _itemCellPrefab;
    [SerializeField] private TMP_Text _hintText;
    [SerializeField] private float _hintDuration = 1.5f;

    [Header("货币显示")]
    [SerializeField] private RectTransform _contentCurrency;
    [SerializeField] private ScrollRect _scrollRectCurrency;
    [SerializeField] private GameObject _slotPrefab;
    [SerializeField] private GameObject _emptySlotPrefab;

    private List<ShopItemCell> _itemCells = new();
    private Coroutine _hintCoroutine;
    private readonly List<ItemSlotUI> _activeCurrencySlots = new();

    /// <summary>
    /// 商店关闭事件
    /// </summary>
    public event System.Action OnShopClosed;

    #region BasePanel Overrides

    public override void Show()
    {
        base.Show();
        // 隐藏提示文本
        if (_hintText != null)
            _hintText.alpha = 0f;
        RefreshShopItems();
        RefreshCurrencyContent();
    }

    public override void Hide()
    {
        base.Hide();
        ClearItemCells();
        ReleaseAllCurrencySlots();
    }

    public override bool CanBeClosedByClosePanel => true;

    public override void OnBeforeClose()
    {
        // 面板关闭时通知商人
        OnShopClosed?.Invoke();
    }

    #endregion

    #region 按钮事件

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case "btn_close":
                CloseShop();
                break;
        }
    }

    #endregion

    #region 商品显示

    /// <summary>
    /// 刷新商店商品列表
    /// </summary>
    private void RefreshShopItems()
    {
        ClearItemCells();

        var items = ShopManager.Instance.CurrentItems;
        if (items == null || items.Count == 0)
        {
            LogWarning("商店商品列表为空");
            return;
        }

        // 实例化商品单元格
        for (int i = 0; i < items.Count; i++)
        {
            CreateItemCell(items[i], i);
        }
    }

    /// <summary>
    /// 创建商品单元格
    /// </summary>
    private void CreateItemCell(ShopDisplayItem shopItem, int index)
    {
        if (_itemCellPrefab == null || _content == null)
        {
            LogError("商品单元格预制体或 Content 为空");
            return;
        }

        var cellObj = UnityEngine.Object.Instantiate(_itemCellPrefab, _content);
        var cell = cellObj.GetComponent<ShopItemCell>();
        if (cell == null)
        {
            LogError("商品单元格预制体缺少 ShopItemCell 组件");
            return;
        }

        cell.Initialize(
            index,
            shopItem.Entry.ItemConfig,
            shopItem.Quantity,
            shopItem.Entry.FinalPrice,
            OnBuyOneClicked,
            OnBuyAllClicked
        );

        _itemCells.Add(cell);
    }

    /// <summary>
    /// 清空商品单元格
    /// </summary>
    private void ClearItemCells()
    {
        foreach (var cell in _itemCells)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
        }
        _itemCells.Clear();
    }

    #endregion

    #region 购买逻辑

    /// <summary>
    /// 购买单个按钮点击回调
    /// </summary>
    private void OnBuyOneClicked(int itemIndex)
    {
        bool success = ShopManager.Instance.TryPurchaseSingle(
            itemIndex,
            onSuccess: () =>
            {
                Log("购买成功");
                RefreshShopItems();
                RefreshCurrencyContent();
            },
            onFail: () =>
            {
                Log("购买失败");
            }
        );

        if (!success)
        {
            ShowInsufficientFundsHint();
        }
    }

    /// <summary>
    /// 购买全部按钮点击回调
    /// </summary>
    private void OnBuyAllClicked(int itemIndex)
    {
        bool success = ShopManager.Instance.TryPurchaseAll(
            itemIndex,
            onSuccess: () =>
            {
                Log("购买成功");
                RefreshShopItems();
                RefreshCurrencyContent();
            },
            onFail: () =>
            {
                Log("购买失败");
            }
        );

        if (!success)
        {
            ShowInsufficientFundsHint();
        }
    }

    #endregion

    #region 提示文本

    /// <summary>
    /// 显示货币不足提示
    /// </summary>
    private void ShowInsufficientFundsHint()
    {
        if (_hintText == null) return;

        if (_hintCoroutine != null)
        {
            StopCoroutine(_hintCoroutine);
        }

        _hintCoroutine = StartCoroutine(ShowHintCoroutine("货币不足！"));
    }

    /// <summary>
    /// 显示提示文本协程（淡出效果）
    /// </summary>
    private IEnumerator ShowHintCoroutine(string message)
    {
        _hintText.text = message;
        _hintText.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < _hintDuration)
        {
            elapsed += Time.deltaTime;
            _hintText.alpha = 1f - (elapsed / _hintDuration);
            yield return null;
        }

        _hintText.alpha = 0f;
        _hintCoroutine = null;
    }

    #endregion

    #region 关闭

    /// <summary>
    /// 关闭商店
    /// </summary>
    private void CloseShop()
    {
        GameLifecycleManager.Instance.CloseCurrentPanel();
    }

    #endregion

    #region 货币显示

    /// <summary>
    /// 刷新货币内容（与背包货币背包逻辑一致）
    /// </summary>
    private void RefreshCurrencyContent()
    {
        if (_contentCurrency == null)
        {
            LogError("_contentCurrency is null! Please assign in Inspector.");
            return;
        }
        if (_emptySlotPrefab == null)
        {
            LogError("_emptySlotPrefab is null! Please assign in Inspector.");
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
            var slotObj = PoolManager.Instance.Get(_emptySlotPrefab, Vector3.zero, Quaternion.identity);
            var slot = slotObj.GetComponent<ItemSlotUI>();

            if (slot != null)
            {
                slot.Initialize(0, 0, ItemType.Currency, i);
                slot.transform.SetParent(_contentCurrency, false);
                slot.transform.SetSiblingIndex(i);
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

    private void Log(string msg) => Debug.Log($"[ShopPanel] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[ShopPanel] {msg}");
    private void LogError(string msg) => Debug.LogError($"[ShopPanel] {msg}");
}
