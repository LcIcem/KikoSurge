using System.Collections;
using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.Pool;
using UnityEngine;

/// <summary>
/// 敌人基类：处理受伤、死亡、寻路、攻击。
/// 实现 IPoolable 支持对象池。
/// </summary>
public class EnemyBase : MonoBehaviour, IPoolable
{
    // 属性
    public float HP { get; private set; }
    public float MaxHP { get; protected set; }
    public float MoveSpeed { get; protected set; }
    public float Attack { get; protected set; }
    public float DetectRange { get; protected set; }
    public float AttackRange { get; protected set; }
    public float LoseRange { get; protected set; }
    public bool IsAlive => HP > 0f;

    // 组件
    protected Rigidbody2D _rigidbody;
    protected SpriteRenderer _sprite;
    protected Animator _animator;
    protected EnemyFSM _fsm;
    protected EnemyPathfinder _pathfinder;

    // 公开访问器（供外部状态机使用）
    public EnemyPathfinder Pathfinder => _pathfinder;

    // 目标引用
    [HideInInspector]
    public Transform _player;   // 玩家位置程序化生成的，无法在Inspector拖拽

    // 工具属性
    // 与玩家的距离
    public float DistanceToPlayer
    {
        get
        {
            if (_player == null) return float.MaxValue;
            return Vector3.Distance(transform.position, _player.position);
        }
    }

    // 朝向玩家的翻转
    public void FacePlayer()
    {
        if (_player == null) return;
        _sprite.flipX = _player.position.x < transform.position.x;
    }

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _pathfinder = GetComponent<EnemyPathfinder>();
        _fsm = new EnemyFSM(this, _animator);
    }

    protected virtual void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _fsm.Start();
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;
        _fsm.Update();
    }

    // ========== 初始化（从对象池取出时调用） ==========
    public void Init(EnemyConfig config)
    {
        MaxHP = config.MaxHP;
        HP = MaxHP;
        MoveSpeed = config.MoveSpeed;
        Attack = config.Attack;
        DetectRange = config.DetectRange;
        AttackRange = config.AttackRange;
        LoseRange = config.LoseRange;
    }

    // IPoolable
    public void OnSpawn()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _fsm.Start();
    }

    public void OnDespawn()
    {
        _fsm?.Stop();
    }

    // 受伤处理
    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        HP -= damage;

        EventCenter.Instance.Publish(EventID.Combat_EnemyDamaged,
            new EnemyDamagedParams { enemy = this, damage = damage, currentHP = HP });

        if (HP <= 0f)
        {
            HP = 0f;
            Die();
        }
    }

    // 死亡处理
    protected virtual void Die()
    {
        _fsm.SetTrigger("Dead");

        EventCenter.Instance.Publish(EventID.Combat_EnemyKilled,
            new EnemyKilledParams { enemy = this, position = transform.position });

        // 延迟回收至对象池（等待死亡动画播放）
        StartCoroutine(DelayRelease());
    }

    private IEnumerator DelayRelease()
    {
        yield return new WaitForSeconds(1f);
        ManagerHub.Pool.Release(gameObject);
    }

    // 开始寻路到目标
    public void ChaseTarget()
    {
        Debug.Log("开始Chase");
        if (_pathfinder != null)
        {
            Debug.Log("开始使用Pathfinder");
            _pathfinder.StartMoveTo(_player.transform);
        }
        else
        {
            // 如果没有_pathfinder 则使用固定方向移动
            Vector2 dir = (_player.position - transform.position).normalized;
            _rigidbody.MovePosition(_rigidbody.position + dir * MoveSpeed * Time.deltaTime);
        }
    }

    // 停止寻路到目标
    public void StopChaseTarget()
    {
        if (_pathfinder != null)
        {
            _pathfinder.StopMove();
        }
        else
        {
            // 如果没有_pathfinder 则将速度设为0
            _rigidbody.linearVelocity = Vector2.zero;
        }
    }

    // 攻击
    public virtual void AttackTarget()
    {
        if (_player == null) return;

        EventCenter.Instance.Publish(EventID.Combat_EnemyAttack,
            new EnemyAttackParams { enemy = this, target = _player, damage = Attack });
    }

    // 调试
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 检测范围 - 绿色
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, DetectRange);

        // 攻击范围 - 红色
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // 丢失范围 - 黄色
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, LoseRange);
    }
}