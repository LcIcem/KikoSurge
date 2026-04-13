namespace LcIcemFramework.Core
{
    /// <summary>
    /// 事件 ID 枚举
    /// </summary>
    public enum EventID
    {
        // Audio
        PlayBGM,
        PlaySFX,

        // UI
        UpdateHeartDisplay,
        PlayerIsDead,
        RestartGame,

        // Input
        ShootPerformed,

        // Combat
        Combat_PlayerDamaged,
        Combat_BulletSpawned,
        Combat_BulletHit,
        Combat_EnemyDamaged,
        Combat_EnemyKilled,
        Combat_EnemyAttack,
        Combat_EnemySpawned,
        Combat_WaveStart,
        Combat_WaveComplete,
        Combat_AllWavesComplete,
        Combat_Reloading,
        Combat_Reloaded,
        Combat_CancelReload,

    }
}
