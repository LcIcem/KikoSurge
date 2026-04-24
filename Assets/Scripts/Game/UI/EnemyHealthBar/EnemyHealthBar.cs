using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人头顶血条 UI
/// 参考 Interactable 的实现方式：UI 作为子对象预设，代码只控制显隐
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("血条配置")]
    [Tooltip("血条隐藏延迟（秒）")]
    [SerializeField] private float _hideDelay = 1f;

    [Header("血条UI引用（拖拽赋值）")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _fillImage;

    private float _hideTimer;
    private bool _isVisible;

    private void Awake()
    {
        // 初始隐藏
        Hide();
    }

    private void Update()
    {
        if (!_isVisible) return;

        // 更新隐藏计时器
        _hideTimer -= Time.deltaTime;
        if (_hideTimer <= 0f)
        {
            Hide();
        }
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
