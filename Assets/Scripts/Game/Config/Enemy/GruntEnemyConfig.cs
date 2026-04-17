using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_Grunt", menuName = "KikoSurge/敌人/Grunt")]
public class GruntEnemyConfig : EnemyConfig
{
    [Header("Grunt 特有属性")]
    public float PatrolSpeed = 1f;
}
