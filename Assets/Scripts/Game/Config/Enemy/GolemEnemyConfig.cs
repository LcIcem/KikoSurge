using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_Golem", menuName = "KikoSurge/敌人/Golem")]
public class GolemEnemyConfig : EliteEnemyConfig
{
    [Header("Golem 子弹攻击")]
    public BulletConfig bulletConfig;
    public float bulletSpread = 5f;
}
