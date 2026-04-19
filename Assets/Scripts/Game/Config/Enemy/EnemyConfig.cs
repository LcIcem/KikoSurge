using UnityEngine;

/// <summary>
/// 敌人配置基类（抽象）
/// </summary>
public abstract class EnemyConfig : ScriptableObject
{
    [Header("标识")]
    [Tooltip("唯一ID，用于字典查找")]
    public int EnemyId;

    [Tooltip("敌人名称")]
    public string EnemyName;

    [Tooltip("敌人类型")]
    public EnemyType Type;

    [Header("战斗属性")]
    public float MaxHP;
    public float MoveSpeed;
    public float Attack;
    public float AttackInterval = 1f;   // 攻击间隔（秒）
    public float AttackDuration = 0.5f;  // 攻击动画持续时间（秒）
    public float AttackHitTime = 0.2f;   // 攻击生效时间点（秒，从动画开始计时）
    public float CollisionDamage = 10f;  // 碰撞伤害
    public float DetectRange;
    public float AttackRange;
    public float LoseRange;

    [Header("预设体")]
    [Tooltip("敌人预设体的 Addressables 地址")]
    public string PrefabAddress;

    [Header("掉落")]
    [Tooltip("掉落表（直接配置，无需 EnemyId 查找）")]
    [SerializeField] private LootTableConfig _lootTable;

    public LootTableConfig lootTable => _lootTable;
}
