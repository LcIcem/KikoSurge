using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.Pool;
using UnityEngine;

/// <summary>
/// 子弹：处理飞行、命中检测、伤害。
/// </summary>
public class Bullet : MonoBehaviour, IPoolable
{
    // 属性
    private float _damage;
    private Vector3 _direction;
    private float _speed;
    private float _maxDistance;
    private Vector3 _spawnPos;
    private int _pierceCount;

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
    }

    //初始化子弹
    public void Init(float damage, Vector3 direction, float speed, float maxDistance, int pierce = 0)
    {
        _damage = damage;
        _direction = direction.normalized;
        _speed = speed;
        _maxDistance = maxDistance;
        _spawnPos = transform.position;
        _pierceCount = pierce;

        // 朝飞行方向旋转
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 碰撞检测
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 造成伤害
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);

                EventCenter.Instance.Publish(EventID.Combat_BulletHit,
                    new BulletHitParams
                    {
                        bullet = this,
                        target = enemy.transform,
                        damage = _damage
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
}