using UnityEngine;

[CreateAssetMenu(fileName = "SO_Enemy_Boss", menuName = "KikoSurge/敌人/Boss")]
public class BossEnemyDef_SO : EnemyDefBase
{
    [Header("Boss 特有属性")]
    public int PhaseCount = 3;
    public float[] PhaseThreshold;
}
