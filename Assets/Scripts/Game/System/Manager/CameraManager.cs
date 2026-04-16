using LcIcemFramework.Core;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 摄像机管理器 -基于CinemachineCamera
/// </summary>
public class CameraManager : SingletonMono<CameraManager>
{
    [SerializeField] private CinemachineCamera _cinemachineCam;

    private CinemachineImpulseSource impulseSrc;
    public Transform Target { get; private set; }

    protected override void Init()
    {
        EventCenter.Instance.Subscribe<WeaponBase>(EventID.ShootPerformed, ScreenShake);

        // 运行时在场景中查找 CinemachineCamera（避免 SerializeField 跨场景失效）
        if (_cinemachineCam == null)
            _cinemachineCam = FindFirstObjectByType<CinemachineCamera>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventCenter.Instance.Unsubscribe<WeaponBase>(EventID.ShootPerformed, ScreenShake);
    }

    // 设置摄像机跟随
    public void Follow(Transform target)
    {
        Target = target;

        // 保障：如果引用失效，在场景中重新查找
        if (_cinemachineCam == null)
            _cinemachineCam = FindFirstObjectByType<CinemachineCamera>();

        if (_cinemachineCam != null)
            _cinemachineCam.Follow = target;
        else
            Debug.LogWarning("[CameraManager] CinemachineCamera not found in scene.");
    }

    public void ScreenShake(WeaponBase weapon)
    {
        impulseSrc = Target.gameObject.GetComponent<CinemachineImpulseSource>();
        if (impulseSrc != null)
        {
            impulseSrc.GenerateImpulse(weapon.Config.recoilForce);
        }
    }

}