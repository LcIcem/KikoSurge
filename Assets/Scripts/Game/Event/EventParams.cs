using UnityEngine;

/// <summary>
/// 玩家受伤事件参数
/// </summary>
public class DamageParams
{
    public float damage;
    public Vector2 from;
}

/// <summary>
/// 敌人受伤事件参数
/// </summary>
public class EnemyDamagedParams
{
    public EnemyBase enemy;
    public float damage;
    public float currentHP;
}

/// <summary>
/// 敌人死亡事件参数
/// </summary>
public class EnemyKilledParams
{
    public EnemyBase enemy;
    public Vector2 position;
}

/// <summary>
/// 敌人生成事件参数
/// </summary>
public class EnemySpawnedParams
{
    public EnemyBase enemy;
    public EnemyType type;
}

/// <summary>
/// 敌人攻击事件参数
/// </summary>
public class EnemyAttackParams
{
    public EnemyBase enemy;
    public Transform target;
    public float damage;
}

/// <summary>
/// 波次开始事件参数
/// </summary>
public class WaveStartParams
{
    public string behaviorName;      // 行为名称
    public int currentWave;          // 当前波次
    public int totalWaves;           // 总波次数
    public int enemiesInWave;        // 当前波敌人数
}

/// <summary>
/// 波次完成事件参数
/// </summary>
public class WaveCompleteParams
{
    public int waveNum;
}

/// <summary>
/// 波次清理完成事件参数
/// </summary>
public class WaveClearedParams
{
    public int waveNum;
}

/// <summary>
/// 波次更新事件参数（异步模式敌人死亡时）
/// </summary>
public class WaveUpdateParams
{
    public string behaviorName;
    public int currentWave;
    public int totalWaves;
    public int remainingEnemies;
}

/// <summary>
/// 子弹生成事件参数
/// </summary>
public class BulletSpawnParams
{
    public Bullet bullet;
    public WeaponType weaponType;
}

/// <summary>
/// 子弹命中事件参数
/// </summary>
public class BulletHitParams
{
    public Bullet bullet;
    public Transform target;
    public float damage;
}

/// <summary>
/// 换弹进度事件参数
/// </summary>
public class ReloadProgressParams
{
    public WeaponBase weapon;
    public float progress;  // 0~1
}
