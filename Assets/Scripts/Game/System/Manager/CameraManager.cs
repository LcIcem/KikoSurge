using LcIcemFramework.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Event;

/// <summary>
/// 摄像机管理器 -基于CinemachineCamera
/// </summary>
public class CameraManager : SingletonMono<CameraManager>
{
    [SerializeField] private CinemachineCamera _cinemachineCam;

    // 当前跟随目标（Player）的相机冲击 Source
    private CinemachineImpulseSource _recoilSource;
    private CinemachineImpulseSource _hurtSource;
    private CinemachineImpulseSource _dashSource;

    // 鼠标偏移跟随
    private CinemachinePositionComposer _positionComposer;
    private Vector2 _currentMouseOffset;

    [Header("鼠标偏移跟随")]
    public float MinMouseOffsetThreshold = 0.2f;
    public float MaxMouseOffsetMagnitude = 0.5f;
    public float MouseOffsetSmoothSpeed = 9f;

    public Transform Target { get; private set; }

    protected override void Init()
    {
        EventCenter.Instance.Subscribe<WeaponBase>(EventID.ShootPerformed, OnShootPerformed);
        EventCenter.Instance.Subscribe(GameEventID.Camera_TriggerHurt, OnHurt);
        EventCenter.Instance.Subscribe(GameEventID.Camera_TriggerDash, OnDash);

        // 运行时在场景中查找 CinemachineCamera（避免 SerializeField 跨场景失效）
        if (_cinemachineCam == null)
            _cinemachineCam = FindFirstObjectByType<CinemachineCamera>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventCenter.Instance.Unsubscribe<WeaponBase>(EventID.ShootPerformed, OnShootPerformed);
        EventCenter.Instance.Unsubscribe(GameEventID.Camera_TriggerHurt, OnHurt);
        EventCenter.Instance.Unsubscribe(GameEventID.Camera_TriggerDash, OnDash);
    }

    // 设置摄像机跟随
    public void Follow(Transform target)
    {
        Target = target;

        // 保障：如果引用失效，在场景中重新查找
        if (_cinemachineCam == null)
            _cinemachineCam = FindFirstObjectByType<CinemachineCamera>();

        if (_cinemachineCam != null)
        {
            _cinemachineCam.Follow = target;
            _positionComposer = _cinemachineCam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
        }
        else
        {
            Debug.LogWarning("[CameraManager] CinemachineCamera not found in scene.");
            return;
        }

        // 从 Player 获取三个冲击 Source
        var player = target.GetComponent<Player>();
        if (player != null)
        {
            _recoilSource = player.RecoilSource;
            _hurtSource = player.HurtSource;
            _dashSource = player.DashSource;
        }
    }

    /// <summary>
    /// 武器射击时震动（后坐力），方向为射击反方向，强度由 recoilForce 控制
    /// </summary>
    private void OnShootPerformed(WeaponBase weapon)
    {
        // 后坐力方向：射击方向 × recoilForce
        Vector3 recoilDir = (Vector3)weapon.Owner.AimDir * weapon.Config.recoilForce;
        _recoilSource?.GenerateImpulse(recoilDir);
    }

    /// <summary>
    /// 受伤相机震动（无方向，随机）
    /// </summary>
    private void OnHurt()
    {
        _hurtSource?.GenerateImpulse();
    }

    /// <summary>
    /// 冲刺相机震动，方向为冲刺方向
    /// </summary>
    private void OnDash()
    {
        var player = Target.GetComponent<Player>();
        if (player != null)
        {
            Vector3 dashDir = new Vector3(player.MoveDir.x, player.MoveDir.y, 0f);
            _dashSource?.GenerateImpulse(dashDir);
        }
    }

    private void LateUpdate()
    {
        if (Target == null) return;

        if (_positionComposer == null)
            _positionComposer = _cinemachineCam?.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
        if (_positionComposer == null) return;

        // 屏幕空间计算：鼠标位置归一化到 [-0.5, 0.5]，屏幕中心为 0
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 screenCentered = new Vector2(
            screenPos.x / Screen.width - 0.5f,
            screenPos.y / Screen.height - 0.5f
        );

        float dist = screenCentered.magnitude;

        // 死区 + 最大限制
        if (dist < MinMouseOffsetThreshold)
            screenCentered = Vector2.zero;
        else if (dist > MaxMouseOffsetMagnitude)
            screenCentered = screenCentered.normalized * MaxMouseOffsetMagnitude;

        // 转换为世界空间偏移量：screenCentered ∈ [-0.5,0.5] → 世界单位
        float orthoSize = Camera.main.orthographicSize;
        float aspect = (float)Screen.width / Screen.height;
        Vector2 worldOffset = new Vector2(
            screenCentered.x * MaxMouseOffsetMagnitude * 2f * orthoSize * aspect,
            screenCentered.y * MaxMouseOffsetMagnitude * 2f * orthoSize
        );

        _currentMouseOffset = Vector2.Lerp(_currentMouseOffset, worldOffset, MouseOffsetSmoothSpeed * Time.deltaTime);
        _positionComposer.TargetOffset = _currentMouseOffset;
    }
}