using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Config.Shop
{
    /// <summary>
    /// 商店商品条目
    /// </summary>
    [Serializable]
    public class ShopItemEntry
    {
        [SerializeField] private ItemConfig _itemConfig;
        public ItemConfig ItemConfig => _itemConfig;

        [Range(0f, 1f)]
        [SerializeField] private float _priceRatio = 1f;
        /// <summary>
        /// 价格系数（0.5 = 半价）
        /// </summary>
        public float PriceRatio => _priceRatio;

        [SerializeField] private int _minQuantity = 1;
        public int MinQuantity => _minQuantity;

        [SerializeField] private int _maxQuantity = 1;
        public int MaxQuantity => _maxQuantity;

        /// <summary>
        /// 计算最终售价：ItemConfig.Value * priceRatio
        /// </summary>
        public int FinalPrice => _itemConfig != null
            ? Mathf.CeilToInt(_itemConfig.Value * _priceRatio)
            : 0;
    }

    /// <summary>
    /// 商店商品分类
    /// </summary>
    [Serializable]
    public class ShopCategory
    {
        [SerializeField] private ItemType _itemType;
        public ItemType ItemType => _itemType;

        [SerializeField] private List<ShopItemEntry> _availableItems = new();
        public List<ShopItemEntry> AvailableItems => _availableItems;

        [SerializeField] private int _maxDisplayCount = 3;
        public int MaxDisplayCount => _maxDisplayCount;
    }

    /// <summary>
    /// 商店配置 SO
    /// </summary>
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "KikoSurge/商店/商店配置")]
    public class ShopConfig : ScriptableObject
    {
        [SerializeField] private List<ShopCategory> _categories = new();
        public List<ShopCategory> Categories => _categories;
    }
}
