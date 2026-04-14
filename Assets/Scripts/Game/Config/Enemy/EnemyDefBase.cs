using UnityEngine;

/// <summary>
/// 敌人配置基类（抽象）
/// </summary>
public abstract class EnemyDefBase : ScriptableObject
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
    public float DetectRange;
    public float AttackRange;
    public float LoseRange;

    [Header("预设体")]
    [Tooltip("敌人预设体的 Addressables 地址")]
    public string PrefabAddress;
}
