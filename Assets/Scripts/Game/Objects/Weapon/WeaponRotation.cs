using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 武器旋转脚本
/// </summary>
public class WeaponRotation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private float _offset;
    public bool IsActive => !_isDead && Time.timeScale != 0;

    private bool _isDead;
    private Vector3 _mousePos;      // 鼠标世界坐标
    private Transform _weaponPivot; // 武器要挂到的锚点的Transform

    void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        _weaponPivot = transform.parent;
    }

    void Update()
    {
        if (IsActive)
            HandleRotation();
    }

    /// <summary>
    /// 设置死亡状态（由 Player 死亡时调用）
    /// </summary>
    public void SetDead(bool dead)
    {
        _isDead = dead;
    }

    // 处理武器旋转
    private void HandleRotation()
    {
        // 获取鼠标坐标
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        _mousePos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));

        // 计算武器锚点到鼠标的一条方向向量
        Vector3 dir = _mousePos - _weaponPivot.position;
        // 从该方向向量计算出沿x轴正向开始的角度
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 根据旋转的角度决定是否翻转sprite
        if (angle >= 86 || angle <= -86)
        {
            _sprite.flipY = true;
        }
        else
        {
            _sprite.flipY = false;
        }

        // 按照角度进行旋转
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        // 得到旋转后的偏移向量
        Vector3 offsetVector = transform.rotation * new Vector3(_offset, 0, 0);
        // 将本地坐标设置为便宜向量
        transform.localPosition = offsetVector;
    }

}