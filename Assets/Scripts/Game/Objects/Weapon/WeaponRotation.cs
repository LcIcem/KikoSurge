using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 武器旋转脚本
/// </summary>
public class WeaponRotation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private float _offset;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private float _aimDeadzoneRadius = 0.5f;
    public bool IsActive => AimInput.Enabled;

    private Vector3 _mousePos;      // 鼠标世界坐标
    private Transform _weaponPivot; // 武器要挂到的锚点的Transform
    private bool _wasFlipped;      // 记录上次翻转状态，用于检测切换
    private float _stableAngle;     // 上次稳定的角度，用于死区时保持角度

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

    // 处理武器旋转
    private void HandleRotation()
    {
        // 获取鼠标坐标
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        _mousePos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));

        // 计算武器锚点到鼠标的一条方向向量
        Vector3 dir = _mousePos - _weaponPivot.position;

        // 死区：距离过小时使用上次稳定角度，避免微小向量导致Atan2震荡
        if (dir.magnitude < _aimDeadzoneRadius)
        {
            transform.rotation = Quaternion.AngleAxis(_stableAngle, Vector3.forward);
            transform.localPosition = transform.rotation * new Vector3(_offset, 0, 0);
            return;
        }

        // 从该方向向量计算出沿x轴正向开始的角度
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _stableAngle = angle;  // 记录稳定角度

        // 根据旋转的角度决定是否翻转sprite
        bool shouldFlip = angle >= 86 || angle <= -86;
        if (shouldFlip != _wasFlipped)
        {
            _sprite.flipY = shouldFlip;
            _wasFlipped = shouldFlip;
            SyncMuzzleFlip(shouldFlip);
        }

        // 按照角度进行旋转
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        // 得到旋转后的偏移向量
        Vector3 offsetVector = transform.rotation * new Vector3(_offset, 0, 0);
        // 将本地坐标设置为便宜向量
        transform.localPosition = offsetVector;
    }

    // 同步Muzzle位置：当flipY切换时，取反Muzzle的本地Y坐标以匹配视觉翻转
    private void SyncMuzzleFlip(bool flipped)
    {
        if (_muzzle == null) return;

        Vector3 localPos = _muzzle.localPosition;
        localPos.y = -localPos.y;
        _muzzle.localPosition = localPos;
    }

}