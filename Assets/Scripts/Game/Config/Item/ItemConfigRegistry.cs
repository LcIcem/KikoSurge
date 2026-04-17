using UnityEngine;

/// <summary>
/// 所有物品定义的集中配置（用于 Addressables 加载）
/// </summary>
[CreateAssetMenu(fileName = "Item_Config_Registry", menuName = "KikoSurge/物品/总配置")]
public class ItemConfigRegistry : ScriptableObject
{
    [Tooltip("所有物品定义")]
    public ItemConfig[] itemConfigs;
}
