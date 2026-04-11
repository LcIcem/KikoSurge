using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 控制人物翻转组件
/// </summary>
public class FlipController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Rigidbody2D _entity;

    [Header("Flipping References")]
    [SerializeField] private bool _useMouseForFlipping;
    [SerializeField] private bool _flipX = true;

    private bool _isFacingRight = true;

    void Update()
    {
        FlipBaseOnInput();
    }

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
