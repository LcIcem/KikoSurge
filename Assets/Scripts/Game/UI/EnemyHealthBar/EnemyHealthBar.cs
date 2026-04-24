using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人头顶血条 UI
/// 参考飘字DamageNumberUI的坐标转换方式
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("血条配置")]
    [Tooltip("血条偏移（世界单位）")]
    [SerializeField] private float _offsetY = 1.5f;
    [Tooltip("血条隐藏延迟（秒）")]
    [SerializeField] private float _hideDelay = 1f;

    [Header("血条UI引用（拖拽赋值）")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _fillImage;

    private Camera _worldCamera;
    private Canvas _uiCanvas;
    private RectTransform _canvasRect;
    private RectTransform _healthBarRect;
    private Transform _targetTransform;
    private float _hideTimer;
    private bool _isVisible;

    private void Awake()
    {
        _healthBarRect = GetComponent<RectTransform>();
        _worldCamera = Camera.main;

        // 查找UI Canvas
        _uiCanvas = FindFirstObjectByType<Canvas>();
        if (_uiCanvas != null)
        {
            _canvasRect = _uiCanvas.GetComponent<RectTransform>();
        }
    }

    private void LateUpdate()
    {
        if (!_isVisible || _targetTransform == null) return;

        // 更新隐藏计时器
        _hideTimer -= Time.deltaTime;
        if (_hideTimer <= 0f)
        {
            Hide();
            return;
        }

        // 参考飘字的世界坐标转UI坐标
        Vector3 worldPos = _targetTransform.position + Vector3.up * _offsetY;

        // 世界坐标 → 屏幕坐标
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_worldCamera, worldPos);

        // 屏幕坐标 → UI局部坐标
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPos,
            null,
            out localPos
        );

        // 设置血条位置（相对于Canvas）
        _healthBarRect.localPosition = localPos;
    }

    /// <summary>
    /// 绑定目标敌人
    /// </summary>
    public void BindTo(Transform target)
    {
        _targetTransform = target;
    }

    /// <summary>
    /// 初始化血条
    /// </summary>
    public void Init(float maxHP)
    {
        if (_fillImage != null)
            _fillImage.fillAmount = 1f;
        Hide();
    }

    /// <summary>
    /// 更新血条（受伤时调用）
    /// </summary>
    public void UpdateHealth(float currentHP, float maxHP)
    {
        if (_fillImage != null && maxHP > 0)
            _fillImage.fillAmount = currentHP / maxHP;

        Show();
        _hideTimer = _hideDelay;
    }

    /// <summary>
    /// 隐藏血条
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示血条
    /// </summary>
    public void Show()
    {
        _isVisible = true;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }
}
