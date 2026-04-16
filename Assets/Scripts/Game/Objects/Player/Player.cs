using UnityEngine;
using LcIcemFramework.Core;
using UnityEngine.Rendering.Universal;
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

    [Header("初始武器配置")]
    [SerializeField] private GunConfig[] _initialWeapons;

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

        // 从配置加载初始武器
        if (_initialWeapons != null)
        {
            foreach (var config in _initialWeapons)
            {
                if (config == null) continue;
                CreateWeaponFromConfig(config);
            }

            if (_initialWeapons.Length > 0)
            {
                weaponHandler.EquipWeapon(0);
            }
        }

        // 订阅事件
        EventCenter.Instance.Subscribe<WeaponBase>(GameEventID.Combat_Reloading, OnReloading);
        EventCenter.Instance.Subscribe<WeaponBase>(GameEventID.Combat_Reloaded, OnReloaded);
        EventCenter.Instance.Subscribe(GameEventID.Combat_CancelReload, OnCancelReload);
        EventCenter.Instance.Subscribe<EnemyAttackDamageParams>(GameEventID.Combat_EnemyHitPlayer, OnEnemyAttackDamage);
        EventCenter.Instance.Subscribe<EnemyCollisionDamageParams>(GameEventID.Combat_EnemyHitPlayer, OnEnemyCollisionDamage);
    }

    /// <summary>
    /// 根据配置创建武器
    /// </summary>
    private void CreateWeaponFromConfig(GunConfig config)
    {
        if (config.gunPrefab == null)
        {
            Debug.LogError($"[Player] 武器配置 {config.gunName} 没有指定预设体");
            return;
        }

        // 实例化武器预设体（预设体上挂载 WeaponBase 组件）
        var weaponObj = Instantiate(config.gunPrefab, transform);
        weaponObj.SetActive(false);

        var weapon = weaponObj.GetComponent<WeaponBase>();
        if (weapon == null)
        {
            Debug.LogError($"[Player] 武器预设体 {config.gunName} 上没有 WeaponBase 组件");
            return;
        }

        weapon.Init(config);
        weaponHandler.AddWeapon(weapon);
    }

    void OnDestroy()
    {
        _fsm.Stop();

        // 退订事件
        EventCenter.Instance.Unsubscribe<WeaponBase>(GameEventID.Combat_Reloading, OnReloading);
        EventCenter.Instance.Unsubscribe<WeaponBase>(GameEventID.Combat_Reloaded, OnReloaded);
        EventCenter.Instance.Unsubscribe(GameEventID.Combat_CancelReload, OnCancelReload);
        EventCenter.Instance.Unsubscribe<EnemyAttackDamageParams>(GameEventID.Combat_EnemyHitPlayer, OnEnemyAttackDamage);
        EventCenter.Instance.Unsubscribe<EnemyCollisionDamageParams>(GameEventID.Combat_EnemyHitPlayer, OnEnemyCollisionDamage);
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
        if (_isInvincible) return;

        float hp = GameDataManager.Instance.PlayerData.Health;
        if (hp <= 0) return;

        hp -= damage;

        // 启动无敌帧
        _invincibleTimer = GameDataManager.Instance.PlayerData.invincibleDuration;
        _isInvincible = true;

        // 触发受伤动画
        _animator.SetTrigger("hurt");

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
        float elapsed = 0f;
        float flashInterval = 0.05f;  // 闪烁频率
        bool visible = true;

        while (elapsed < _invincibleTimer)
        {
            visible = !visible;
            _sprite.enabled = visible;
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        _sprite.enabled = true;  // 确保结束时可见
    }

    // 敌人攻击伤害处理（子弹/范围检测）
    private void OnEnemyAttackDamage(EnemyAttackDamageParams p)
    {
        TakeDamage(p.damage);
    }

    // 敌人碰撞伤害处理
    private void OnEnemyCollisionDamage(EnemyCollisionDamageParams p)
    {
        TakeDamage(p.damage);
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