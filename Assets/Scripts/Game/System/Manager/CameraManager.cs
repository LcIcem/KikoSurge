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
    public float MinMouseOffsetThreshold = 0.5f;   // 死区阈值（屏幕空间 0~0.707）：鼠标距屏幕中心小于此值时偏移归零
    public float MaxMouseOffsetMagnitude = 0.7f;   // 最大范围（屏幕空间 0~0.707）：鼠标超出此半径时方向向量钳制到此半径
    public float MouseOffsetWorldScale = 1f;        // 世界空间缩放系数：乘以视锥体尺寸，控制最大偏移时的世界坐标倍率
    public float MouseOffsetSmoothSpeed = 8f;       // 平滑速度：值越大从当前位置过渡到目标偏移越快

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

        // 第一步：屏幕空间计算
        // 将鼠标像素坐标 (0~Screen.width, 0~Screen.height) 归一化到以屏幕中心为原点的 [-0.5, 0.5] 范围
        // 屏幕中心=(0,0)，左边缘=(-0.5,0)，右边缘=(0.5,0)，下边缘=(0,-0.5)，上边缘=(0,0.5)
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 screenCentered = new Vector2(
            screenPos.x / Screen.width - 0.5f,
            screenPos.y / Screen.height - 0.5f
        );

        float dist = screenCentered.magnitude;

        // 第二步：死区 + 屏幕空间钳制
        // 鼠标距屏幕中心 < 死区阈值时归零，避免相机在鼠标靠近中心时频繁抖动
        // 鼠标距屏幕中心 > 最大范围时，将方向向量钳制到最大半径内
        if (dist < MinMouseOffsetThreshold)
            screenCentered = Vector2.zero;
        else if (dist > MaxMouseOffsetMagnitude)
            screenCentered = screenCentered.normalized * MaxMouseOffsetMagnitude;

        // 第三步：转换为世界空间偏移量
        // 正交相机视锥体：X方向总宽度 = orthoSize × aspect × 2，Y方向总高度 = orthoSize × 2
        // screenCentered ∈ [-0.5, 0.5] → screenCentered × 2 ∈ [-1, 1]（全屏比例）
        // 乘以视锥体总尺寸得到世界坐标，再乘以 MouseOffsetWorldScale 控制整体强度
        float orthoSize = Camera.main.orthographicSize;
        float aspect = (float)Screen.width / Screen.height;
        Vector2 worldOffset = new Vector2(
            screenCentered.x * 2f * orthoSize * aspect * MouseOffsetWorldScale,
            screenCentered.y * 2f * orthoSize * MouseOffsetWorldScale
        );

        // 第四步：平滑插值，避免偏移量瞬间跳变导致相机抖动
        _currentMouseOffset = Vector2.Lerp(_currentMouseOffset, worldOffset, MouseOffsetSmoothSpeed * Time.deltaTime);

        // 第五步：写入 CinemachinePositionComposer.TargetOffset，叠加到相机跟随点上
        _positionComposer.TargetOffset = _currentMouseOffset;
    }
}