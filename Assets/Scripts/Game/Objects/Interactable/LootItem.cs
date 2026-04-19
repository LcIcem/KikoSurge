using UnityEngine;
using LcIcemFramework;
using System.Collections;
using System.Text;

/// <summary>
/// 运行时掉落物实体：挂在到场景中的掉落物品
/// 实现 IPoolable 支持对象池
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
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

    // 物品定义（运行时传入）
    public ItemConfig ItemDef { get; private set; }

    // 数量
    public int Quantity { get; private set; }

    // 保存的初始化参数（用于池化后重置）
    private ItemConfig _savedItemDef;
    private int _savedQuantity;

    // 视觉组件
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _collider;
    [SerializeField] private Transform _visualRoot;
    private bool _canPickup;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _spriteRenderer = _visualRoot?.GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 初始化掉落物
    /// </summary>
    public void Initialize(ItemConfig itemDef, int quantity)
    {
        _savedItemDef = itemDef;
        _savedQuantity = quantity;
        ItemDef = itemDef;
        Quantity = quantity;
        _canPickup = false;

        // 记录初始位置（浮动基准）
        _basePosition = transform.position;

        // 设置视觉
        if (_spriteRenderer != null && itemDef != null && itemDef.Icon != null)
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

        // 根据物品类型设置交互模式
        SetupInteractionMode();

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
        if (_floatSpeed <= 0f || _visualRoot == null) yield break;

        while (true)
        {
            float t = 0f;
            float period = 1f / _floatSpeed;
            while (t < period * 0.5f)
            {
                float offset = Mathf.Lerp(0f, _floatAmplitude, t / (period * 0.5f));
                _visualRoot.localPosition = Vector3.up * offset;
                t += Time.deltaTime;
                yield return null;
            }

            t = 0f;
            while (t < period * 0.5f)
            {
                float offset = Mathf.Lerp(_floatAmplitude, 0f, t / (period * 0.5f));
                _visualRoot.localPosition = Vector3.up * offset;
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator BlinkEffect()
    {
        if (_blinkSpeed <= 0f) yield break;

        Color baseColor = _normalColor == Color.white ? _spriteRenderer.color : _normalColor;
        float period = 1f / _blinkSpeed;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed / period, 1f);
            _spriteRenderer.color = Color.Lerp(baseColor, _highlightColor, t * _highlightBlend);
            yield return null;
        }
    }

    // IPoolable: 对象从池中取出时调用
    public void OnSpawn()
    {
        if (_savedItemDef != null)
        {
            Initialize(_savedItemDef, _savedQuantity);
        }

        StopAllCoroutines();
        _canPickup = false;
        _blinkCoroutine = null;
        _floatCoroutine = null;

        transform.position = _basePosition;

        if (_visualRoot != null)
            _visualRoot.localPosition = Vector3.zero;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
        }
    }

    // IPoolable: 对象归还池时调用
    public void OnDespawn()
    {
        // 取消订阅交互事件
        if (_interactable != null)
        {
            _interactable.OnInteract -= OnInteractTriggered;
        }

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
        transform.position = _basePosition;

        if (_visualRoot != null)
            _visualRoot.localPosition = Vector3.zero;

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
        if (!other.CompareTag("Player")) return;

        // 武器类型需要按E交互，不自动拾取
        if (ItemDef?.Type == ItemType.Weapon) return;

        Pickup();
    }

    private void Pickup()
    {
        if (ItemDef == null) return;

        // 播放拾取音效
        PlayPickupSFX();

        switch (ItemDef.Type)
        {
            case ItemType.Weapon:
                HandleWeaponPickup();
                break;
            case ItemType.Currency:
                HandleCurrencyPickup();
                break;
            case ItemType.Potion:
            case ItemType.Ammo:
                HandleConsumablePickup();
                break;
        }

        ManagerHub.Pool.Release(gameObject);
    }

    /// <summary>
    /// 播放拾取音效
    /// </summary>
    private void PlayPickupSFX()
    {
        if (ItemDef?.PickupSFX != null)
        {
            ManagerHub.Audio.PlaySFX(ItemDef.PickupSFX);
        }
    }

    private Interactable _interactable;

    /// <summary>
    /// 根据物品类型设置交互模式
    /// </summary>
    private void SetupInteractionMode()
    {
        switch (ItemDef?.Type)
        {
            case ItemType.Weapon:
                // 武器类型需要按E交互
                SetupAsInteractable();
                break;
            case ItemType.Currency:
                // 货币自动拾取
                SetupAsAutoPickup();
                break;
            default:
                // 其他类型自动拾取
                SetupAsAutoPickup();
                break;
        }
    }

    /// <summary>
    /// 设置为可交互模式（需要按E拾取）
    /// </summary>
    private void SetupAsInteractable()
    {
        // 直接获取 Prefab 上已有的 Interactable 组件，不要销毁重建
        // 否则会丢失 Prefab 上配置的 UI 引用（_interactionHintUI 等）
        _interactable = gameObject.GetComponent<Interactable>();
        if (_interactable == null)
        {
            _interactable = gameObject.AddComponent<Interactable>();
        }

        // 重置交互状态（复用时需要）
        _interactable.ResetInteractionState();

        // 取消订阅，防止重复订阅
        _interactable.OnInteract -= OnInteractTriggered;

        // 设置交互提示文本（{0} 会被替换为实际按键）
        _interactable.SetHintText($"按[{{0}}]拾取");

        // 设置物品信息卡片内容（显示由 Interactable.OnTriggerEnter 控制）
        string title = $"<B>{ItemDef?.Name ?? "Unknown"}</B>";
        string description = BuildItemDescription();
        _interactable.SetInfoCardContent(title, description);

        // 订阅交互事件（交互后卡片会自动隐藏）
        _interactable.OnInteract += OnInteractTriggered;
    }

    /// <summary>
    /// 构建物品描述信息
    /// </summary>
    private string BuildItemDescription()
    {
        if (ItemDef == null)
            return "No description available.";

        switch (ItemDef.Type)
        {
            case ItemType.Weapon:
                var weaponConfig = GameDataManager.Instance?.GetWeaponConfig(ItemDef.Id);
                if (weaponConfig != null)
                {
                    return BuildWeaponDescription(weaponConfig);
                }
                return ItemDef.Description;

            default:
                return ItemDef.Description;
        }
    }

    /// <summary>
    /// 构建武器描述信息
    /// </summary>
    private string BuildWeaponDescription(WeaponConfig weaponConfig)
    {
        var sb = new StringBuilder();

        // 显示武器属性
        sb.AppendLine($"射速: {1f / weaponConfig.fireRate:F1}/秒");
        sb.AppendLine($"弹夹: {weaponConfig.magazineSize} 发");

        // 霰弹模式显示弹丸数量
        if (weaponConfig.fireMode == FireMode.Spread && weaponConfig.bulletCount > 1)
            sb.AppendLine($"弹丸: {weaponConfig.bulletCount}");

        // 连发模式显示连发数量
        if (weaponConfig.fireMode == FireMode.Burst)
            sb.AppendLine($"连发: {weaponConfig.burstCount} 发");

        // 显示散布角度
        if (weaponConfig.randomSpreadAngle > 0)
            sb.AppendLine($"散布: {weaponConfig.randomSpreadAngle}°");

        sb.AppendLine($"换弹: {weaponConfig.reloadTime:F1}秒");

        sb.AppendLine();
        if (!string.IsNullOrEmpty(ItemDef.Description))
        {
            sb.AppendLine(ItemDef.Description);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 设置为自动拾取模式
    /// </summary>
    private void SetupAsAutoPickup()
    {
        // 保持碰撞体自动拾取逻辑（在 OnTriggerEnter2D 中处理）
    }

    /// <summary>
    /// 交互触发时调用
    /// </summary>
    private void OnInteractTriggered()
    {
        // 结束交互状态
        Player.EndInteraction();
        Pickup();
    }

    private void HandleWeaponPickup()
    {
        var weaponConfig = GameDataManager.Instance?.GetWeaponConfig(ItemDef.Id);
        if (weaponConfig == null) return;

        // 获取玩家
        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
        if (player == null) return;

        WeaponFactory.Instance.Create(weaponConfig, player.WeaponPivot, (weapon) =>
        {
            if (weapon == null) return;

            // 获取当前已装备武器列表和最大数量
            var equippedWeaponIds = SessionManager.Instance.GetEquippedWeaponIds();
            var roleData = GameDataManager.Instance?.GetRoleStaticData(SessionManager.Instance.CurrentSession?.selectedRoleId ?? 0);
            int maxSlots = roleData?.maxWeaponSlots ?? 2;

            if (equippedWeaponIds.Count < maxSlots)
            {
                // 装备栏未满，装备武器
                player.weaponHandler.AddWeapon(weapon);
                equippedWeaponIds.Add(weaponConfig.itemConfig.Id);
                SessionManager.Instance.SetEquippedWeaponIds(equippedWeaponIds);
            }
            else
            {
                // 装备栏已满，放入背包
                var inventoryWeaponIds = SessionManager.Instance.GetInventoryWeaponIds();
                inventoryWeaponIds.Add(weaponConfig.itemConfig.Id);
                SessionManager.Instance.SetInventoryWeaponIds(inventoryWeaponIds);
                WeaponFactory.Instance.Release(weapon);
            }
        });
    }

    private void HandleCurrencyPickup()
    {
        if (ItemDef == null) return;

        // 添加到背包（触发 OnInventoryChanged 事件，UI 自动刷新）
        InventoryManager.Instance?.AddItem(ItemType.Currency, ItemDef.Id, Quantity);
    }

    private void HandleConsumablePickup()
    {
        // 消耗品拾取逻辑（待实现）
    }
}
