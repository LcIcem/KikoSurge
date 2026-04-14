using UnityEngine;
using UnityEngine.InputSystem;
using LcIcemFramework.Core;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using LcIcemFramework.Managers;


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
    private WeaponFactory _weaponFactory;

    [SerializeField] private GameObject _bulletPrefab;

    // 输入状态
    private Vector2 _moveInput;

    // 当前移动方向（供 FSM/其他系统读取）
    public Vector2 MoveDir { get; private set; }

    // 当前瞄准方向（供 FSM/其他系统读取）
    public Vector2 AimDir { get; private set; }
    private Vector3? _mouseWorldPos;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _fsm = new PlayerFSM(this, _animator);
        _rigidbody = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        weaponHandler = new WeaponHandler(this);
        _weaponFactory = new WeaponFactory();

        _moveInput = Vector2.zero;
    }

    private void Start()
    {
        _fsm.Start();

        // 从配置加载初始武器
        var initialWeaponIds = new[] { 101, 102 }; // 枪械ID=101, 霰弹枪ID=102

        // 开始加载武器
        int loadedCount = 0;
        foreach (var weaponId in initialWeaponIds)
        {
            var config = GameDataManager.Instance.GetWeaponConfig(weaponId);
            if (config == null)
            {
                loadedCount++;
                continue;
            }

            _weaponFactory.CreateWeapon(config, this, weapon =>
            {
                if (weapon != null)
                {
                    weaponHandler.AddWeapon(weapon);
                }

                loadedCount++;
                if (loadedCount >= initialWeaponIds.Length)
                {
                    // 所有武器加载完毕后再装备第一把
                    weaponHandler.EquipWeapon(0);
                }
            });
        }

        // 订阅事件
        EventCenter.Instance.Subscribe<WeaponBase>(EventID.Combat_Reloading, OnReloading);
        EventCenter.Instance.Subscribe<WeaponBase>(EventID.Combat_Reloaded, OnReloaded);
        EventCenter.Instance.Subscribe(EventID.Combat_CancelReload, OnCancelReload);
    }

    void OnDestroy()
    {
        _fsm.Stop();

        // 退订事件
        EventCenter.Instance.Unsubscribe<WeaponBase>(EventID.Combat_Reloading, OnReloading);
        EventCenter.Instance.Unsubscribe<WeaponBase>(EventID.Combat_Reloaded, OnReloaded);
        EventCenter.Instance.Unsubscribe(EventID.Combat_CancelReload, OnCancelReload);
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
        if (_mouseWorldPos.HasValue)
        {
            Vector2 aimDir = ((Vector2)_mouseWorldPos.Value - (Vector2)transform.position).normalized;
            if (aimDir.magnitude > 0.01f)
                AimDir = aimDir;
        }

        // 更新武器相关信息，用于维护武器状态（比如：武器冷却）
        weaponHandler.Update();

        // FSM 驱动
        _fsm.Update();
    }

    // 处理玩家输入
    private void HandleInput()
    {
        // 处理移动
        _moveInput = InputManager.Instance.UIActions["Move"].ReadValue<Vector2>();
        MoveDir = _moveInput.normalized;
        _fsm.SetBool("isMoving", MoveDir.magnitude >= 0.1f);
        _fsm.SetBool("isIdle", MoveDir.magnitude < 0.1f);

        // 处理鼠标位置
        _mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // 处理射击
        if (weaponHandler.CurrentWeapon != null &&
            weaponHandler.CurrentWeapon.CanFire &&
            InputManager.Instance.UIActions["Shoot"].IsPressed())
        {
            _fsm.SetTrigger("shoot");
        }

        // 处理冲刺
        if (InputManager.Instance.UIActions["Dash"].WasPressedThisFrame())
        {
            _fsm.SetTrigger("dash");
        }

        // 调试用
        if (InputManager.Instance.UIActions["Dead"].WasPressedThisFrame())
        {
            _fsm.SetBool("isDead", true);
        }
        if (InputManager.Instance.UIActions["Hurt"].WasPressedThisFrame())
        {
            _fsm.SetTrigger("hurt");
        }
        if (InputManager.Instance.UIActions["Switch"].WasPressedThisFrame())
        {
            weaponHandler.SwitchToNextWeapon();
            var weapon = weaponHandler.CurrentWeapon;
            if (weapon != null)
                Debug.Log("当前武器为" + weapon.Type.ToString() + ", 武器当前容量：" + weapon.CurrentAmmo);
        }
    }

    // 伤害处理 
    public void TakeDamage(float damage)
    {
        float hp = GameDataManager.Instance.PlayerData.Health;
        if (hp <= 0) return;

        hp -= damage;
        _fsm.SetTrigger("hurt");

        EventCenter.Instance.Publish(EventID.Combat_PlayerDamaged,
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