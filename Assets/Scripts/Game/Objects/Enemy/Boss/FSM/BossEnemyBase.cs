using UnityEngine;

public class BossEnemyBase : EnemyBase
{
    public float DashSpeed { get; protected set; }
    public float DashDuration { get; protected set; }
    public float DashCooldown { get; protected set; }
    public float StunDuration { get; protected set; }
    public float BirthStaggerDuration { get; protected set; }

    /// <summary>出生硬直是否结束</summary>
    public bool IsInBirthStagger => _birthStaggerTimer > 0f;

    [Header("Boss音效")]
    [SerializeField] private AudioClip _bossDashSFX;

    /// <summary>Boss冲刺音效</summary>
    public AudioClip BossDashSFX => _bossDashSFX;

    /// <summary>
    /// 供 BossDashState 访问 Rigidbody2D
    /// </summary>
    public Rigidbody2D DashRigidbody => _rigidbody;

    private float _birthStaggerTimer;

    public float BirthStaggerTimerDebug => _birthStaggerTimer;

    protected override void Awake()
    {
        base.Awake();
        _fsm = new BossFSM(this, _animator);
    }

    public override void Init(EnemyConfig config)
    {
        base.Init(config);

        if (config is BossEnemyConfig bossConfig)
        {
            DashSpeed = bossConfig.DashSpeed;
            DashDuration = bossConfig.DashDuration;
            DashCooldown = bossConfig.DashCooldown;
            StunDuration = bossConfig.StunDuration;
            BirthStaggerDuration = bossConfig.BirthStaggerDuration;
        }
        else
        {
            BirthStaggerDuration = 1.5f; // 默认值
        }

        // 初始化出生硬直计时器
        _birthStaggerTimer = BirthStaggerDuration;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        _birthStaggerTimer = 0f;
    }

    /// <summary>
    /// 每帧递减出生硬直计时器
    /// </summary>
    public void TickBirthStagger()
    {
        if (_birthStaggerTimer > 0f)
        {
            _birthStaggerTimer -= Time.deltaTime;
        }
    }
}
