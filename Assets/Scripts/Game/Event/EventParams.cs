using ProcGen.Core;
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
/// 敌人伤害类型（区分碰撞和攻击）
/// </summary>
public enum EnemyDamageType
{
    Attack,    // 子弹/范围攻击
    Collision  // 接触伤害
}

/// <summary>
/// 敌人伤害玩家参数（碰撞/子弹/范围检测，统一类型）
/// </summary>
public class EnemyHitPlayerParams
{
    public EnemyBase enemy;
    public Transform target;
    public float damage;
    public EnemyDamageType damageType;
}

/// <summary>
/// 波次开始事件参数
/// </summary>
public class WaveStartParams
{
    public string behaviourName;      // 行为名称
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
    public string behaviourName;
    public int currentWave;
    public int totalWaves;
    public int remainingEnemies;
}

/// <summary>
/// 子弹命中事件参数
/// </summary>
public class BulletHitParams
{
    public Bullet bullet;
    public Transform target;
    public float damage;
    public bool isCrit;
}

/// <summary>
/// 暴击事件参数
/// </summary>
public class CriticalHitParams
{
    public Transform target;
    public Transform attacker;
    public float damage;
    public float critMultiplier;
    public Vector3 worldPosition;
}

/// <summary>
/// 伤害数字显示事件参数
/// </summary>
public class DamageNumberParams
{
    public Transform target;
    public float damage;
    public bool isCrit;
    public Vector3 worldPosition;
}

/// <summary>
/// 换弹进度事件参数
/// </summary>
public class ReloadProgressParams
{
    public WeaponBase weapon;
    public float progress;  // 0~1
}

/// <summary>
/// 进入房间事件参数
/// </summary>
public class RoomEnterParams
{
    public int roomId;
    public RoomType roomType;
}

/// <summary>
/// 进入走廊事件参数
/// </summary>
public class CorridorEnterParams
{
    public int corridorId;
}

/// <summary>
/// 死亡动画播放结束事件参数
/// </summary>
public class DeathAnimationEndParams
{
}

/// <summary>
/// 背包变化类型
/// </summary>
public enum InventoryChangeType
{
    Add,
    Remove,
    Clear,
    Move,
    Swap
}

/// <summary>
/// 背包物品变化事件参数
/// </summary>
public class InventoryChangeParams
{
    public ItemType itemType;
    public int itemId;
    public int quantity;
    public InventoryChangeType changeType;

    public InventoryChangeParams(ItemType type, int id, int qty, InventoryChangeType change)
    {
        itemType = type;
        itemId = id;
        quantity = qty;
        changeType = change;
    }
}
