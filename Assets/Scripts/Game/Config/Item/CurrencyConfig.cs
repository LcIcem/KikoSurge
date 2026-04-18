using UnityEngine;

/// <summary>
/// 货币配置
/// </summary>
[CreateAssetMenu(fileName = "Currency_Gold", menuName = "KikoSurge/物品/货币")]
public class CurrencyConfig : ItemConfig
{
    [Header("货币配置")]
    [Tooltip("1枚货币的价值")]
    public int coinValue = 1;

    private void OnValidate()
    {
        Type = ItemType.Currency;
        MaxStack = 9999;
    }
}
