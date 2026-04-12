using UnityEngine;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.Pool;

/// <summary>
/// 子弹逻辑，挂载到子弹预设体上。
/// 沿自身 X 轴正向飞行，生命周期由对象池管理。
/// </summary>
public class Bullet : MonoBehaviour, IPoolable
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifetime = 3f;
    [SerializeField] private int _damage = 10;

    private float _timer;   // 销毁计时器
    private Rigidbody2D _rb;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    #region IPoolable 实现
    public void OnSpawn()
    {
        _timer = _lifetime;
    }

    public void OnDespawn()
    {
        _timer = 0f;
    }

    #endregion

    void FixedUpdate()
    {
        _rb.linearVelocity = transform.right * _speed;
    }

    private void Update()
    {
        // 沿自身 X 轴正向飞行
        transform.position += transform.right * _speed * Time.deltaTime;

        // 计时，到期归还
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            ManagerHub.Pool.Release(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 如果碰到实体Tag的碰撞体，直接归还到对象池
        if (collision.gameObject.CompareTag("Solid"))
        {
            Debug.Log("销毁");
            ManagerHub.Pool.Release(gameObject);
        }
    }

    public int Damage => _damage;
}
