using UnityEngine;

/// <summary>
/// 宝箱配置 SO
/// </summary>
[CreateAssetMenu(fileName = "Chest", menuName = "KikoSurge/宝箱/宝箱配置")]
public class ChestConfig : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private string _chestName = "宝箱";
    public string ChestName => _chestName;

    [Header("掉落配置")]
    [SerializeField] private LootTableConfig _lootTable;
    public LootTableConfig LootTable => _lootTable;
}
