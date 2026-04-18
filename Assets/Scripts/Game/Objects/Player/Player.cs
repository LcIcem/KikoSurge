using UnityEngine;
using LcIcemFramework.Core;
using System.Collections;
using System.Collections.Generic;
using LcIcemFramework;
using Game.Event;


/// <summary>
/// 玩家角色：持有 FSM、武器、输入（通过 PlayerInput），处理移动/射击/受击。
/// 注意：动画由 AnimatorController 驱动，FSM 只负责逻辑状态和设置 Animator 参数。
/// </summary>
public class Player : MonoBehaviour
{
    // 组件引用 
    [SerializeField] private CapsuleCollider2D _hitCollider;
    [HideInInspector] public Rigidbody2D _rigidbody;
    private SpriteRenderer _sprite;
    private Animator _animator;


    // 子系统
    private PlayerFSM _fsm;
    public WeaponHandler weaponHandler;

    // 玩家运行时数据
    private PlayerRuntimeData _playerData;

    /// <summary>
    /// 运行时数据（供 FSM 内部访问）
    /// </summary>
    internal PlayerRuntimeData RuntimeData => _playerData;

    [Header("武器挂点")]
    [Tooltip("武器挂点，所有武器将创建在此 Transform 下")]
    [SerializeField] private Transform _weaponPivot;

    /// <summary>
    /// 武器挂点
    /// </summary>
    public Transform WeaponPivot => _weaponPivot;

    // 输入状态
    private Vector2 _moveInput;

    // 当前移动方向（供 FSM/其他系统读取）
    public Vector2 MoveDir { get; private set; }

    // 当前瞄准方向（供 FSM/其他系统读取）
    public Vector2 AimDir { get; private set; }

    // 无敌帧
    private float _invincibleTimer = 0f;
    private bool _isInvincible = false;

    // 死亡标记（用于防止死亡后继续处理输入）
    private bool _isDead = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _fsm = new PlayerFSM(this, _animator);
        _rigidbody = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        weaponHandler = new WeaponHandler(this);

