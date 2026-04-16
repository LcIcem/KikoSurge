using UnityEngine;
using LcIcemFramework.Core;
using UnityEngine.Rendering.Universal;
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
        if (InputManager.Instance.Actions["Hurt"].WasPressedThisFrame())
        {
            _fsm.SetTrigger("hurt");
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
        float hp = GameDataManager.Instance.PlayerData.Health;
        if (hp <= 0) return;

        hp -= damage;
        _fsm.SetTrigger("hurt");

        EventCenter.Instance.Publish(GameEventID.Combat_PlayerDamaged,
            new DamageParams { damage = damage, from = transform.position });

        if (hp <= 0f)
        {
            _fsm.SetTrigger("dead");
        }

        GameDataManager.Instance.PlayerData.Health = hp;
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