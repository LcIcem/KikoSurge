using System;
using System.Collections.Generic;
using System.Linq;
using Game.Config.Shop;
using LcIcemFramework.Core;
using ProcGen.Seed;
using UnityEngine;

namespace Game.Manager
{
    /// <summary>
    /// 商店商品运行时数据结构
    /// </summary>
    public class ShopDisplayItem
    {
        public ShopItemEntry Entry { get; }
        public int Quantity { get; private set; }

        public ShopDisplayItem(ShopItemEntry entry, int quantity)
        {
            Entry = entry;
            Quantity = quantity;
        }

        /// <summary>
        /// 减少数量
        /// </summary>
        public void DecrementQuantity(int amount = 1)
        {
            Quantity = Mathf.Max(0, Quantity - amount);
        }
    }

    /// <summary>
    /// 商人管理器
    /// </summary>
    public class ShopManager : SingletonMono<ShopManager>
    {
        private ShopConfig _currentConfig;
        private List<ShopDisplayItem> _currentItems = new();
        private int _goldCurrencyId = 1; // 默认金币货币ID

        /// <summary>
        /// 当前商店商品列表（只读）
        /// </summary>
        public IReadOnlyList<ShopDisplayItem> CurrentItems => _currentItems;

        protected override void Init()
        {
            // 初始化
        }

        /// <summary>
        /// 刷新商店商品（从配置随机选取）
        /// </summary>
        public void RefreshShopItems(ShopConfig config, GameRandom rng)
        {
            _currentConfig = config;
            _currentItems.Clear();

            if (config == null || rng == null)
            {
                LogWarning("ShopConfig 或 GameRandom 为空");
                return;
            }

            foreach (var category in config.Categories)
            {
                if (category.AvailableItems.Count == 0) continue;

                // 复制并洗牌
                var shuffled = category.AvailableItems.ToList();
                rng.Shuffle(shuffled);
                int count = Mathf.Min(category.MaxDisplayCount, shuffled.Count);

                for (int i = 0; i < count; i++)
                {
                    var entry = shuffled[i];
                    int quantity = rng.Range(entry.MinQuantity, entry.MaxQuantity + 1);
                    _currentItems.Add(new ShopDisplayItem(entry, quantity));
                }
            }

            Log($"商店商品刷新完成，共 {_currentItems.Count} 件商品");
        }

        /// <summary>
        /// 尝试购买单个商品
        /// </summary>
        /// <param name="shopItemIndex">商品索引</param>
        /// <param name="onSuccess">购买成功回调</param>
        /// <param name="onFail">购买失败回调</param>
        /// <returns>是否购买成功</returns>
        public bool TryPurchaseSingle(int shopItemIndex, Action onSuccess, Action onFail)
        {
            if (shopItemIndex < 0 || shopItemIndex >= _currentItems.Count)
            {
                LogError($"无效的商品索引: {shopItemIndex}");
                onFail?.Invoke();
                return false;
            }

            var item = _currentItems[shopItemIndex];
            int price = item.Entry.FinalPrice;

            // 检查货币是否足够
            int playerCurrency = GetTotalCurrencyValue();
            if (playerCurrency < price)
            {
                Log($"货币不足: 需要 {price}，拥有 {playerCurrency}");
                onFail?.Invoke();
                return false;
            }

            // 扣除货币
            if (!RemoveCurrency(price))
            {
                onFail?.Invoke();
                return false;
            }

            // 添加商品到背包（1个）
            var itemConfig = item.Entry.ItemConfig;
            if (itemConfig != null)
            {
                InventoryManager.Instance.AddItem(itemConfig.Type, itemConfig.Id, 1);
                Log($"购买成功: {itemConfig.Name} x1");
            }

            // 减少商品数量
            item.DecrementQuantity(1);

            // 如果数量为0，移除商品
            if (item.Quantity <= 0)
            {
                _currentItems.RemoveAt(shopItemIndex);
            }

            onSuccess?.Invoke();
            return true;
        }

