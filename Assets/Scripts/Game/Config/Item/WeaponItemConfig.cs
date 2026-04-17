using UnityEngine;

/// <summary>
/// 武器 item 配置
/// </summary>
[CreateAssetMenu(fileName = "Item_Weapon", menuName = "KikoSurge/物品/武器")]
public class WeaponItemConfig : ItemConfig
{
    [Header("武器掉落配置")]
    [Tooltip("引用的武器配置")]
    public GunConfig gunConfig;

    private void OnValidate()
    {
        ItemType = ItemType.Weapon;
    }
}
