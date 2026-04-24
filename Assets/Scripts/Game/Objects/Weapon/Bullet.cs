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

    // 标记是否已造成过伤害（防止同一发子弹触发多次伤害事件）
    private bool _hasDealtDamage = false;

    // 武器ID（用于Buff来源追踪）
    private string _weaponId;

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
        _hasDealtDamage = false;
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
        Init(config, direction, damageParams, 0);
    }

    /// <summary>
    /// 初始化子弹（带伤害参数和穿透数量）
    /// </summary>
    public void Init(BulletConfig config, Vector3 direction, BulletDamageParams damageParams, int penetrateCount)
    {
        Init(config, direction, damageParams, penetrateCount, null);
    }

    /// <summary>
    /// 初始化子弹（带武器ID）
    /// </summary>
    public void Init(BulletConfig config, Vector3 direction, BulletDamageParams damageParams, int penetrateCount, string weaponId)
    {
        Damage = config.baseDamage;
        this.damageParams = damageParams;
        Direction = direction.normalized;
        Speed = config.bulletSpeed;
        BulletType = config.bulletType;
        HitEffect = config.hitEffect;
        EffectValue = config.effectValue;
        PierceCount = penetrateCount;
        MaxDistance = config.maxDistance;
        _homingRange = config.homingRange;
        _homingStrength = config.homingStrength;
        _weaponId = weaponId ?? "unknown";
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

    /// <summary>
    /// 设置子弹伤害（用于覆盖默认伤害值）
    /// </summary>
    public void SetDamage(int damage)
    {
        Damage = damage;
    }

    // 碰撞检测
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 防止同一发子弹触发多次伤害事件
        if (_hasDealtDamage) return;

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
                    Debug.Log($"[Bullet] 伤害计算完成: finalDamage={result.finalDamage}, isCrit={result.isCrit}, critMultiplier={result.critMultiplier}, rawDamage={result.rawDamage}");
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
                    Debug.Log($"[Bullet] 降级处理: damageParams=null, 使用 Damage={Damage}");
                }

                enemy.TakeDamage(result.finalDamage);
                _hasDealtDamage = true;

                // 如果穿透，不重置标记但继续飞行
                if (PierceCount > 0)
                {
                    PierceCount--;
                    // 穿透后允许再次造成伤害
                    _hasDealtDamage = false;
                }
                else
                {
                    ManagerHub.Pool.Release(gameObject);
                }

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

            ApplyHitEffect(other.gameObject, HitEffect, EffectValue);
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

    /// <summary>
    /// 应用命中效果
    /// </summary>
    private void ApplyHitEffect(GameObject target, HitEffect effect, float value)
    {
        if (effect == HitEffect.None || target == null) return;

        string targetId = target.GetInstanceID().ToString();

        switch (effect)
        {
            case HitEffect.Freeze:
                BuffManager.Instance.AddBuff(
                    BuffType.Freeze, duration: 3f, value: value,
                    sourceId: $"freeze_{_weaponId}",
                    tickInterval: 0f, maxStacks: 1, targetId: targetId
                );
                break;

            case HitEffect.Burn:
                BuffManager.Instance.AddBuff(
                    BuffType.Burn, duration: 5f, value: value,
                    sourceId: $"burn_{_weaponId}",
                    tickInterval: 1f, maxStacks: 3, targetId: targetId
                );
                break;

            case HitEffect.Explode:
                // 爆炸效果：直接造成伤害
                var enemy = target.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(value);
                }
                // AOE伤害
                float radius = 3f;
                var colliders = Physics2D.OverlapCircleAll(target.transform.position, radius);
                foreach (var col in colliders)
                {
                    if (col.gameObject == target) continue;
                    var other = col.GetComponent<EnemyBase>();
                    if (other != null)
                    {
                        other.TakeDamage(value * 0.8f);
                    }
                }
                break;
        }
    }
}