        /// <summary>
        /// 尝试购买全部商品
        /// </summary>
        /// <param name="shopItemIndex">商品索引</param>
        /// <param name="onSuccess">购买成功回调</param>
        /// <param name="onFail">购买失败回调</param>
        /// <returns>是否购买成功</returns>
        public bool TryPurchaseAll(int shopItemIndex, Action onSuccess, Action onFail)
        {
            if (shopItemIndex < 0 || shopItemIndex >= _currentItems.Count)
            {
                LogError($"无效的商品索引: {shopItemIndex}");
                onFail?.Invoke();
                return false;
            }

            var item = _currentItems[shopItemIndex];
            int totalPrice = item.Entry.FinalPrice * item.Quantity;

            // 检查货币是否足够
            int playerCurrency = GetTotalCurrencyValue();
            if (playerCurrency < totalPrice)
            {
                Log($"货币不足: 需要 {totalPrice}，拥有 {playerCurrency}");
                onFail?.Invoke();
                return false;
            }

            // 扣除货币
            if (!RemoveCurrency(totalPrice))
            {
                onFail?.Invoke();
                return false;
            }

            // 添加商品到背包（全部数量）
            var itemConfig = item.Entry.ItemConfig;
            if (itemConfig != null)
            {
                InventoryManager.Instance.AddItem(itemConfig.Type, itemConfig.Id, item.Quantity);
                Log($"购买成功: {itemConfig.Name} x{item.Quantity}");
            }

            // 移除商品
            _currentItems.RemoveAt(shopItemIndex);

            onSuccess?.Invoke();
            return true;
        }

        /// <summary>
        /// 计算玩家货币总价值
        /// </summary>
        public int GetTotalCurrencyValue()
        {
            var slots = InventoryManager.Instance.GetInventory(ItemType.Currency);
            if (slots == null) return 0;

            int total = 0;
            foreach (var slot in slots)
            {
                if (slot.itemId <= 0 || slot.quantity <= 0) continue;

                // 通过 GameDataManager 获取货币配置
                var currencyConfig = GameDataManager.Instance.GetItemConfig(slot.itemId) as CurrencyConfig;
                int coinValue = currencyConfig?.coinValue ?? 1;
                total += coinValue * slot.quantity;
            }

            return total;
        }

        /// <summary>
        /// 扣除货币（优先使用低价值货币）
        /// </summary>
        /// <param name="amount">要扣除的金额</param>
        /// <returns>是否扣除成功</returns>
        private bool RemoveCurrency(int amount)
        {
            if (amount <= 0) return true;

            var slots = InventoryManager.Instance.GetInventory(ItemType.Currency);
            if (slots == null || slots.Count == 0) return false;

            // 按 coinValue 从小到大排序，优先消耗低价值货币
            slots.Sort((a, b) =>
            {
                var configA = GameDataManager.Instance.GetItemConfig(a.itemId) as CurrencyConfig;
                var configB = GameDataManager.Instance.GetItemConfig(b.itemId) as CurrencyConfig;
                int valueA = configA?.coinValue ?? int.MaxValue;
                int valueB = configB?.coinValue ?? int.MaxValue;
                return valueA.CompareTo(valueB);
            });

            int remaining = amount;

            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (slot.itemId <= 0 || slot.quantity <= 0) continue;

                var config = GameDataManager.Instance.GetItemConfig(slot.itemId) as CurrencyConfig;
                int coinValue = config?.coinValue ?? 1;
                int slotValue = coinValue * slot.quantity;

                if (slotValue <= remaining)
                {
                    // 清空此格
                    remaining -= slotValue;
                    InventoryManager.Instance.RemoveItem(ItemType.Currency, slot.itemId, slot.quantity);
                }
                else
                {
                    // 部分扣除：计算需要多少个货币
                    int needCoins = Mathf.CeilToInt((float)remaining / coinValue);
                    remaining = 0;
                    InventoryManager.Instance.RemoveItem(ItemType.Currency, slot.itemId, needCoins);
                }
            }

            return remaining <= 0;
        }

        private void Log(string msg) => Debug.Log($"[ShopManager] {msg}");
        private void LogWarning(string msg) => Debug.LogWarning($"[ShopManager] {msg}");
        private void LogError(string msg) => Debug.LogError($"[ShopManager] {msg}");
    }
}
