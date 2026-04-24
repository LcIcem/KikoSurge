using UnityEngine;

/// <summary>
/// Mushroom 敌人：朝自身前方扇形区域进行范围检测攻击。
/// 使用 GruntEnemyConfig 配置，在预设体上配置扇形参数。
/// </summary>
public class Mushroom : EnemyBase
{
    [Header("扇形攻击参数（预设体配置）")]
    [SerializeField] private float _fanAngle = 60f;   // 扇形总角度
    [SerializeField] private float _fanRange = 3f;    // 扇形半径

    [Header("扇形绘制")]
    [SerializeField] private LineRenderer _fanRenderer;
    [SerializeField] private int _fanSegments = 20;
    [SerializeField] private Color _fanColor = new Color(1f, 0.8f, 0f, 0.3f);
    [SerializeField] private float _fanLineWidth = 0.05f;

    [Header("扇形持续时间")]
    [SerializeField] private float _fanDisplayDuration = 0.3f;

    // 扇形绘制计时器
    private float _fanDisplayTimer = 0f;

    private new void Awake()
    {
        base.Awake();  // 确保基类的 Awake 被调用，初始化 _rigidbody 等组件

        if (_fanRenderer == null)
        {
            _fanRenderer = GetComponent<LineRenderer>();
        }

        if (_fanRenderer != null)
        {
            SetupLineRenderer();
        }
    }

    private new void OnEnable()
    {
        _fanDisplayTimer = 0f;
        if (_fanRenderer != null)
        {
            _fanRenderer.enabled = false;
        }
    }

    private new void OnDisable()
    {
        _fanDisplayTimer = 0f;
        if (_fanRenderer != null)
        {
            _fanRenderer.enabled = false;
        }
    }

    private void SetupLineRenderer()
    {
        // 扇形点位：中心(1) + 左边界(1) + 弧线(segments) + 右边界(1) + 闭合(1) = segments + 4
        _fanRenderer.positionCount = _fanSegments + 4;
        _fanRenderer.useWorldSpace = true;
        _fanRenderer.startColor = _fanColor;
        _fanRenderer.endColor = _fanColor;
        _fanRenderer.startWidth = _fanLineWidth;
        _fanRenderer.endWidth = _fanLineWidth;
    }

    private new void Update()
    {
        base.Update();

        // 更新扇形显示
        if (_fanDisplayTimer > 0f && _fanRenderer != null)
        {
            _fanDisplayTimer -= Time.deltaTime;

            DrawFanShape();
            _fanRenderer.enabled = true;

            if (_fanDisplayTimer <= 0f)
            {
                _fanRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// 扇形攻击命中检测：检测玩家是否在扇形范围内
    /// </summary>
    protected override bool CheckAttackHit()
    {
        if (_player == null) return false;

        // 显示扇形（使用开始攻击时锁定的 _attackDirection）
        _fanDisplayTimer = _fanDisplayDuration;

        // 1. 先检查距离是否在扇形半径内
        if (DistanceToPlayer >= _fanRange) return false;

        // 2. 计算玩家是否在扇形范围内（使用锁定的攻击方向）
        Vector2 toPlayer = (_player.position - transform.position).normalized;
        float angle = Vector2.Angle(_attackDirection, toPlayer);


        return angle < _fanAngle * 0.5f;
    }

    /// <summary>
    /// 绘制扇形形状
    /// 点位分配：0=中心, 1=左边界, 2~segments+1=弧线(含右端点), segments+2=右边界, segments+3=闭合到左边界
    /// </summary>
    private void DrawFanShape()
    {
        // 安全检查：确保 _attackDirection 有效
        if (_attackDirection.sqrMagnitude < 0.01f)
        {
            _attackDirection = Vector2.right;
        }

        float halfAngle = _fanAngle * 0.5f;
        Vector3 center = transform.position;

    }
}
