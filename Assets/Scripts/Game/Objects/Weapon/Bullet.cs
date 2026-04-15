using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using UnityEngine;
using Game.Event;
using LcIcemFramework.Managers.Pool;

/// <summary>
/// 子弹：处理碰撞、伤害、效果
/// 移动逻辑由 BulletModule 分发
/// </summary>
public class Bullet : MonoBehaviour, IPoolable
{
    [Header("HUD图标")]
    [Tooltip("HUD显示用的子弹图标")]
    public Sprite Icon;

    // 飞行参数
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
        Damage = config.damage;
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
            _rigidbody.isKinematic = false;
            _rigidbody.freezeRotation = true;
            _rigidbody.linearVelocity = Direction * Speed;
        }
        else
        {
            _rigidbody.gravityScale = 0f;
            _rigidbody.isKinematic = true;
            _rigidbody.freezeRotation = true;
        }

        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 碰撞检测
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage);

                EventCenter.Instance.Publish(GameEventID.Combat_BulletHit,
                    new BulletHitParams
                    {
                        bullet = this,
                        target = enemy.transform,
                        damage = Damage
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
        else if (other.CompareTag("Solid"))
        {
            ManagerHub.Pool.Release(gameObject);
        }
    }
}
