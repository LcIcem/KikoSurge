using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家角色：持有 FSM、武器、输入（通过 PlayerInput），处理移动/射击/受击。
/// 注意：动画由 AnimatorController 驱动，FSM 只负责逻辑状态和设置 Animator 参数。
/// </summary>
public class Player : MonoBehaviour, IPoolable
{
    // 组件引用 
    [SerializeField] private CircleCollider2D _hitCollider;
    [HideInInspector] public Rigidbody2D _rigidbody;
    private SpriteRenderer _sprite;
    private Animator _animator;


    // 子系统 
    private PlayerFSM _fsm;
    private WeaponHandler _weaponHandler;

    // ========== 运行时状态 ==========
    public float HP { get; private set; }
    public bool IsAlive => HP > 0f;

    /// <summary>当前移动方向（供 FSM/其他系统读取）</summary>
    public Vector2 MoveDir { get; private set; }

    /// <summary>当前瞄准方向（供 FSM/其他系统读取）</summary>
    public Vector2 AimDir { get; private set; }

    // ========== Unity 生命周期 ==========
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _fsm = new PlayerFSM(this, _animator);
        _rigidbody = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _weaponHandler = new WeaponHandler(this, _animator);

        HP = _data.MaxHp;
    }

    private void Start()
    {
        _fsm.Start();
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

        // FSM 驱动
        _fsm.Update();
    }

    // 处理玩家输入
    private void HandleInput()
    {
        MoveDir = InputManager.Instance.UIActions["Move"].ReadValue<Vector2>().normalized;
        _mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    private void FixedUpdate()
    {
        if (_fsm.CurrentState is PlayerMoveState)
            _rigidbody.MovePosition(_rigidbody.position + MoveDir * _data.MoveSpeed * Time.fixedDeltaTime);
    }

    // ========== PlayerInput 回调（由 InputSystem 自动调用） ==========
    private Vector3? _mouseWorldPos;

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.started)  // 按下瞬间触发
            _fsm.SetTrigger("shoot");
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
            _fsm.SetTrigger("dash");
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        // 鼠标位置：屏幕坐标 → 世界坐标（Z=0 平面）
        Vector2 screenPos = context.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        worldPos.z = 0;
        _mouseWorldPos = worldPos;
    }

    // ========== 伤害处理 ==========
    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        HP -= damage;
        _fsm.SetTrigger("hurt");

        EventCenter.Instance.Publish(EventID.Combat_PlayerDamaged,
            new DamageParams { damage, from = transform.position });

        if (HP <= 0f)
        {
            HP = 0f;
            _fsm.SetTrigger("dead");
        }
    }

    // ========== IPoolable ==========
    public void OnSpawn()
    {
        HP = _data.MaxHp;
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        _fsm.Stop();
        gameObject.SetActive(false);
    }
}