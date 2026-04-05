using UnityEngine;

/// <summary>
/// 相机控制器
/// 挂载在相机 GameObject 上，随场景一起销毁/重建
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("跟随")]
    public Transform target; // 跟随目标
    public float SmoothSpeed = Constants.CAMERA_SMOOTH_SPEED; // 平滑速度，越大跟随越紧
    public Vector3 Offset = new Vector3(0, 0, Constants.CAMERA_OFFSET_Z); // 相对目标偏移，Z=-10

    [Header("冲击")]
    public CameraImpactEffect impactEff;

    [Header("螺旋")]
    public CameraSpiralEffect spiralEff;

    [Header("推进")]
    public CameraZoomEffect zoomEff;

    [Header("调试")]
    public bool ShowDebug = true; // 是否绘制调试图形
    private Vector3 _desiredPosition; // desired 位置，供 Gizmos 绘制


    void Start()
    {
        impactEff = new CameraImpactEffect(this.transform);
        spiralEff = new CameraSpiralEffect(this.transform);
        zoomEff = new CameraZoomEffect(this.transform);
    }

    // 使用 LateUpdate 而非 Update：确保在角色移动之后再移动相机，避免抖动
    private void LateUpdate()
    {
        if (target == null) return;

        impactEff.UpdateEff(); // 更新冲击状态
        spiralEff.UpdateEff(); // 更新螺旋状态
        zoomEff.UpdateEff();   // 更新推进状态

        // 合成所有偏移
        Vector3 totalOffset = Offset + impactEff.GetOffset() + spiralEff.GetOffset() + zoomEff.GetOffset();
        // 计算 desired 位置
        _desiredPosition = target.position + totalOffset;
        // 平滑跟随
        transform.position = Vector3.Lerp(transform.position, _desiredPosition, SmoothSpeed * Time.deltaTime);
    }

    #region 公开 API
    /// <summary>
    /// 触发冲击效果：随机方向偏移后自然衰减
    /// </summary>
    /// <param name="intensity">冲击强度（0~1）</param>
    public void ImpactRandom(float intensity = 0.5f)
    {
        impactEff.Trigger(intensity);
    }

    /// <summary>
    /// 触发冲击效果：指定方向偏移后自然衰减
    /// </summary>
    /// <param name="dir">冲击方向</param>
    /// <param name="intensity">冲击强度（0~1）</param>
    public void ImpactDir(ImpactDirection dir, float intensity = 0.5f)
    {
        impactEff.TriggerDir(dir, intensity);
    }

    /// <summary>
    /// 触发螺旋效果：圆周旋转轨迹，幅度自然衰减呈向心螺旋
    /// </summary>
    /// <param name="intensity">螺旋振幅的强度（0~1）</param>
    public void Spiral(float intensity = 0.5f) => spiralEff.Trigger(intensity);

    /// <summary>
    /// 触发推进效果：瞬间拉近后平滑回位
    /// </summary>
    /// <param name="intensity">推进强度（0~1）</param>
    public void Zoom(float intensity = 0.5f) => zoomEff.Trigger(intensity);
    #endregion

    #region 调试
    private void OnDrawGizmos()
    {
        if (!ShowDebug) return; // 未开启调试时跳过

        // 绘制跟随目标
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position, 0.3f);
        }

        // 绘制相机目标位置（desired）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_desiredPosition, 0.3f);

        // 绘制相机实际位置
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
    #endregion

    #region 日志
    private void Log(string msg) => Debug.Log($"[{GetType().Name}] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[{GetType().Name}] {msg}");
    private void LogError(string msg) => Debug.LogError($"[{GetType().Name}] {msg}");
    #endregion
}
