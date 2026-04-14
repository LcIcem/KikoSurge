using UnityEngine;

[CreateAssetMenu(fileName = "SO_Enemy_Grunt", menuName = "KikoSurge/敌人/Grunt")]
public class GruntEnemyDef_SO : EnemyDefBase
{
    [Header("Grunt 特有属性")]
    public float PatrolSpeed = 1f;
}
