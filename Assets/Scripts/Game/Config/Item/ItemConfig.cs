using UnityEngine;

/// <summary>
/// 物品配置基类（抽象）
/// 所有可掉落/可使用物品的共同属性定义
/// </summary>
[CreateAssetMenu(fileName = "Item_XXX", menuName = "KikoSurge/物品/物品定义")]
public class ItemConfig : ScriptableObject
{
    [Header("标识")]
    [Tooltip("全局唯一ID，所有物品共享此ID系统")]
    public int Id;

    [Tooltip("物品名称")]
    public string Name;

    [Tooltip("物品类型")]
    public ItemType Type;

    [Tooltip("最大堆叠数")]
    public int MaxStack = 1;

    [Tooltip("卖价")]
    public int Value;

    [Tooltip("图标")]
    public Sprite Icon;

    [Tooltip("弹药图标（HUD显示用，留空则使用子弹预制体的Icon）")]
    public Sprite ammoIcon;

    [Tooltip("描述")]
    public string Description;

    [Tooltip("拾取音效")]
    public AudioClip PickupSFX;
}
