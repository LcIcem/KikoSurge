using System.Collections.Generic;
using LcIcemFramework.Core;
using UnityEngine;

/// <summary>
/// 波次管理器：控制敌人生成节奏、难度曲线。
/// </summary>
public class WaveManager : SingletonMono<WaveManager>
{
    // 当前波次
    public int CurrentWave { get; private set; }
    public int TotalWaves => _waveConfigs.Count;

    [SerializeField] private List<WaveConfig> _waveConfigs = new();

    // 生成控制
    private float _spawnTimer;
    private Queue<EnemySpawnParams> _spawnQueue = new();
    private bool _isWaveActive;

    protected override void Init()
    {
        LoadWaveConfigs();
    }

    // TODO
    private void LoadWaveConfigs()
    {
        // 从 ScriptableObject 加载波次配置
        // var data = Resources.Load<WaveData_SO>("Config/WaveData");
        // _waveConfigs = data.Waves;
    }

    public void StartWave(int waveNum)
    {
        if (waveNum < 0 || waveNum >= _waveConfigs.Count)
        {
            Debug.LogError($"[WaveManager] 无效波次: {waveNum}");
            return;
        }

        CurrentWave = waveNum;
        _spawnQueue.Clear();

        // 根据波次生成敌人生成队列
        GenerateSpawnQueue(_waveConfigs[waveNum]);

        _isWaveActive = true;
        _spawnTimer = 0f;

        EventCenter.Instance.Publish(EventID.Combat_WaveStart,
            new WaveStartParams { waveNum = waveNum, totalEnemies = _spawnQueue.Count });
    }

    private void Update()
    {
        if (!_isWaveActive) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f && _spawnQueue.Count > 0)
        {
            SpawnNext();
            _spawnTimer = GetCurrentWaveConfig().SpawnInterval;
        }

        // 波次完成检测
        if (_spawnQueue.Count == 0 && AreAllEnemiesDefeated())
        {
            CompleteWave();
        }
    }

    private WaveConfig GetCurrentWaveConfig()
    {
        if (CurrentWave < _waveConfigs.Count)
            return _waveConfigs[CurrentWave];
        return _waveConfigs[^1];
    }

    /// 根据难度曲线生成敌人生成队列
    private void GenerateSpawnQueue(WaveConfig config)
    {
        float difficultyMultiplier = 1f + CurrentWave * 0.15f; // 每波 +15% 难度

        for (int i = 0; i < config.GruntCount; i++)
        {
            _spawnQueue.Enqueue(new EnemySpawnParams
            {
                Type = EnemyType.Grunt,
                PrefabName = "GruntEnemy",
                Position = GetRandomSpawnPoint(),
                Config = new EnemyConfig
                {
                    MaxHP = 30f * difficultyMultiplier,
                    MoveSpeed = 3f,
                    Attack = 5f * difficultyMultiplier,
                    DetectRange = 8f,
                    AttackRange = 1.5f,
                    LoseRange = 12f
                }
            });
        }

        if (CurrentWave >= 3)
        {
            for (int i = 0; i < config.EliteCount; i++)
            {
                _spawnQueue.Enqueue(new EnemySpawnParams
                {
                    Type = EnemyType.Elite,
                    PrefabName = "EliteEnemy",
                    Position = GetRandomSpawnPoint(),
                    Config = new EnemyConfig
                    {
                        MaxHP = 80f * difficultyMultiplier,
                        MoveSpeed = 2.5f,
                        Attack = 10f * difficultyMultiplier,
                        DetectRange = 10f,
                        AttackRange = 2f,
                        LoseRange = 15f
                    }
                });
            }
        }

        if (CurrentWave >= 5 && CurrentWave % 5 == 0)
        {
            // 每5波出现一个 Boss
            _spawnQueue.Enqueue(new EnemySpawnParams
            {
                Type = EnemyType.Boss,
                PrefabName = "BossEnemy",
                Position = GetBossSpawnPoint(),
                Config = new EnemyConfig
                {
                    MaxHP = 500f * difficultyMultiplier,
                    MoveSpeed = 2f,
                    Attack = 20f * difficultyMultiplier,
                    DetectRange = 15f,
                    AttackRange = 2f,
                    LoseRange = 20f
                }
            });
        }
    }

    private void SpawnNext()
    {
        if (_spawnQueue.Count == 0) return;

        var p = _spawnQueue.Dequeue();
        EnemyFactory.Instance.Create(p);
    }

    private bool AreAllEnemiesDefeated()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length == 0;
    }

    private void CompleteWave()
    {
        _isWaveActive = false;

        EventCenter.Instance.Publish(EventID.Combat_WaveComplete,
            new WaveCompleteParams { waveNum = CurrentWave });

        if (CurrentWave + 1 < TotalWaves)
        {
            // 自动开始下一波（可加延迟）
            StartWave(CurrentWave + 1);
        }
        else
        {
            // 全部波次完成
            EventCenter.Instance.Publish(EventID.Combat_AllWavesComplete);
        }
    }

    private Vector3 GetRandomSpawnPoint()
    {
        // 从屏幕边缘随机生成点
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(15f, 20f);
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
    }

    private Vector3 GetBossSpawnPoint()
    {
        // Boss 从固定位置生成
        return new Vector3(0f, 10f, 0f);
    }
}