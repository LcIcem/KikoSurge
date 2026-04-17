using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_Boss", menuName = "KikoSurge/敌人/Boss")]
public class BossEnemyConfig : EnemyConfig
{
    [Header("Boss 特有属性")]
    public int PhaseCount = 3;
    public float[] PhaseThreshold;
}
