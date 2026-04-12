using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 控制人物翻转组件
/// </summary>
public class FlipController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Rigidbody2D _entity;

    [Header("翻转相关")]
    [SerializeField] private bool _useMouseForFlipping;
    [SerializeField] private bool _flipX = true;

    private bool _isFacingRight = true;

    void Update()
    {
        CheckSwitchInput();
        if (_useMouseForFlipping)
            FlipBaseOnMosue();
        else
            FlipBaseOnInput();
    }

    private void CheckSwitchInput()
    {
        InputAction switchAction = InputManager.Instance.UIActions["SwitchWeapon"];
        if (switchAction.WasPerformedThisFrame())
        {
            _useMouseForFlipping = !_useMouseForFlipping;
        }
    }

    // 根据鼠标位置判断是否旋转
    private void FlipBaseOnMosue()
    {
        // 检查是否达到翻转条件
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        bool shouldFaceRight = mouseWorldPos.x > transform.position.x;
        if (shouldFaceRight != _isFacingRight)
        {
            FlipSprite(shouldFaceRight);
        }
    }

    // 根据输入判断是否翻转
    private void FlipBaseOnInput()
    {
        // 检查是否存在水平输入
        if (Mathf.Abs(_entity.linearVelocityX) > 0.1f)
        {
            bool shouldFaceRight = _entity.linearVelocityX > 0;
            if (shouldFaceRight != _isFacingRight)
            {
                FlipSprite(shouldFaceRight);
            }
        }
    }

    // 翻转Sprite
    private void FlipSprite(bool faceRight)
    {
        _isFacingRight = faceRight;
        if (_flipX)
        {
            // 通过flipx属性翻转
            _sprite.flipX = !_isFacingRight;
        }
        else
        {
            // 通过rotation属性翻转
            transform.rotation = Quaternion.Euler(0, faceRight ? 0 : 180, 0);
        }
    }
}
