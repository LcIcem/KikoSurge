using LcIcemFramework.Core;
using LcIcemFramework;
using UnityEngine;
using Game.Event;

/// <summary>
/// 子弹：处理碰撞、伤害、效果
/// 移动逻辑由 BulletModule 分发
/// </summary>
public class Bullet : MonoBehaviour, IPoolable
{
    [Header("HUD图标")]
    [Tooltip("HUD显示用的子弹图标")]
    public Sprite Icon;

    // 子弹归属标签（由发射者在Spawn时设置）
    private string _ownerTag = "Enemy";

    // 伤害参数
    public BulletDamageParams damageParams { get; private set; }

    // 降级用的基础伤害（当 damageParams 为 null 时使用）
    public int Damage { get; private set; }

    public Vector3 Direction { get; private set; }
    public float Speed { get; private set; }
    public float MaxDistance { get; private set; }
    public Vector3 SpawnPos { get; private set; }
    public int PierceCount { get; private set; }
    public BulletType BulletType { get; private set; }
    public HitEffect HitEffect { get; private set; }
    public float EffectValue { get; private set; }
    public Rigidbody2D Rigidbody => _rigidbody;

    // 追踪参数
    private Vector3 _currentDir;
    private float _homingRange;
    private float _homingStrength;
    public float HomingRange => _homingRange;
    public float HomingStrength => _homingStrength;

    public Vector3 CurrentDir
    {
        get => _currentDir;
        set => _currentDir = value.normalized;
    }

    // 物理
    private Rigidbody2D _rigidbody;
    private CircleCollider2D _collider;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
    }

    private void Update()
    {
        BulletModule.Move(this);
    }

    // IPoolable
    public void OnSpawn()
    {
        _collider.enabled = true;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
    }

    public void OnDespawn()
    {
        _collider.enabled = false;
    }

    /// <summary>
    /// 超过最大飞行距离
    /// </summary>
    public bool IsExceedMaxDistance =>
        Vector3.Distance(transform.position, SpawnPos) > MaxDistance;

    /// <summary>
    /// 初始化子弹（统一入口）
    /// </summary>
    public void Init(BulletConfig config, Vector3 direction)
    {
        Init(config, direction, null);
    }

    /// <summary>
    /// 初始化子弹（带伤害参数）
    /// </summary>
    public void Init(BulletConfig config, Vector3 direction, BulletDamageParams damageParams)
    {
        Damage = config.baseDamage;
        this.damageParams = damageParams;
        Direction = direction.normalized;
        Speed = config.bulletSpeed;
        BulletType = config.bulletType;
        HitEffect = config.hitEffect;
        EffectValue = config.effectValue;
        PierceCount = config.penetrateCount;
        MaxDistance = config.maxDistance;
        _homingRange = config.homingRange;
        _homingStrength = config.homingStrength;
        SpawnPos = transform.position;

        // 追踪子弹初始方向
        _currentDir = direction.normalized;

        // 抛物线子弹使用 Dynamic，velocity 驱动 + 重力
        // 其他子弹使用 Kinematic，MovePosition 驱动
        if (config.bulletType == BulletType.Parabola)
        {
            _rigidbody.gravityScale = 1f;
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.freezeRotation = true;
            _rigidbody.linearVelocity = Direction * Speed;
        }
        else
        {
            _rigidbody.gravityScale = 0f;
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.freezeRotation = true;
        }

        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 设置子弹归属标签（由发射者在Spawn时调用）
    /// </summary>
    public void SetOwnerTag(string tag)
    {
        _ownerTag = tag;
    }

    // 碰撞检测
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 过滤己方子弹：如果碰撞对象的 tag 与 ownerTag 相同，跳过
        if (other.CompareTag(_ownerTag)) return;

        if (other.CompareTag("Enemy") && _ownerTag == "Player")
        {
            // 己方子弹击中敌人
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                DamageResult result;

                // 如果有完整伤害参数，使用 DamageCalculator 计算
                if (damageParams != null)
                {
                    damageParams.targetDefense = enemy.EnemyConfig?.Defense ?? 0f;
                    result = DamageCalculator.CalculateEnemyDamage(damageParams, enemy.transform.position);
                }
                else
                {
                    // 降级处理：使用原始 Damage
                    result = new DamageResult
                    {
                        finalDamage = Damage,
                        isCrit = false,
                        critRate = 0f,
                        critMultiplier = 1f,
                        source = DamageSource.PlayerBullet,
                        rawDamage = Damage,
                        defenseReduction = 0f,
                        worldPosition = enemy.transform.position
                    };
                }

                enemy.TakeDamage(result.finalDamage);

                EventCenter.Instance.Publish(GameEventID.Combat_BulletHit,
                    new BulletHitParams
                    {
                        bullet = this,
                        target = enemy.transform,
                        damage = result.finalDamage,
                        isCrit = result.isCrit
                    });

                // 发布伤害数字显示事件（由接收方根据 isCrit 决定显示样式）
                EventCenter.Instance.Publish(GameEventID.Combat_ShowDamageNumber,
                    new DamageNumberParams
                    {
                        target = enemy.transform,
                        damage = result.finalDamage,
                        isCrit = result.isCrit,
                        worldPosition = enemy.transform.position
                    });
            }

            HitEffectModule.Apply(other.gameObject, HitEffect, EffectValue);

            if (PierceCount > 0)
            {
                PierceCount--;
            }
            else
            {
                ManagerHub.Pool.Release(gameObject);
            }
        }
        else if (other.CompareTag("Player") && _ownerTag == "Enemy")
        {
            // 敌方子弹击中玩家 → 发布命中事件
            EventCenter.Instance.Publish(GameEventID.Combat_EnemyHitPlayer,
                new EnemyHitPlayerParams { enemy = null, target = other.transform, damage = Damage, damageType = EnemyDamageType.Attack });
            ManagerHub.Pool.Release(gameObject);
        }
        else if (other.CompareTag("Solid"))
        {
            ManagerHub.Pool.Release(gameObject);
        }
    }
}
