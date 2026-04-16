using UnityEngine;
using LcIcemFramework;
using System.Collections;

/// <summary>
/// 运行时掉落物实体：挂在到场景中的掉落物品
/// 实现 IPoolable 支持对象池
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class LootItem : MonoBehaviour, IPoolable
{
    [SerializeField] private SortingLayer _sortingLayer;

    [Header("拾取配置")]
    [Tooltip("掉落后可拾取的延迟时间（秒）")]
    [SerializeField] private float _pickupDelay = 0.3f;

    [Header("闪烁效果")]
    [Tooltip("闪烁速度（每秒切换次数）")]
    [SerializeField] private float _blinkSpeed = 4f;

    [Tooltip("正常状态颜色（留空则使用精灵原本颜色）")]
    [SerializeField] private Color _normalColor = Color.white;

    [Tooltip("高亮状态叠加色")]
    [SerializeField] private Color _highlightColor = new Color(1f, 0.95f, 0.7f);

    [Tooltip("高亮色插值权重（0-1），值越高高亮越强")]
    [Range(0f, 1f)]
    [SerializeField] private float _highlightBlend = 0.6f;

    [Header("浮动效果")]
    [Tooltip("浮动速度（每秒完整周期次数）")]
    [SerializeField] private float _floatSpeed = 1f;

    [Tooltip("浮动幅度（上下偏移量）")]
    [SerializeField] private float _floatAmplitude = 0.1f;

    private Coroutine _blinkCoroutine;
    private Coroutine _floatCoroutine;
    private Vector3 _basePosition;

    // 物品定义（配置）
    public LootItemDefBase ItemDef { get; private set; }

    // 数量
    public int Quantity { get; private set; }

    // 保存的初始化参数（用于池化后重置）
    private LootItemDefBase _savedItemDef;
    private int _savedQuantity;

    // 视觉组件
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _collider;
    private bool _canPickup;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// 初始化掉落物
    /// </summary>
    public void Initialize(LootItemDefBase itemDef, int quantity)
    {
        _savedItemDef = itemDef;
        _savedQuantity = quantity;
        ItemDef = itemDef;
        Quantity = quantity;
        _canPickup = false;

        // 记录初始位置（浮动基准）
        _basePosition = transform.position;

        // 设置视觉
        if (_spriteRenderer != null && itemDef.Icon != null)
        {
            _spriteRenderer.sprite = itemDef.Icon;

            // 根据 sprite 尺寸调整碰撞体大小
            if (_collider != null)
            {
                Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;
                _collider.size = spriteSize;
                _collider.offset = Vector2.zero;
            }
        }

        // 禁用碰撞体，等待延迟后启用
        if (_collider != null)
        {
            _collider.enabled = false;
        }

        // 延迟启用拾取
        StartCoroutine(EnablePickupAfterDelay());
    }

    private IEnumerator EnablePickupAfterDelay()
    {
        yield return new WaitForSeconds(_pickupDelay);
        if (_collider != null)
        {
            _collider.enabled = true;
        }
        _canPickup = true;

        // 开始浮动效果
        _floatCoroutine = StartCoroutine(FloatEffect());
        // 开始闪烁效果
        _blinkCoroutine = StartCoroutine(BlinkEffect());
    }

    private IEnumerator FloatEffect()
    {
        // 防零保护
        if (_floatSpeed <= 0f) yield break;

        while (true)
        {
            // 向上
            float t = 0f;
            float period = 1f / _floatSpeed;
            while (t < period * 0.5f)
            {
                float offset = Mathf.Lerp(0f, _floatAmplitude, t / (period * 0.5f));
                transform.position = _basePosition + Vector3.up * offset;
                t += Time.deltaTime;
                yield return null;
            }

            // 向下
            t = 0f;
            while (t < period * 0.5f)
            {
                float offset = Mathf.Lerp(_floatAmplitude, 0f, t / (period * 0.5f));
                transform.position = _basePosition + Vector3.up * offset;
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator BlinkEffect()
    {
        // 防零保护
        if (_blinkSpeed <= 0f) yield break;

        // 使用配置的正常色（如果为白色则使用精灵原本颜色）
        Color baseColor = _normalColor == Color.white ? _spriteRenderer.color : _normalColor;
        float period = 1f / _blinkSpeed;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;
            // 使用 PingPong 实现平滑的颜色插值（0 -> 1 -> 0 -> 1 ...）
            float t = Mathf.PingPong(elapsed / period, 1f);
            // 在 baseColor 和 highlightColor 之间平滑插值
            _spriteRenderer.color = Color.Lerp(baseColor, _highlightColor, t * _highlightBlend);
            yield return null;
        }
    }

    // IPoolable: 对象从池中取出时调用
    public void OnSpawn()
    {
        // 重新初始化（使用保存的参数）
        if (_savedItemDef != null)
        {
            Initialize(_savedItemDef, _savedQuantity);
        }

        StopAllCoroutines();
        _canPickup = false;
        _blinkCoroutine = null;
        _floatCoroutine = null;

        // 重置位置
        transform.position = _basePosition;

        // 重置颜色
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
        }
    }

    // IPoolable: 对象归还池时调用
    public void OnDespawn()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
        if (_floatCoroutine != null)
        {
            StopCoroutine(_floatCoroutine);
            _floatCoroutine = null;
        }
        // 重置位置
        transform.position = _basePosition;
        ItemDef = null;
        Quantity = 0;
        _canPickup = false;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sprite = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_canPickup) return;
        if (other.CompareTag("Player"))
        {
            Pickup();
        }
    }

    /// <summary>
    /// 拾取掉落物
    /// </summary>
    private void Pickup()
    {
        if (ItemDef == null) return;

        // 处理不同类型物品的拾取逻辑
        switch (ItemDef.ItemType)
        {
            case LootItemType.Weapon:
                HandleWeaponPickup();
                break;

            case LootItemType.Prop:
                HandlePropPickup();
                break;

            case LootItemType.Gold:
                HandleGoldPickup();
                break;
        }

        // 归还对象池
        ManagerHub.Pool.Release(gameObject);
    }

    private void HandleWeaponPickup()
    {
        if (ItemDef is WeaponLootItemDef_SO weaponLoot && weaponLoot.gunConfig != null)
        {
            // 通过 LootManager 创建武器并添加给玩家
            LootManager.Instance?.CreateWeaponForPlayer(weaponLoot.gunConfig);
        }
    }

    private void HandlePropPickup()
    {
        // TODO: 道具系统实现后扩展
        Debug.Log($"[LootItem] 拾取道具: {ItemDef.ItemName} x{Quantity}");
    }

    private void HandleGoldPickup()
    {
        // TODO: 金币系统实现后扩展
        Debug.Log($"[LootItem] 拾取金币: {Quantity}");
    }
}
