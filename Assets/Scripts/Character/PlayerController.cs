using LcIcemFramework.Util.Ext;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;
    private PlayerInput _playerInput;

    private float _moveSpeed = 10f;
    private InputAction _moveAction;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();

        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        _moveAction = _playerInput.actions["Move"];
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _moveInput * _moveSpeed * Time.fixedDeltaTime);
    }

    void Update()
    {
        Move();
    }

    public void Move()
    {
        _moveInput = _moveAction.ReadValue<Vector2>().normalized;
    }
}
