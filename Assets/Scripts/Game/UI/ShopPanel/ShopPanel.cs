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

    private List<ShopItemCell> _itemCells = new();
    private Coroutine _hintCoroutine;

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
    }

    public override void Hide()
    {
        base.Hide();
        ClearItemCells();
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

    private void Log(string msg) => Debug.Log($"[ShopPanel] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[ShopPanel] {msg}");
    private void LogError(string msg) => Debug.LogError($"[ShopPanel] {msg}");
}
