using UnityEngine;

public class BossEnemyBase : EnemyBase
{
    public float DashSpeed { get; protected set; }
    public float DashDuration { get; protected set; }
    public float DashCooldown { get; protected set; }
    public float StunDuration { get; protected set; }

    [Header("Boss音效")]
    [SerializeField] private AudioClip _bossDashSFX;

    /// <summary>Boss冲刺音效</summary>
    public AudioClip BossDashSFX => _bossDashSFX;

    /// <summary>
    /// 供 BossDashState 访问 Rigidbody2D
    /// </summary>
    public Rigidbody2D DashRigidbody => _rigidbody;

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
        }
    }
}
