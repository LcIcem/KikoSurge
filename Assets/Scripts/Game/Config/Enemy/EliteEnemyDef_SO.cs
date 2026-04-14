using UnityEngine;

[CreateAssetMenu(fileName = "SO_Enemy_Elite", menuName = "KikoSurge/敌人/Elite")]
public class EliteEnemyDef_SO : EnemyDefBase
{
    [Header("Elite 特有属性")]
    public float SpecialAttackCooldown = 2f;
}
