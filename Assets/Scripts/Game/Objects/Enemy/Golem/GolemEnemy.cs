using UnityEngine;
using LcIcemFramework;

/// <summary>
/// Golem 敌人：朝锁定方向发射子弹攻击
/// </summary>
public class GolemEnemy : EnemyBase
{
    [Header("Golem 攻击参数")]
    [SerializeField] private BulletConfig _bulletConfig;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private float _bulletSpread = 5f;

    /// <summary>
    /// 发射子弹（由攻击状态在 AttackHitTime 调用）
    /// </summary>
    public void FireBullet()
    {
        if (_bulletConfig == null || _muzzle == null) return;

        Vector3 spawnPos = _muzzle.position;
        Vector3 direction = _attackDirection;

        // 计算带散布的方向
        float spreadRad = Random.Range(-_bulletSpread, _bulletSpread) * Mathf.Deg2Rad;
        float totalAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spreadRad;
        Vector3 finalDir = new Vector3(Mathf.Cos(totalAngle * Mathf.Deg2Rad), Mathf.Sin(totalAngle * Mathf.Deg2Rad), 0f);

        // 创建子弹（伤害使用敌人的 Attack 属性）
        BulletModule.SpawnEnemyBullet(_bulletConfig, spawnPos, finalDir, Mathf.RoundToInt(Attack));
    }

    /// <summary>
    /// Golem 攻击命中处理：发射子弹代替直接伤害
    /// </summary>
    protected override void HandleAttackHit()
    {
        FireBullet();
    }
}
