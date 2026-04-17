using UnityEngine;

/// <summary>
/// 道具 item 配置
/// </summary>
[CreateAssetMenu(fileName = "Item_Prop", menuName = "KikoSurge/物品/道具")]
public class PropItemConfig : ItemConfig
{
    [Header("道具配置")]
    [Tooltip("道具ID")]
    public int PropId;

    [Tooltip("效果描述")]
    public string Description;

    private void OnValidate()
    {
        ItemType = ItemType.Prop;
    }
}
