using UnityEngine;

/// <summary>
/// 武器抽象基类
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("武器基础配置")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;    
    [SerializeField] private float attackRate = 0.2f;  // 攻击间隔
    [SerializeField] private FirePattern pattern;  // 攻击模式

    protected float attackTimer;   // 攻击间隔计时器
    private bool _canAttack = true;

    protected void Update()
    {
        // 维护攻击计时器
        if (!_canAttack)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackRate;
                _canAttack = true;
            }
        }
    }

    // 根据攻击模式攻击
    public void Attack()
    {
        if (_canAttack)
        {
            pattern.Fire(_projectilePrefab, _firePoint);
            _canAttack = false;
        }         
    }
}
