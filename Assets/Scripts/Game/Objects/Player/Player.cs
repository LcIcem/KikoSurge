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

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _fsm = new PlayerFSM(this, _animator);
        _rigidbody = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        weaponHandler = new WeaponHandler(this);

        _moveInput = Vector2.zero;
    }

    private void Start()
    {
        _fsm.Start();

        // 从 RoleInfo 配置初始化武器
        var roleInfo = GameDataManager.Instance.GetRoleDataByCurSel();
        weaponHandler.InitializeWeapons(roleInfo.initialWeaponIds);

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
            PlayerMoveState => GameDataManager.Instance.PlayerData.moveSpeed,
            PlayerShootState => GameDataManager.Instance.PlayerData.moveSpeed * 0.5f,
            PlayerReloadState => GameDataManager.Instance.PlayerData.moveSpeed * 0.7f,
            PlayerHurtState => GameDataManager.Instance.PlayerData.moveSpeed * 0.9f,
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
            _fsm.SetBool("isDead", true);
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

        float hp = GameDataManager.Instance.PlayerData.Health;
        if (hp <= 0)
        {
            Debug.Log($"[Player] HP 为 0，伤害 {damage} 被忽略");
            return;
        }

        hp -= damage;
        Debug.Log($"[Player] 受伤！伤害: {damage}, 剩余HP: {hp}");

        // 启动无敌帧
        _invincibleTimer = GameDataManager.Instance.PlayerData.invincibleDuration;
        _isInvincible = true;

        // 触发受伤动画（同时通知 FSM 和 Animator）
        _fsm.SetTrigger("hurt");

        // 启动闪烁协程
        StartCoroutine(HurtFlashCoroutine());

        EventCenter.Instance.Publish(GameEventID.OnPlayerDamaged,
            new DamageParams { damage = damage, from = transform.position });

        if (hp <= 0f)
        {
            _fsm.SetBool("isDead", true);
        }

        GameDataManager.Instance.PlayerData.Health = hp;
        EventCenter.Instance.Publish(GameEventID.UpdateHeartDisplay, GameDataManager.Instance.PlayerData);
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