using UnityEngine;
using TMPro;
using System.Collections;
using LcIcemFramework;

/// <summary>
/// 伤害数字飘字组件
/// 实现 IPoolable 接口，对象池生命周期由池管理
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class DamageNumberUI : MonoBehaviour, IPoolable
{
    [Header("动画配置")]
    [Tooltip("飘动距离（像素）")]
    [SerializeField] private float _floatDistance = 80f;
    [Tooltip("飘动持续时间（秒）")]
    [SerializeField] private float _floatDuration = 1f;
    [Tooltip("开始淡出的时间比例（0~1）")]
    [SerializeField] private float _fadeStartRatio = 0.5f;

    [Header("颜色配置")]
    [Tooltip("普通伤害颜色")]
    [SerializeField] private Color _normalColor = Color.white;
    [Tooltip("暴击伤害颜色")]
    [SerializeField] private Color _critColor = new Color(1f, 0.8f, 0f);
    [Tooltip("玩家受伤颜色")]
    [SerializeField] private Color _playerDamageColor = Color.red;

    private TextMeshProUGUI _text;
    private RectTransform _rectTransform;
    private Coroutine _animCoroutine;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _rectTransform = GetComponent<RectTransform>();
    }

    // IPoolable.OnSpawn：对象从池取出时调用
    public void OnSpawn()
    {
        gameObject.SetActive(true);
    }

    // IPoolable.OnDespawn：对象归还池时调用
    public void OnDespawn()
    {
        // 停止动画
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }

        // 重置状态
        _text.color = Color.white;
        _text.alpha = 1f;
        _text.fontStyle = TMPro.FontStyles.Normal;
        _rectTransform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// 显示伤害数字（由 DamageNumberManager 调用）
    /// </summary>
    public void Show(float damage, bool isCrit = false, bool isPlayerDamage = false)
    {
        // 设置文本
        int displayDamage = Mathf.FloorToInt(damage);
        if (isPlayerDamage)
        {
            _text.text = $"-{displayDamage}";
        }
        else
        {
            _text.text = displayDamage.ToString();
        }

        if (isPlayerDamage)
        {
            _text.color = _playerDamageColor;
            _text.fontSize = 24f;
        }
        else if (isCrit)
        {
            _text.color = _critColor;
            _text.fontSize = 36f;
            _text.fontStyle = FontStyles.Bold;
        }
        else
        {
            _text.color = _normalColor;
            _text.fontSize = 28f;
            _text.fontStyle = TMPro.FontStyles.Normal;
        }

        // 停止之前的动画
        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        // 开始飘字动画
        _animCoroutine = StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float elapsed = 0f;
        Vector3 startPos = _rectTransform.localPosition;
        Vector3 endPos = startPos + new Vector3(0f, _floatDistance, 0f);
        Color startColor = _text.color;

        while (elapsed < _floatDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _floatDuration;

            _rectTransform.localPosition = Vector3.Lerp(startPos, endPos, t);

            if (t > _fadeStartRatio)
            {
                float fadeT = (t - _fadeStartRatio) / (1f - _fadeStartRatio);
                _text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
            }

            yield return null;
        }

        // 动画结束，归还对象池
        ManagerHub.Pool.Release(gameObject);
    }
}
