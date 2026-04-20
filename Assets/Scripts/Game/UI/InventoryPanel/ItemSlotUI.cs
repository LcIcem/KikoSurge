using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using LcIcemFramework;

/// <summary>
/// 背包物品格子（支持对象池）
/// <para>显示物品图标和堆叠数量，响应点击事件</para>
/// </summary>
public class ItemSlotUI : MonoBehaviour, IPoolable, IPointerClickHandler
{
    #region 序列化字段

    [SerializeField] private Image _imgIcon;
    [SerializeField] private TMP_Text _txtCount;
    [SerializeField] private Image _imgHighlight;

    #endregion

    #region 事件

    /// <summary>
    /// 格子点击事件回调（slotUI）
    /// </summary>
    public event Action<ItemSlotUI> OnSlotClicked;

    /// <summary>
    /// 格子右键点击事件回调（slotUI, screenPosition）
    /// </summary>
    public event Action<ItemSlotUI, Vector2> OnSlotRightClicked;

    #endregion

    #region 字段

    private int _itemId;
    private ItemType _itemType;
    private int _quantity;
    private int _currentIndex = -1;
    private bool _isPlaceholder = false;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化格子数据
    /// </summary>
    public void Initialize(int itemId, int quantity, ItemType type, int index = -1)
    {
        _itemId = itemId;
        _quantity = quantity;
        _itemType = type;
        _currentIndex = index;

        // 空格子：清除图标和数量
        if (itemId == 0)
        {
            if (_imgIcon != null)
            {
                _imgIcon.sprite = null;
                _imgIcon.enabled = false;
            }
            if (_txtCount != null)
                _txtCount.text = "";
            return;
        }

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
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(0, 0);
            rt.anchoredPosition = Vector2.zero;
        }

        if (_imgIcon != null)
            _imgIcon.enabled = true;

        if (_imgHighlight != null)
            _imgHighlight.enabled = false;

        // 重置状态
        _isPlaceholder = false;
    }

    public void OnDespawn()
    {
        if (_imgIcon != null)
        {
            _imgIcon.sprite = null;
            _imgIcon.enabled = false;
        }

        if (_txtCount != null)
            _txtCount.text = "";

        if (_imgHighlight != null)
            _imgHighlight.enabled = false;

        // 重置状态
        _isPlaceholder = false;
    }

    #endregion

    #region 点击处理

    public void OnPointerClick(PointerEventData eventData)
    {
        // 右键点击
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnSlotRightClicked?.Invoke(this, eventData.position);
            return;
        }

        // 左键点击
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnSlotClicked?.Invoke(this);
        }
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

    /// <summary>
    /// 设置为占位符（空位提示）
    /// </summary>
    public void SetAsPlaceholder(bool isPlaceholder)
    {
        _isPlaceholder = isPlaceholder;
    }

    #endregion

    #region 属性

    public int ItemId => _itemId;
    public ItemType ItemType => _itemType;
    public int Quantity => _quantity;
    public int CurrentIndex
    {
        get => _currentIndex;
        set => _currentIndex = value;
    }

    public RectTransform RectTransform => transform as RectTransform;
    public bool IsPlaceholder => _isPlaceholder;
    public bool IsEmpty => _itemId == 0 || _quantity <= 0;

    #endregion
}
