using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LcIcemFramework;

/// <summary>
/// 背包物品格子（支持对象池）
/// <para>显示物品图标和堆叠数量，响应点击事件</para>
/// </summary>
public class ItemSlotUI : MonoBehaviour, IPoolable
{
    #region 序列化字段

    [SerializeField] private Image _imgIcon;
    [SerializeField] private TMP_Text _txtCount;
    [SerializeField] private Image _imgHighlight;

    #endregion

    #region 字段

    private int _itemId;
    private ItemType _itemType;
    private int _quantity;

    /// <summary>
    /// 格子点击事件回调（itemId, itemType）
    /// </summary>
    public event Action<int, ItemType> OnSlotClicked;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化格子数据
    /// </summary>
    public void Initialize(int itemId, int quantity, ItemType type)
    {
        _itemId = itemId;
        _quantity = quantity;
        _itemType = type;

        // 获取物品配置
        var config = GameDataManager.Instance?.GetItemConfig(itemId);

        // 设置图标（只有当 config 存在且有有效图标时才覆盖预设体默认图标）
        if (_imgIcon != null && config != null && config.Icon != null)
        {
            _imgIcon.sprite = config.Icon;
            _imgIcon.enabled = true;
        }

        // 设置数量文本
        if (_txtCount != null)
        {
            _txtCount.text = quantity > 1 ? quantity.ToString() : "";
        }
    }

    #endregion

    #region IPoolable 实现

    public void OnSpawn()
    {
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        gameObject.SetActive(false);
        _itemId = 0;
        _quantity = 0;
        _itemType = ItemType.Weapon;

        if (_imgIcon != null)
        {
            _imgIcon.sprite = null;
            _imgIcon.enabled = false;
        }

        if (_txtCount != null)
        {
            _txtCount.text = "";
        }

        if (_imgHighlight != null)
        {
            _imgHighlight.enabled = false;
        }
    }

    #endregion

    #region 点击处理

    /// <summary>
    /// 供面板 Button 组件调用的点击处理
    /// </summary>
    public void HandleClick()
    {
        OnSlotClicked?.Invoke(_itemId, _itemType);
    }

    /// <summary>
    /// 设置选中高亮状态
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (_imgHighlight != null)
        {
            _imgHighlight.enabled = highlight;
        }
    }

    #endregion

    #region 属性

    public int ItemId => _itemId;
    public ItemType ItemType => _itemType;
    public int Quantity => _quantity;

    #endregion
}
