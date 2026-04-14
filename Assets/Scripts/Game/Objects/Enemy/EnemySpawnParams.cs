using UnityEngine;

/// <summary>
/// 敌人生成参数
/// </summary>
public class EnemySpawnParams
{
    public EnemyType Type { get; set; }
    public string PrefabName { get; set; }
    public Vector3 Position { get; set; }
    public EnemyDefBase Config { get; set; }
}