        _moveInput = Vector2.zero;
    }

    /// <summary>
    /// 初始化玩家数据（由 PlayerHandler 调用）
    /// </summary>
    public void Initialize(PlayerRuntimeData data, List<int> weaponIds)
    {
        _playerData = data;
        weaponHandler.InitializeWeapons(weaponIds);
    }

    private void Start()
    {
        _fsm.Start();

        // 订阅事件
        EventCenter.Instance.Subscribe<WeaponBase>(GameEventID.Combat_Reloading, OnReloading);
        EventCenter.Instance.Subscribe<WeaponBase>(GameEventID.Combat_Reloaded, OnReloaded);
        EventCenter.Instance.Subscribe(GameEventID.Combat_CancelReload, OnCancelReload);
        EventCenter.Instance.Subscribe<EnemyHitPlayerParams>(GameEventID.Combat_EnemyHitPlayer, OnEnemyHitPlayer);
    }

    void OnDestroy()
    {
        _fsm.Stop();

        // 清理所有武器（交还对象池）
        weaponHandler.ClearAllWeapons();

        // 退订事件
        EventCenter.Instance.Unsubscribe<WeaponBase>(GameEventID.Combat_Reloading, OnReloading);
        EventCenter.Instance.Unsubscribe<WeaponBase>(GameEventID.Combat_Reloaded, OnReloaded);
        EventCenter.Instance.Unsubscribe(GameEventID.Combat_CancelReload, OnCancelReload);
        EventCenter.Instance.Unsubscribe<EnemyHitPlayerParams>(GameEventID.Combat_EnemyHitPlayer, OnEnemyHitPlayer);
    }

    private float _curSpeed = 0f;
    void FixedUpdate()
    {
        _curSpeed = _fsm.CurrentState switch
        {
            PlayerMoveState => _playerData.moveSpeed,
            PlayerShootState => _playerData.moveSpeed * 0.5f,
            PlayerReloadState => _playerData.moveSpeed * 0.7f,
            PlayerHurtState => _playerData.moveSpeed * 0.9f,
            _ => 0f
        };

        if (_fsm.CurrentState is not PlayerDashState)
            _rigidbody.MovePosition(_rigidbody.position + MoveDir * _curSpeed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        HandleInput();

        // 瞄准方向
        AimDir = InputManager.Instance.GetAimDirection(transform.position);

        // 无敌帧更新
        if (_isInvincible)
        {
            _invincibleTimer -= Time.deltaTime;
            if (_invincibleTimer <= 0f)
            {
                _isInvincible = false;
            }
        }

        // FSM 驱动
        _fsm.Update();
    }

    // 处理玩家输入
    private void HandleInput()
    {
        // Guard: 死亡后不处理任何输入
        if (_isDead)
            return;

        // Guard: Only process input when Player action map is active
        if (!InputManager.Instance.Actions.ContainsKey("Move"))
            return;

        // 处理移动
        _moveInput = InputManager.Instance.Actions["Move"].ReadValue<Vector2>();
        MoveDir = _moveInput.normalized;
        _fsm.SetBool("isMoving", MoveDir.magnitude >= 0.1f);
        _fsm.SetBool("isIdle", MoveDir.magnitude < 0.1f);

        // 处理射击
        if (weaponHandler.CurrentWeapon != null &&
            weaponHandler.CurrentWeapon.CanFire &&
            InputManager.Instance.Actions["Shoot"].IsPressed())
        {
            _fsm.SetTrigger("shoot");
        }

        // 处理冲刺
        if (InputManager.Instance.Actions["Dash"].WasPressedThisFrame())
        {
            _fsm.SetTrigger("dash");
        }

        // 调试用
        if (InputManager.Instance.Actions["Dead"].WasPressedThisFrame())
        {
            TriggerDeath();
        }
        if (InputManager.Instance.Actions["SwitchWeapon"].WasPressedThisFrame())
        {
            weaponHandler.SwitchToNextWeapon();
            var weapon = weaponHandler.CurrentWeapon;
            if (weapon != null)
                Debug.Log("当前武器为" + weapon.Config.gunName + ", 武器当前容量：" + weapon.CurrentAmmo);
        }
    }

    // 伤害处理
    public void TakeDamage(float damage)
    {
        // 无敌帧期间忽略伤害
        if (_isInvincible)
        {
            Debug.Log($"[Player] 无敌帧中，伤害 {damage} 被忽略");
            return;
        }

        float hp = _playerData.Health;
        if (hp <= 0)
        {
            Debug.Log($"[Player] HP 为 0，伤害 {damage} 被忽略");
            return;
        }

        hp -= damage;
        Debug.Log($"[Player] 受伤！伤害: {damage}, 剩余HP: {hp}");

        // 启动无敌帧
        _invincibleTimer = _playerData.invincibleDuration;
        _isInvincible = true;

        // 非致命伤害才触发受伤动画（致命伤害直接走死亡流程）
        if (hp > 0f)
        {
            _fsm.SetTrigger("hurt");
            StartCoroutine(HurtFlashCoroutine());
        }

        EventCenter.Instance.Publish(GameEventID.OnPlayerDamaged,
            new DamageParams { damage = damage, from = transform.position });

        _playerData.Health = hp;
        EventCenter.Instance.Publish(GameEventID.UpdateHeartDisplay, _playerData);

        // 同步生命值到 SessionManager（用于检查点保存）
        SessionManager.Instance?.SetPlayerHealth(hp);

        if (hp <= 0f)
        {
            TriggerDeath();
        }

    }

    /// <summary>
    /// 触发死亡（被攻击死亡和调试按键死亡共用）
    /// </summary>
    public void TriggerDeath()
    {
        Debug.Log($"[Player] TriggerDeath called. HP before death: {_playerData.Health}");

        // 停止武器跟随鼠标（立即停止，避免死亡动画播放期间武器还在转）
        var weaponRotations = GetComponentsInChildren<WeaponRotation>();
        foreach (var wr in weaponRotations)
        {
            AimInput.Enabled = false;
        }

        // 设置死亡标记，防止死亡后继续处理输入
        _isDead = true;

        // 设置死亡标志，FSM 会在下一帧 UpdateTransition 处理到 DeadState
        _fsm.SetBool("isDead", true);

        // 通知外部玩家已死亡（订阅者会等死亡动画结束后才显示 GameOver）
        EventCenter.Instance.Publish(GameEventID.OnPlayerDeath);

        // 启动协程：等死亡动画播放完毕后触发 GameOver
        StartCoroutine(WaitDeathAnimationAndGameOver());
    }

    /// <summary>
    /// 等待死亡动画播放完毕后触发 GameOver
    /// </summary>
    private IEnumerator WaitDeathAnimationAndGameOver()
    {
        if (_animator == null)
        {
            EventCenter.Instance.Publish(GameEventID.OnDeathAnimationEnd);
            yield break;
        }

        // 记录死亡动画开始前的状态
        var preHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        var preNormTime = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        // 等待动画状态切换（说明死亡动画开始播放）
        yield return new WaitUntil(() =>
        {
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash != preHash || info.normalizedTime < preNormTime;
        });

        // 等待死亡动画播放完毕
        yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
        EventCenter.Instance.Publish(GameEventID.OnDeathAnimationEnd);
    }

    // 闪烁协程
    private IEnumerator HurtFlashCoroutine()
    {
        if (_sprite == null)
        {
            Debug.LogError("[Player] HurtFlashCoroutine: _sprite is null!");
            yield break;
        }

        Debug.Log($"[Player] 受伤闪烁开始，无敌时间: {_invincibleTimer}s");
        float elapsed = 0f;
        float flashInterval = 0.1f;  // 闪烁间隔（0.1秒更明显）
        bool visible = true;

        while (elapsed < _invincibleTimer)
        {
            visible = !visible;
            _sprite.enabled = visible;
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        _sprite.enabled = true;  // 确保结束时可见
        Debug.Log("[Player] 受伤闪烁结束");
    }

    // 敌人伤害处理（碰撞/子弹/攻击）
    private void OnEnemyHitPlayer(EnemyHitPlayerParams p)
    {
        Debug.Log($"[Player] OnEnemyHitPlayer: damageType={p.damageType}, damage={p.damage}");
        switch (p.damageType)
        {
            case EnemyDamageType.Collision:
                // 碰撞伤害：后续可扩展击退等逻辑
                TakeDamage(p.damage);
                break;
            case EnemyDamageType.Attack:
            default:
                TakeDamage(p.damage);
                break;
        }
    }

    private void OnReloading(WeaponBase weapon)
    {
        _fsm.SetBool("isReload", true);
    }

    private void OnReloaded(WeaponBase weapon)
    {
        _fsm.SetBool("isReload", false);
    }

    private void OnCancelReload()
    {
        _fsm.SetBool("isReload", false);
    }
}