using LcIcemFramework.Util.Ext;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 10f;

    private Rigidbody2D _rb;
    private Animator _animator;    

    private PlayerInput _playerInput;
    private Vector2 _moveInput;
    private InputAction _moveAction;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();
        _animator = GetComponent<Animator>();

        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        _moveAction = _playerInput.actions["Move"];
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
