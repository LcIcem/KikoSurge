using System.Collections;
using LcIcemFramework.Core;
using LcIcemFramework;
using UnityEngine;
using Game.Event;

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
    public float AttackInterval { get; protected set; }
    public float AttackDuration { get; protected set; }  // 攻击动画持续时间
    public float AttackHitTime { get; protected set; }   // 攻击生效时间点
    public float CollisionDamage { get; protected set; }
    public float DetectRange { get; protected set; }
    public float AttackRange { get; protected set; }
    public float LoseRange { get; protected set; }
    public bool IsAlive => HP > 0f;
    public EnemyType Type { get; protected set; }
    public int EnemyId { get; private set; }
    public int RoomId { get; set; } = -1;
    public EnemyConfig EnemyConfig => _config;

    // ========== 调试开关 ==========
    // EditorPrefs key 与 DebugMenu.cs 保持一致，Editor assembly 写入，runtime 读取
    private const string KEY_DEBUG_MODE = "KikoSurge.Debug.EnemyDebug";

    private static bool IsDebugMode
    {
        get
        {
            #if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(KEY_DEBUG_MODE, false);
            #else
            return false;
            #endif
        }
    }

    private const float DEBUG_SHOCKWAVE_DURATION = 0.3f;  // 冲击波特效持续时间

    // 攻击状态计时器
    private float _attackTimer = 0f;
    private float _cooldownTimer = 0f;
    private bool _attackHitTriggered = false;  // 攻击是否已触发
    private bool _shouldExitAfterAttack = false;  // 攻击动画完成后是否需要退出（冷却期间离开攻击范围时设置）
    private float _shockwaveTimer = 0f;  // 冲击波特效计时器

    // 保存的配置引用（用于池化后重置）
    private EnemyConfig _config;

    // 防止重复释放的标记
    private bool _isReleased;

    // 组件
    protected Rigidbody2D _rigidbody;
    protected SpriteRenderer _sprite;
    protected Animator _animator;
    protected EnemyFSM _fsm;
    protected EnemyPathfinder _pathfinder;

    // 颜色缓存
    private Color _tmpColor;

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
        _sprite = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _pathfinder = GetComponent<EnemyPathfinder>();
        _fsm = new EnemyFSM(this, _animator);
        // 缓存颜色
        _tmpColor = _sprite.color;
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;
        if (_player == null) {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // 冷却计时器独立递减（不管当前什么状态）
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        // 冲击波特效计时器递减
        if (_shockwaveTimer > 0f)
        {
            _shockwaveTimer -= Time.deltaTime;
        }

        _fsm.Update();
    }

    // 初始化（从对象池取出时调用）
    public void Init(EnemyConfig config)
    {
        _config = config;
        EnemyId = config.EnemyId;
        Type = config.Type;
        MaxHP = config.MaxHP;
        HP = MaxHP;
        MoveSpeed = config.MoveSpeed;
        Attack = config.Attack;
        AttackInterval = config.AttackInterval;
        AttackDuration = config.AttackDuration;
        AttackHitTime = config.AttackHitTime;
        CollisionDamage = config.CollisionDamage;
        DetectRange = config.DetectRange;
        AttackRange = config.AttackRange;
        LoseRange = config.LoseRange;
        _attackTimer = 0f;
        _cooldownTimer = 0f;
        _attackHitTriggered = false;
        _pathfinder.SetSpeed(MoveSpeed);
    }

    // IPoolable
    public void OnSpawn()
    {
        // 重置释放标记
        _isReleased = false;

        // 重新初始化（使用保存的配置）
        if (_config != null)
        {
            Init(_config);
        }

        // 重置旋转和速度（位置由 PoolManager.Get 设置，这里不覆盖）
        transform.localRotation = Quaternion.identity;
        _rigidbody.linearVelocity = Vector2.zero;

        // 重置死亡标记
        _fsm.SetAnimatorBool("dead", false);
        _fsm.CheckTrigger("dead"); // 重置 FSM 内部的 dead trigger

        // 重置碰撞器
        GetComponent<Collider2D>().enabled = true;

        // 重置 Player 引用
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        _shockwaveTimer = 0f;

        // 重启 FSM
        _fsm.Start();
    }

    public void OnDespawn()
    {
        // 停止死亡协程，防止复用时错误释放
        if (_deathCoroutine != null)
        {
            StopCoroutine(_deathCoroutine);
            _deathCoroutine = null;
        }
        _fsm?.Stop();
    }

    // 受伤处理
    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        // 显示受击颜色
        _sprite.color = new Color(_tmpColor.r, _tmpColor.g, _tmpColor.b, 125);
        // 0.5秒后恢复原来颜色
        TimerManager.Instance.AddTimeOutUnscaled(0.2f, () =>
        {
            _sprite.color = _tmpColor;
        });

        HP -= damage;

        EventCenter.Instance.Publish(GameEventID.Combat_EnemyDamaged,
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
        _fsm.SetAnimatorBool("dead", true);

        _rigidbody.linearVelocity = Vector2.zero;
        StopChaseTarget();

        // 禁用碰撞体，防止死亡后继续造成碰撞伤害
        var collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        EventCenter.Instance.Publish(GameEventID.Combat_EnemyKilled,
            new EnemyKilledParams { enemy = this, position = transform.position });

        // 延迟回收至对象池（等待死亡动画播放）
        _deathCoroutine = StartCoroutine(DelayRelease());
    }

    private Coroutine _deathCoroutine;

    private IEnumerator DelayRelease()
    {
        yield return new WaitForSeconds(1f);
        if (!_isReleased)
        {
            _isReleased = true;
            ManagerHub.Pool.Release(gameObject);
        }
    }

    // 立即释放到对象池（切换层时调用）
    public void ReleaseImmediately()
    {
        if (_isReleased)
            return;
        _isReleased = true;
        StopAllCoroutines();
        ManagerHub.Pool.Release(gameObject);
    }

    // 开始寻路到目标
    public void ChaseTarget()
    {
        if (_pathfinder != null)
        {
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

    /// <summary>
    /// 攻击阶段是否已完成（动画播放完毕）
    /// </summary>
    public bool IsAttackCompleted => _attackTimer >= AttackDuration;

    /// <summary>
    /// 标记攻击动画完成后需要退出（冷却期间离开攻击范围时由 EnemyAttackState 设置）
    /// </summary>
    public bool ShouldExitAfterAttack
    {
        get => _shouldExitAfterAttack;
        set => _shouldExitAfterAttack = value;
    }

    /// <summary>
    /// 获取攻击计时器
    /// </summary>
    public float GetAttackTimer() => _attackTimer;

    /// <summary>
    /// 更新攻击计时器
    /// </summary>
    public void UpdateAttackTimer(float deltaTime)
    {
        _attackTimer += deltaTime;
    }

    /// <summary>
    /// 获取冷却计时器
    /// </summary>
    public float GetCooldownTimer() => _cooldownTimer;

    /// <summary>
    /// 更新冷却计时器
    /// </summary>
    public void UpdateCooldownTimer(float deltaTime)
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= deltaTime;
        }
    }

    /// <summary>
    /// 是否已触发攻击伤害
    /// </summary>
    public bool IsAttackHitTriggered() => _attackHitTriggered;

    /// <summary>
    /// 重置攻击计时器（进入攻击状态时调用）
    /// </summary>
    public void ResetAttackState()
    {
        _attackTimer = 0f;
        _cooldownTimer = 0f;
        _attackHitTriggered = false;
        _shouldExitAfterAttack = false;
        _shockwaveTimer = 0f;
    }

    /// <summary>
    /// 重置攻击计时器但不重置CD（重新进入攻击状态时调用）
    /// </summary>
    public void ResetAttackStateNoCooldown()
    {
        _attackTimer = 0f;
        _attackHitTriggered = false;
        _shouldExitAfterAttack = false;
        _shockwaveTimer = 0f;
    }

    /// <summary>
    /// 触发攻击伤害（攻击生效时间点调用）
    /// </summary>
    public void TriggerAttackHit()
    {
        if (_attackHitTriggered) return;
        _attackHitTriggered = true;

        Debug.Log($"[EnemyAttack] {gameObject.name} 攻击命中! 攻击计时: {_attackTimer:F3}s / {AttackDuration}s, 生效时刻: {AttackHitTime}s");

        // 启动冲击波特效
        _shockwaveTimer = DEBUG_SHOCKWAVE_DURATION;

        // 调用攻击命中处理（可被子类覆盖实现自定义命中逻辑）
        HandleAttackHit();
    }

    /// <summary>
    /// 攻击命中检测（可被子类覆盖）
    /// <para>基类：检测玩家是否在攻击范围内，在范围内才视为命中</para>
    /// <returns>是否命中</returns>
    /// </summary>
    protected virtual bool CheckAttackHit()
    {
        if (_player == null) return false;
        return IsInAttackRange();
    }

    /// <summary>
    /// 攻击命中处理（通过 CheckAttackHit() 检测，命中则发布事件）
    /// </summary>
    private void HandleAttackHit()
    {
        if (!CheckAttackHit())
        {
            Debug.Log($"[EnemyAttack] {gameObject.name} 攻击已生效但检测未命中，攻击落空");
            return;
        }

        // 发布命中事件（与碰撞、子弹命中使用统一的 hit 事件）
        EventCenter.Instance.Publish(GameEventID.Combat_EnemyHitPlayer,
            new EnemyHitPlayerParams { enemy = this, target = _player, damage = Attack, damageType = EnemyDamageType.Attack });
    }

    /// <summary>
    /// 进入冷却阶段（攻击动画结束后调用）
    /// </summary>
    public void StartCooldown()
    {
        _cooldownTimer = AttackInterval;
    }

    /// <summary>
    /// 重置攻击冷却（退出攻击状态时调用）
    /// </summary>
    public void ResetAttackCooldown()
    {
        _attackTimer = 0f;
        _cooldownTimer = 0f;
        _attackHitTriggered = false;
    }

    /// <summary>
    /// 冷却是否结束
    /// </summary>
    public bool IsCooldownFinished() => _cooldownTimer <= 0f;

    /// <summary>
    /// 是否在攻击范围内
    /// </summary>
    public bool IsInAttackRange() => DistanceToPlayer < AttackRange;

    /// <summary>
    /// 发布碰撞伤害事件
    /// </summary>
    protected void PublishCollisionDamage(Transform target)
    {
        EventCenter.Instance.Publish(GameEventID.Combat_EnemyHitPlayer,
            new EnemyHitPlayerParams { enemy = this, target = target, damage = CollisionDamage, damageType = EnemyDamageType.Collision });
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Debug.Log($"[Enemy] OnCollisionEnter2D with Player! CollisionDamage={CollisionDamage}");
            PublishCollisionDamage(col.transform);
        }
    }

    // 敌人的触发器碰撞体（CircleCollider2D isTrigger=true）与玩家的触发器_hitCollider重叠时触发
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[Enemy] OnTriggerEnter2D with Player! CollisionDamage={CollisionDamage}");
            PublishCollisionDamage(other.transform);
        }
    }

    // ========== 调试特效 ==========
    /// <summary>
    /// 调试：获取攻击生效时的冲击波半径（0→1→0 循环）
    /// </summary>
    private float GetShockwaveRadius(float progress)
    {
        // 0→0.5: 从 0 扩大到 AttackRange，0.5→1: 从 AttackRange 缩小到 0
        float t = progress * 2f;
        if (t <= 1f)
            return Mathf.Lerp(0f, AttackRange, t);
        else
            return Mathf.Lerp(AttackRange, 0f, t - 1f);
    }

    // 调试
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!IsDebugMode) return;  // 调试开关关闭时不绘制

        // 检测范围 - 绿色
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, DetectRange);

        // 攻击范围 - 红色
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // 丢失范围 - 黄色
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, LoseRange);

        // 攻击状态可视化：头顶图标
        Vector3 headPos = transform.position + Vector3.up * 1.2f;

        if (_cooldownTimer > 0)
        {
            // 冷却中 - 黄色圆形
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(headPos, 0.2f);
        }
        else if (_attackTimer > 0)
        {
            // 攻击中 - 红色圆圈
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(headPos, 0.2f);
        }
        else
        {
            // 可攻击 - 绿色对勾
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(headPos, 0.15f);
        }

        // 冲击波特效：从敌人位置向外扩散的圆环（TriggerAttackHit 时启动）
        if (_shockwaveTimer > 0f)
        {
            float totalDuration = DEBUG_SHOCKWAVE_DURATION;
            float progress = 1f - (_shockwaveTimer / totalDuration); // 0→1
            float radius = GetShockwaveRadius(progress);
            float alpha = 1f - progress; // 渐隐

            Gizmos.color = new Color(1f, 0.8f, 0f, alpha); // 金色冲击波
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        // 显示状态文字
        #if UNITY_EDITOR
        string stateInfo;
        if (_cooldownTimer > 0)
            stateInfo = $"CD: {_cooldownTimer:F1}s";
        else if (_attackTimer > 0)
            stateInfo = $"ATK: {_attackTimer:F1}s";
        else
            stateInfo = "Ready";

        UnityEditor.Handles.Label(headPos + Vector3.up * 0.4f, stateInfo);
        #endif
    }
}
