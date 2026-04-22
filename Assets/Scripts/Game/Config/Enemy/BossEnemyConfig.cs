using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_Boss", menuName = "KikoSurge/敌人/Boss")]
public class BossEnemyConfig : EnemyConfig
{
    [Header("Boss 特有属性")]
    public int PhaseCount = 3;
    public float[] PhaseThreshold;

    [Header("冲刺攻击")]
    public float DashSpeed = 15f;
    public float DashDuration = 0.5f;
    public float DashCooldown = 2f;
    public float StunDuration = 0.8f;
}
