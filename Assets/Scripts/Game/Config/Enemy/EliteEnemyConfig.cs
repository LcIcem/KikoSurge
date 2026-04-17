using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_Elite", menuName = "KikoSurge/敌人/Elite")]
public class EliteEnemyConfig : EnemyConfig
{
    [Header("Elite 特有属性")]
    public float SpecialAttackCooldown = 2f;
}
