using UnityEngine;

/// <summary>
/// 道具类掉落物品定义（预留扩展）
/// </summary>
[CreateAssetMenu(fileName = "PropLootItemDef_SO", menuName = "KikoSurge/掉落物品/道具")]
public class PropLootItemDef_SO : LootItemDefBase
{
    [Header("道具掉落配置")]
    [Tooltip("道具ID（未来用于查询道具配置）")]
    public int PropId;

    [Tooltip("道具效果描述")]
    [TextArea]
    public string Description;

    private void OnValidate()
    {
        ItemType = LootItemType.Prop;
    }
}
