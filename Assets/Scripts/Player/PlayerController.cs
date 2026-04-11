using LcIcemFramework.Managers;
using LcIcemFramework.Util.Ext;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家控制器
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;

    private Rigidbody2D _rb;
    private Animator _animator;    

    private PlayerInput _playerInput;
    private Vector2 _moveInput;
    private InputAction _moveAction;
    private PlayerData _playerData;

    private void Awake()
    {
        // 获得组件
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();
        _animator = GetComponent<Animator>();

        // 初始化rigidbody
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        // 获取移动输入的InputAction
        _moveAction = _playerInput.actions["Move"];

        // 设置玩家数据
        _playerData = GameDataManager.Instance.GetPlayerData();
        _moveSpeed = _playerData.moveSpeed;
    }

    private void FixedUpdate()
    {
        Move();
    }

    void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>().normalized;
        SetAnim();
    }

    public void Move()
    {
        _rb.linearVelocity = _moveInput * _moveSpeed;
    }

    private void SetAnim()
    {
        if (_rb.linearVelocity != Vector2.zero)
        {
            _animator.SetBool("isMoving", true);
        }
        else
        {
            _animator.SetBool("isMoving", false);
        }
    }
}
