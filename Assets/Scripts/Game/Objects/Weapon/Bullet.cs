using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.Pool;
using UnityEngine;
using Game.Event;

/// <summary>
/// 子弹：处理飞行、命中检测、伤害。
/// 子弹伤害和来源由发射者（IDamageSource）决定。
/// </summary>
public class Bullet : MonoBehaviour, IPoolable
{
    // 属性
    private Vector3 _direction;
    private float _speed;
    private float _maxDistance;
    private Vector3 _spawnPos;
    private int _pierceCount;
    private IDamageSource _damageSource;  // 伤害源（武器或敌人）

    [Header("HUD图标")]
    [Tooltip("HUD显示用的子弹图标")]
    public Sprite Icon;

    // 组件
    private Rigidbody2D _rigidbody;
    private CircleCollider2D _collider;
    private SpriteRenderer _sprite;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // 飞行
        _rigidbody.MovePosition(_rigidbody.position +
            (Vector2)_direction * _speed * Time.deltaTime);

        // 超距检测
        if (Vector3.Distance(transform.position, _spawnPos) > _maxDistance)
        {
            ManagerHub.Pool.Release(gameObject);
        }
    }

    // IPoolable
    public void OnSpawn()
    {
        _collider.enabled = true;
    }

    public void OnDespawn()
    {
        _collider.enabled = false;
        _damageSource = null;
    }

    /// <summary>
    /// 初始化子弹
    /// </summary>
    /// <param name="direction">飞行方向</param>
    /// <param name="speed">飞行速度</param>
    /// <param name="maxDistance">最大飞行距离</param>
    /// <param name="pierce">穿透次数</param>
    /// <param name="damageSource">伤害源（武器或敌人）</param>
    public void Init(Vector3 direction, float speed, float maxDistance, int pierce, IDamageSource damageSource)
    {
        _direction = direction.normalized;
        _speed = speed;
        _maxDistance = maxDistance;
        _spawnPos = transform.position;
        _pierceCount = pierce;
        _damageSource = damageSource;

        // 朝飞行方向旋转
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 获取当前子弹伤害值
    /// </summary>
    private float GetDamage() => _damageSource?.GetDamage() ?? 0f;

    /// <summary>
    /// 获取子弹发射者的GameObject
    /// </summary>
    private GameObject GetOwner() => _damageSource?.GetGameObject();

    // 碰撞检测
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 跳过发射者自身
        if (other.gameObject == GetOwner())
            return;

        float damage = GetDamage();

        // 根据发射者类型判断命中逻辑
        if (_damageSource is WeaponBase)
        {
            // 玩家武器：击中敌人
            if (other.CompareTag("Enemy"))
            {
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);

                    EventCenter.Instance.Publish(GameEventID.Combat_BulletHit,
                        new BulletHitParams
                        {
                            bullet = this,
                            target = enemy.transform,
                            damage = damage
                        });
                }

                // 穿透逻辑
                if (_pierceCount > 0)
                {
                    _pierceCount--;
                }
                else
                {
                    ManagerHub.Pool.Release(gameObject);
                }
            }
            else if (other.CompareTag("Solid"))
            {
                // 碰到墙壁，回收
                ManagerHub.Pool.Release(gameObject);
            }
        }
        else if (_damageSource is EnemyBase)
        {
            // 敌人武器：击中玩家
            if (other.CompareTag("Player"))
            {
                var player = other.GetComponent<Player>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                }
                ManagerHub.Pool.Release(gameObject);
            }
            else if (other.CompareTag("Solid"))
            {
                // 碰到墙壁，回收
                ManagerHub.Pool.Release(gameObject);
            }
        }
        else
        {
            // 未知来源，碰到任何东西都回收
            ManagerHub.Pool.Release(gameObject);
        }
    }
}
