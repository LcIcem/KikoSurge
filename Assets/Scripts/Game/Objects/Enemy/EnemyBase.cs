using System.Collections;
using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.Pool;
using Pathfinding;
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
    protected AIDestinationSetter _aiPath;

    // 目标引用
    public Transform _player;

    // 工具属性
    // 于玩家的距离
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
        _sprite = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _aiPath = GetComponent<AIDestinationSetter>();
    }

    protected virtual void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _fsm = new EnemyFSM(this, _animator);
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;
        _fsm.Update();
    }

    // ========== 初始化（从对象池取出时调用） ==========
    public void Init(string prefabName, EnemyConfig config)
    {
        MaxHP = config.MaxHP;
        HP = MaxHP;
        MoveSpeed = config.MoveSpeed;
        Attack = config.Attack;
        DetectRange = config.DetectRange;
        AttackRange = config.AttackRange;
        LoseRange = config.LoseRange;

        gameObject.SetActive(true);
    }

    // IPoolable
    public void OnSpawn()
    {
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

    // 寻路
    public void MoveTo(Transform target)
    {
        if (_aiPath != null)
        {
            _aiPath.target = target;
        }
        else
        {
            // 备用：手动移动
            Vector2 dir = ((Vector3)target.position - transform.position).normalized;
            _rigidbody.MovePosition(_rigidbody.position + dir * MoveSpeed * Time.deltaTime);
        }
    }

    // 攻击
    public virtual void AttackTarget()
    {
        if (_player == null) return;

        EventCenter.Instance.Publish(EventID.Combat_EnemyAttack,
            new EnemyAttackParams { enemy = this, target = _player, damage = Attack });
    }
}