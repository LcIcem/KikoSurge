using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有敌人配置的统一SO
/// </summary>
[CreateAssetMenu(fileName = "AllEnemyDefs_SO", menuName = "KikoSurge/敌人/所有敌人集中配置")]
public class AllEnemyDefs_SO : ScriptableObject
{
    public List<EnemyDefBase> enemyDefs;
}
