using UnityEngine;
using LcIcemFramework.Managers;
using UnityEngine.InputSystem;

/// <summary>
/// 武器逻辑，处理开火和子弹创建。
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("子弹配置")]
    [SerializeField] private string _bulletAddress = "Bullet";
    [SerializeField] private float _fireRate = 0.2f;
    [SerializeField] private Transform _spawnPoint;

    // 调试
    private PlayerInput _playerInput;
    private InputAction _fireAction;

    // 开火计时器
    private float _fireTimer;

    void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        _fireAction = _playerInput.actions["Fire"];
    }

    private void Update()
    {
        _fireTimer -= Time.deltaTime;

        if (_fireAction.ReadValue<float>() > 0f && _fireTimer <= 0f)
        {
            TryFire();
            _fireTimer = _fireRate;
        }
    }

    private void TryFire()
    {
        // 子弹方向由 SpawnPoint 的旋转角度决定
        Vector3 spawnPos = _spawnPoint.position;
        Quaternion spawnRot = _spawnPoint.rotation;

        var bullet = ManagerHub.Pool.Get<Bullet>(_bulletAddress, spawnPos, spawnRot);
        if (bullet == null) return;
    }
}
