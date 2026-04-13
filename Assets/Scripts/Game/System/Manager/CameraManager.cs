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
        EventCenter.Instance.Subscribe(EventID.ShootPerformed, () => ScreenShake());
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventCenter.Instance.Unsubscribe(EventID.ShootPerformed, () => ScreenShake());
    }

    // 设置摄像机跟随
    public void Follow(Transform target)
    {
        Target = target;
        _cinemachineCam.Follow = target;
    }

    public void ScreenShake()
    {
        impulseSrc = Target.gameObject.GetComponent<CinemachineImpulseSource>();
        if (impulseSrc != null)
        {
            impulseSrc.GenerateImpulse();
        }
    }

}