using LcIcemFramework.Util.Ext;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    private Animator animator;

    private Rigidbody2D _rb;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        Vector2 targetPos = _rb.position + Vector2.right * speed * Time.fixedDeltaTime;
        _rb.MovePosition(targetPos);
        // transform.Translate(_moveInput * speed * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (_moveInput != Vector2.zero)
            animator.SetBool("IsMoving", true);
        else 
            animator.SetBool("IsMoving", false);
    }

    public void Move(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    public void Jump(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            Debug.Log("Jump pressed");
        }
    }
}
