using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有敌人配置的统一SO
/// </summary>
[CreateAssetMenu(fileName = "Enemy_Registry", menuName = "KikoSurge/敌人/总配置")]
public class EnemyConfigRegistry : ScriptableObject
{
    public List<EnemyConfig> enemyConfigs;
}
