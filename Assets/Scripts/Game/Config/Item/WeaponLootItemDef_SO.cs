using UnityEngine;

/// <summary>
/// 武器类掉落物品定义
/// </summary>
[CreateAssetMenu(fileName = "WeaponLootItemDef_SO", menuName = "KikoSurge/掉落物品/武器")]
public class WeaponLootItemDef_SO : LootItemDefBase
{
    [Header("武器掉落配置")]
    [Tooltip("引用的武器配置")]
    public WeaponDefBase WeaponDef;

    private void OnValidate()
    {
        ItemType = LootItemType.Weapon;
    }
}
