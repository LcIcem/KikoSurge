using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 遗物配置
/// </summary>
[CreateAssetMenu(fileName = "Item_Relic", menuName = "KikoSurge/物品/遗物")]
public class RelicConfig : ItemConfig
{
    [Header("属性加成")]
    public List<ModifierData> modifiers = new();

    [Header("遗物效果")]
    public List<RelicEffect> effects = new();

    [Header("护甲属性（可选）")]
    public float baseDefense;
    public float damageReduction;
}
