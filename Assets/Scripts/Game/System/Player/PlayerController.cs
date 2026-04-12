using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using LcIcemFramework.Util.Ext;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家控制器
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // 玩家组件相关
    private Rigidbody2D _rb;
    private Animator _animator;

    // 玩家输入相关
    private Vector2 _moveInput;
    private bool _attackBtnPressed;

    // 玩家状态控制相关
    public bool _isDead = false; // TODO：暂时用来调试 所以设置为公开字段，实际需要设置为只读属性

    private void Awake()
    {
        // 获得组件
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();

        // 初始化rigidbody
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        // 玩家没有死亡时进行移动，如果死亡将速度设为0
        if (!_isDead) Move();
        else _rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        // 处理输入
        HandleInput();
        // 检测玩家是否攻击
        CheckAttack();
        // 处理动画
        HandleAnim();

        // 监听玩家是否死亡
        if (GameDataManager.Instance.PlayerData.Heath <= 0)
        {
            EventCenter.Instance.Publish(EventID.PlayerIsDead);
            _isDead = true;
        }
        else _isDead = false;
    }

    // 玩家移动
    public void Move()
    {
        _rb.linearVelocity = _moveInput * GameDataManager.Instance.PlayerData.moveSpeed;
    }

    // 检测玩家是否攻击
    public void CheckAttack()
    {
        if (_attackBtnPressed)
        {
            Weapon weapon = transform.Find("WeaponPivot").GetChild(0).GetComponent<Weapon>();
            EventCenter.Instance.Publish(EventID.AttackPerformed, weapon);
        }
    }

    // 处理玩家输入
    void HandleInput()
    {
        InputAction moveAction = InputManager.Instance.UIActions["Move"];
        InputAction attackAction = InputManager.Instance.UIActions["Attack"];
        _moveInput = moveAction.ReadValue<Vector2>().normalized;
        _attackBtnPressed = attackAction.IsPressed();
    }

    // 处理动画
    private void HandleAnim()
    {
        // isDead
        if (_isDead)
        {
            _animator.SetBool("isDead", true);
            return;
        }

        // isMoving
        if (_rb.linearVelocity != Vector2.zero)
        {
            _animator.SetBool("isMoving", true);
        }
        else
        {
            _animator.SetBool("isMoving", false);
        }
    }

    // 重启关卡（调试用）
    public void ReStartLevel()
    {
        EventCenter.Instance.Publish(EventID.RestartGame);
    }
}
